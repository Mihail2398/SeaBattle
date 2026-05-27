using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NavalBattle.Network
{
    public class NetworkManager
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        public event Action<GamePacket> OnPacketReceived;
        public event Action OnConnectionLost;

        public async Task StartServer(int port)
        {
            var listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            _client = await listener.AcceptTcpClientAsync();
            StartProcessing();
        }

        public async Task ConnectToServer(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            StartProcessing();
        }

        private void StartProcessing()
        {
            _stream = _client.GetStream();
            _isConnected = true;
            _ = ReceiveLoop();
        }

        public async Task SendPacket(GamePacket packet)
        {
            if (!_isConnected) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet) + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch { HandleDisconnect(); }
        }

        private async Task ReceiveLoop()
        {
            using var reader = new StreamReader(_stream, Encoding.UTF8);
            try
            {
                while (_isConnected)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    var packet = JsonSerializer.Deserialize<GamePacket>(line);
                    if (packet != null) OnPacketReceived?.Invoke(packet);
                }
            }
            catch { HandleDisconnect(); }
            finally { HandleDisconnect(); }
        }

        private void HandleDisconnect()
        {
            if (!_isConnected) return;
            _isConnected = false;
            OnConnectionLost?.Invoke();
            _client?.Close();
        }
    }
}