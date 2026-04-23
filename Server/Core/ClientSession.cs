using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using KspConnected.Server.Handlers;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Core
{
    public class ClientSession
    {
        public int    PlayerId    { get; private set; } = -1;
        public string PlayerName  { get; private set; } = "";
        public bool   IsConnected { get; private set; } = true;

        private readonly TcpClient      _tcp;
        private readonly NetworkStream  _stream;
        private readonly KspServer      _server;
        private readonly object         _writeLock = new object();

        public ClientSession(TcpClient tcp, KspServer server)
        {
            _tcp    = tcp;
            _stream = tcp.GetStream();
            _server = server;
        }

        public void Start()
        {
            var thread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name         = "KspServer-Client-" + _tcp.Client.RemoteEndPoint,
            };
            thread.Start();
        }

        public void SetIdentity(int playerId, string playerName)
        {
            PlayerId   = playerId;
            PlayerName = playerName;
        }

        public void Send(MessageType type, byte[] payload)
        {
            if (!IsConnected) return;
            try
            {
                lock (_writeLock)
                    MessageFrame.WriteFrame(_stream, type, payload);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Session {PlayerId}] Send error: {ex.Message}");
                Close("Send error");
            }
        }

        private void ReadLoop()
        {
            Console.WriteLine($"[Session] Client connected from {_tcp.Client.RemoteEndPoint}");
            try
            {
                while (IsConnected)
                {
                    var (type, payload) = MessageFrame.ReadFrame(_stream);
                    MessageRouter.Route(this, _server, type, payload);
                }
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine($"[Session {PlayerId}] Client disconnected.");
            }
            catch (Exception ex) when (IsConnected)
            {
                Console.Error.WriteLine($"[Session {PlayerId}] Read error: {ex.Message}");
            }
            finally
            {
                Close("Read loop ended");
            }
        }

        public void Close(string reason = "")
        {
            if (!IsConnected) return;
            IsConnected = false;
            try { _stream.Close(); } catch { }
            try { _tcp.Close(); }    catch { }

            if (PlayerId >= 0)
                _server.OnSessionClosed(this, reason);
        }
    }
}
