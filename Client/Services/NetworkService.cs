using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using FortuneCookie.Shared;

namespace FortuneCookie.Client.Services
{
    public class NetworkService
    {
        private TcpClient _tcpClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private UdpClient _udpClient;
        private const int ServerPort = 5000;
        private const int UdpPort = 5001;
        private const string MulticastGroup = "239.0.0.1";

        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action<string> OnRegisterSuccess;
        public event Action<string> OnRegisterFailed;
        public event Action<Fortune> OnFortuneReceived;
        public event Action<List<FortuneHistoryDto>> OnHistoryReceived;
        public event Action<string> OnBroadcastReceived;
        public event Action<List<string>> OnUserListReceived;
        public event Action<DirectMessagePayload> OnMessageReceived;
        public event Action<List<Fortune>> OnMyFortunesReceived;

        public async Task ConnectAsync(string username, string password)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("127.0.0.1", ServerPort);
                var stream = _tcpClient.GetStream();
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream) { AutoFlush = true };

                // Start Listening Loop
                _ = Task.Run(ReceiveLoop);

                // Send Login
                var payload = new LoginPayload { Username = username, Password = password };
                var loginPacket = Packet.Create(PacketType.Login, payload);
                await SendPacketAsync(loginPacket);

                // Start UDP Listener
                _ = Task.Run(StartUdpListener);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        public async Task RegisterAsync(string username, string password)
        {
             // Establish temp connection just for Reg or reuse logic?
             // Since we need to connect first usually.
             // Let's assume Connect -> Register -> Disconnect or similar
             // Actually, simplest is: Login screen has "Connect" logic.
             // If we click Register, we Connect -> Send Register -> Receive Result.
             
             if (_tcpClient == null || !_tcpClient.Connected)
             {
                 _tcpClient = new TcpClient();
                 await _tcpClient.ConnectAsync("127.0.0.1", ServerPort);
                 var stream = _tcpClient.GetStream();
                 _reader = new StreamReader(stream);
                 _writer = new StreamWriter(stream) { AutoFlush = true };
                 _ = Task.Run(ReceiveLoop);
             }

             var payload = new RegisterPayload { Username = username, Password = password };
             await SendPacketAsync(Packet.Create(PacketType.Register, payload));
        }

        public async Task SubmitFortuneAsync(string text, FortuneCategory category)
        {
            var payload = new SubmitFortunePayload { Text = text, Category = category };
            await SendPacketAsync(Packet.Create(PacketType.SubmitFortune, payload));
        }

        public async Task RequestFortuneAsync(FortuneCategory category)
        {
            var payload = new GetFortunePayload { Category = category };
            var packet = Packet.Create(PacketType.GetFortune, payload);
            await SendPacketAsync(packet);
        }

        public async Task RequestHistoryAsync()
        {
            var packet = Packet.Create(PacketType.GetHistory, null);
            await SendPacketAsync(packet);
        }

        public async Task RequestMyFortunesAsync()
        {
            var packet = Packet.Create(PacketType.GetMyFortunes, null);
            await SendPacketAsync(packet);
        }

        public async Task SendDirectMessageAsync(string targetUser, string message)
        {
            var payload = new DirectMessagePayload { ToUser = targetUser, Message = message };
            var packet = Packet.Create(PacketType.DirectMessage, payload);
            await SendPacketAsync(packet);
        }

        private async Task SendPacketAsync(Packet packet)
        {
            if (_tcpClient == null || !_tcpClient.Connected) return;
            string json = JsonSerializer.Serialize(packet);
            await _writer.WriteLineAsync(json);
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    string line = await _reader.ReadLineAsync();
                    if (line == null) break;

                    var packet = JsonSerializer.Deserialize<Packet>(line);
                    if (packet == null) continue;

                    switch (packet.Type)
                    {
                        case PacketType.LoginSuccess:
                            OnLoginSuccess?.Invoke(packet.ExtractPayload<string>());
                            break;
                        case PacketType.LoginFailed:
                            OnLoginFailed?.Invoke(packet.ExtractPayload<string>());
                            break;
                        case PacketType.RegisterSuccess:
                            OnRegisterSuccess?.Invoke(packet.ExtractPayload<string>());
                            break;
                        case PacketType.RegisterFailed:
                            OnRegisterFailed?.Invoke(packet.ExtractPayload<string>());
                            break;
                        case PacketType.FortuneResponse:
                            OnFortuneReceived?.Invoke(packet.ExtractPayload<Fortune>());
                            break;
                        case PacketType.HistoryResponse:
                            OnHistoryReceived?.Invoke(packet.ExtractPayload<List<FortuneHistoryDto>>());
                            break;
                        case PacketType.UserList:
                            OnUserListReceived?.Invoke(packet.ExtractPayload<List<string>>());
                            break;
                        case PacketType.Broadcast:
                             // Reusing the same event as UDP broadcast for UI consistency
                             var fortune = packet.ExtractPayload<Fortune>();
                             string nums = (fortune.LuckyNumbers != null) ? $" [Şanslı Sayılar: {string.Join("-", fortune.LuckyNumbers)}]" : "";
                             OnBroadcastReceived?.Invoke($"📢 GÜNÜN FALI: {fortune.Text}{nums}");
                             break;
                        case PacketType.DirectMessage:
                            OnMessageReceived?.Invoke(packet.ExtractPayload<DirectMessagePayload>());
                            break;
                        case PacketType.MyFortunesResponse:
                             OnMyFortunesReceived?.Invoke(packet.ExtractPayload<List<Fortune>>());
                             break;
                    }
                }
            }
            catch (Exception ex)
            {
                 // Handle disconnect
            }
        }

        private async Task StartUdpListener()
        {
            try
            {
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.ExclusiveAddressUse = false;
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
                _udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastGroup));

                while (true)
                {
                    var result = await _udpClient.ReceiveAsync();
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    var packet = JsonSerializer.Deserialize<Packet>(json);

                    if (packet?.Type == PacketType.Broadcast)
                    {
                        var fortune = packet.ExtractPayload<Fortune>();
                        string nums = (fortune.LuckyNumbers != null) ? $" [Şanslı Sayılar: {string.Join("-", fortune.LuckyNumbers)}]" : "";
                        OnBroadcastReceived?.Invoke($"📢 GÜNÜN FALI: {fortune.Text}{nums}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle UDP error
            }
        }
    }
}
