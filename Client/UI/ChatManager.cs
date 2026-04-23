using System;
using System.Collections.Generic;
using KspConnected.Client.Core;
using KspConnected.Shared.Messages;

namespace KspConnected.Client.UI
{
    public class ChatEntry
    {
        public string SenderName;
        public string Text;
        public DateTime Timestamp;
    }

    public class ChatManager
    {
        private const int MaxHistory = 200;

        private readonly ConnectionManager _conn;
        private readonly List<ChatEntry>   _history = new List<ChatEntry>();
        private readonly object            _lock    = new object();

        public event Action<ChatEntry> OnNewMessage;

        public ChatManager(ConnectionManager conn) => _conn = conn;

        public void Receive(ChatMessage msg)
        {
            var entry = new ChatEntry
            {
                SenderName = msg.SenderName,
                Text       = msg.Text,
                Timestamp  = DateTime.UtcNow,
            };
            lock (_lock)
            {
                _history.Add(entry);
                if (_history.Count > MaxHistory)
                    _history.RemoveAt(0);
            }
            OnNewMessage?.Invoke(entry);
        }

        public void Send(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _conn.State != ConnectionState.Connected) return;
            _conn.SendChat(new ChatMessage
            {
                SenderId   = _conn.LocalPlayerId,
                SenderName = KspConnectedMod.Instance?.PlayerName ?? "?",
                Text       = text.Trim(),
            });
        }

        public List<ChatEntry> GetHistory()
        {
            lock (_lock) return new List<ChatEntry>(_history);
        }
    }
}
