using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FortuneCookie.Shared;

namespace FortuneCookie.Server
{
    class Program
    {
        private static TcpListener? _tcpListener;
        private static UdpClient? _udpClient;
        private static ConcurrentDictionary<string, TcpClient> _connectedClients = new();
        private const int TcpPort = 5000;
        private const int UdpPort = 5001;
        private const string MulticastGroup = "239.0.0.1";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Fortune Server Starting...");

            // Initialize Database
            FortuneBank.Initialize();

            // Start UDP Broadcaster
            _ = Task.Run(StartUdpBroadcast);

            // Start TCP Listener
            _tcpListener = new TcpListener(IPAddress.Any, TcpPort);
            _tcpListener.Start();
            Console.WriteLine($"TCP Server listening on port {TcpPort}");

            while (true)
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                Console.WriteLine("New client connected.");
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new System.IO.StreamReader(stream);
            var writer = new System.IO.StreamWriter(stream) { AutoFlush = true };
            string username = "Unknown";

            try
            {
                while (client.Connected)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break;

                    var packet = JsonSerializer.Deserialize<Packet>(line);
                    if (packet == null) continue;

                    switch (packet.Type)
                    {
                        case PacketType.Login:
                            var loginData = packet.ExtractPayload<LoginPayload>(); // Assuming Client now sends object or we parse string
                            // For backward compatibility if client wasn't fully updated yet, we might check if payload is json or plain string
                            // But let's assume we update client. 
                            // Wait, previous code sent just "username" string. 
                            // Let's support simple username login for backward compat but Check DB if user exists.
                            
                             username = loginData.Username;
                             
                             using (var db = new FortuneDbContext())
                             {
                                 var user = System.Linq.Enumerable.FirstOrDefault(db.Users, u => u.Username == username && u.Password == loginData.Password);
                                 if (user != null)
                                 {
                                     _connectedClients.TryAdd(username, client);
                                     Console.WriteLine($"{username} logged in.");
                                     var response = Packet.Create(PacketType.LoginSuccess, $"Welcome back {username}");
                                     await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                                     await BroadcastUserList();

                                     // Send "Fortune of the Day" immediately (Welcome Fortune)
                                     var welcomeFortune = FortuneBank.GetRandomFortune();
                                     var welcomePacket = Packet.Create(PacketType.Broadcast, welcomeFortune);
                                     await writer.WriteLineAsync(JsonSerializer.Serialize(welcomePacket));
                                 }
                                 else
                                 {
                                     var response = Packet.Create(PacketType.LoginFailed, "Invalid username or password.");
                                     await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                                     username = "Unknown"; // don't set session username
                                 }
                             }
                            break;

                        case PacketType.Register:
                             var regData = packet.ExtractPayload<RegisterPayload>();
                             using (var db = new FortuneDbContext())
                             {
                                 if (System.Linq.Enumerable.Any(db.Users, u => u.Username == regData.Username))
                                 {
                                     var fail = Packet.Create(PacketType.RegisterFailed, "Username already exists.");
                                     await writer.WriteLineAsync(JsonSerializer.Serialize(fail));
                                 }
                                 else
                                 {
                                     db.Users.Add(new User { Username = regData.Username, Password = regData.Password, Reputation = 0 });
                                     db.SaveChanges();
                                     var success = Packet.Create(PacketType.RegisterSuccess, "Registration successful! You can login now.");
                                     await writer.WriteLineAsync(JsonSerializer.Serialize(success));
                                     Console.WriteLine($"Registered new user: {regData.Username}");
                                 }
                             }
                             break;
                        
                        case PacketType.SubmitFortune:
                             var subData = packet.ExtractPayload<SubmitFortunePayload>();
                             int? submitterId = null;
                             if (username != "Unknown")
                             {
                                 using (var db = new FortuneDbContext())
                                 {
                                     var u = System.Linq.Enumerable.FirstOrDefault(db.Users, x => x.Username == username);
                                     if (u != null) submitterId = u.Id;
                                 }
                             }
                             FortuneBank.AddFortune(subData.Text, subData.Category, submitterId);
                             Console.WriteLine($"New fortune submitted by {username}");
                             break;
                        
                        case PacketType.GetMyFortunes:
                             if (username != "Unknown")
                             {
                                 using (var db = new FortuneDbContext())
                                 {
                                     var u = System.Linq.Enumerable.FirstOrDefault(db.Users, x => x.Username == username);
                                     if (u != null)
                                     {
                                         var myFortunes = FortuneBank.GetFortunesByUser(u.Id);
                                         var resp = Packet.Create(PacketType.MyFortunesResponse, myFortunes);
                                         await writer.WriteLineAsync(JsonSerializer.Serialize(resp));
                                     }
                                 }
                             }
                             break;

                         case PacketType.GetFortune:
                            FortuneCategory? filter = null;
                            if (!string.IsNullOrEmpty(packet.Payload) && packet.Payload != "null")
                            {
                                try 
                                {
                                    var payload = packet.ExtractPayload<GetFortunePayload>();
                                    filter = payload.Category;
                                }
                                catch {} // Fallback to null/random if payload structure mismatch
                            }
                            
                            var fortune = FortuneBank.GetRandomFortune(filter);
                            
                            // Save to History
                            if (username != "Unknown")
                            {
                                using (var db = new FortuneDbContext())
                                {
                                    var user = System.Linq.Enumerable.FirstOrDefault(db.Users, u => u.Username == username);
                                    var dbFortune = System.Linq.Enumerable.FirstOrDefault(db.Fortunes, f => f.Text == fortune.Text); // Simple lookup
                                    
                                    if (user != null && dbFortune != null)
                                    {
                                        db.FortuneHistories.Add(new FortuneHistory
                                        {
                                            UserId = user.Id,
                                            FortuneId = dbFortune.Id,
                                            ReceivedAt = DateTime.Now
                                        });
                                        db.SaveChanges();
                                    }
                                }
                            }

                            var fortunePacket = Packet.Create(PacketType.FortuneResponse, fortune);
                            await writer.WriteLineAsync(JsonSerializer.Serialize(fortunePacket));
                            Console.WriteLine($"Sent fortune to {username}");
                            break;

                        case PacketType.GetHistory:
                             if (username != "Unknown")
                             {
                                 using (var db = new FortuneDbContext())
                                 {
                                     var user = System.Linq.Enumerable.FirstOrDefault(db.Users, u => u.Username == username);
                                     if (user != null)
                                     {
                                         // Manual Join since we didn't set up navigation props fully in code-first for simplicity or to avoid lazy loading issues with serialization
                                         var historyList = (from h in db.FortuneHistories
                                                            join f in db.Fortunes on h.FortuneId equals f.Id
                                                            where h.UserId == user.Id
                                                            orderby h.ReceivedAt descending
                                                            select new FortuneHistoryDto
                                                            {
                                                                FortuneText = f.Text,
                                                                Rarity = f.Rarity.ToString(),
                                                                Date = h.ReceivedAt
                                                            }).ToList();

                                         var histPacket = Packet.Create(PacketType.HistoryResponse, historyList);
                                         await writer.WriteLineAsync(JsonSerializer.Serialize(histPacket));
                                     }
                                 }
                             }
                             break;
                            
                        case PacketType.DirectMessage:
                            var dm = packet.ExtractPayload<DirectMessagePayload>();
                            if (dm != null && _connectedClients.TryGetValue(dm.ToUser, out var targetClient))
                            {
                                dm.FromUser = username; // Ensure authenticity
                                var outgoing = Packet.Create(PacketType.DirectMessage, dm);
                                var targetWriter = new System.IO.StreamWriter(targetClient.GetStream()) { AutoFlush = true };
                                await targetWriter.WriteLineAsync(JsonSerializer.Serialize(outgoing));
                                Console.WriteLine($"Message from {username} to {dm.ToUser}");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                if (username != "Unknown") 
                {
                    _connectedClients.TryRemove(username, out _);
                    await BroadcastUserList(); // Update others
                }
                client.Close();
                Console.WriteLine($"{username} disconnected.");
            }
        }

        private static async Task BroadcastUserList()
        {
            var userList = _connectedClients.Keys.ToList();
            var packet = Packet.Create(PacketType.UserList, userList);
            var json = JsonSerializer.Serialize(packet);

            foreach (var client in _connectedClients.Values)
            {
                try
                {
                    var writer = new System.IO.StreamWriter(client.GetStream()) { AutoFlush = true };
                    await writer.WriteLineAsync(json);
                }
                catch { /* Ignore disconnected clients here */ }
            }
        }

        private static async Task StartUdpBroadcast()
        {
            _udpClient = new UdpClient();
            var endpoint = new IPEndPoint(IPAddress.Parse(MulticastGroup), UdpPort);
            _udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastGroup));

            Console.WriteLine($"UDP Broadcast started on {MulticastGroup}:{UdpPort}");

            while (true)
            {
                await Task.Delay(60000); // Every 60 seconds
                var fortune = FortuneBank.GetRandomFortune();
                var packet = Packet.Create(PacketType.Broadcast, fortune);
                string json = JsonSerializer.Serialize(packet);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                
                await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
                Console.WriteLine("Broadcasted Daily Fortune.");
            }
        }
    }
}
