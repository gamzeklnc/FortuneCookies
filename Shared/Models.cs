using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FortuneCookie.Shared
{
    public enum PacketType
    {
        Login,
        LoginSuccess,
        LoginFailed,
        GetFortune,
        FortuneResponse,
        TradeRequest,
        TradeResponse,
        FileTransferRequest,
        FileTransferAck,
        Broadcast,
        UserList,
        DirectMessage,
        Register,
        RegisterSuccess,
        RegisterFailed,
        SubmitFortune,
        GetHistory,
        HistoryResponse,
        GetMyFortunes,
        MyFortunesResponse
    }

    public class DirectMessagePayload
    {
        public string FromUser { get; set; } = string.Empty;
        public string ToUser { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    
    public class RegisterPayload
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    
    public class LoginPayload
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    
    public class SubmitFortunePayload
    {
         public string Text { get; set; } = string.Empty;
         public FortuneCategory Category { get; set; }
    }

    public class GetFortunePayload
    {
        public FortuneCategory Category { get; set; } = FortuneCategory.General;
    }

    public class FortuneHistoryDto
    {
        public string FortuneText { get; set; }
        public string Rarity { get; set; }
        public DateTime Date { get; set; }
    }

    public class FortuneHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FortuneId { get; set; }
        public DateTime ReceivedAt { get; set; }
        
        // Navigation properties (optional for JSON serialization but good for EF)
        // We won't serialize these deep graphs usually
        // public User User { get; set; }
        // public Fortune Fortune { get; set; }
    }

    public class Packet
    {
        public PacketType Type { get; set; }
        public string Payload { get; set; } // JSON serialized object

        public static Packet Create(PacketType type, object payload)
        {
            return new Packet
            {
                Type = type,
                Payload = System.Text.Json.JsonSerializer.Serialize(payload)
            };
        }

        public T ExtractPayload<T>()
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Payload);
        }
    }

    public enum FortuneRarity
    {
        Common,
        Rare,
        Legendary,
        Critical // Added for Cursed/Bad outcomes
    }

    public enum FortuneCategory
    {
        General,
        Funny,
        Wise,
        Cursed,
        Motivational, // New
        Romantic      // New
    }

    public class Fortune
    {
        public int Id { get; set; } // Changed to int for easy EF auto-inc
        public string Text { get; set; } = string.Empty;
        public FortuneCategory Category { get; set; }
        public FortuneRarity Rarity { get; set; }
        public int? AddedByUserId { get; set; } // Track who added it
        public int[]? LuckyNumbers { get; set; } // Not mapped to DB, used for transport
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Plaintext for simplicity
        public int Reputation { get; set; }
        // Relationships can be complex in serialized models, keeping simple for now
    }
}
