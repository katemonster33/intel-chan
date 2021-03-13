using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Zkill
{

    public class ZkillClient : IDisposable, IZkillClient
    {
        ClientWebSocket zkillConnection;
        CancellationTokenSource cancelToken;
        Task readThread = null;

        public event EventHandler<string> KillReceived;

        public ZkillClient()
        {
            zkillConnection = new ClientWebSocket();
            cancelToken = new CancellationTokenSource();
        }

        public bool Connected
        {
            get => zkillConnection.State == WebSocketState.Open;
        }

        public async void Dispose()
        {
            if (zkillConnection.State == WebSocketState.Open)
            {
                await DisconnectAsync();
            }
            zkillConnection.Dispose();
        }

        public async Task ConnectAsync()
        {
            if (Connected)
            {
                throw new InvalidOperationException();
            }
            if (readThread != null)
            {
                cancelToken.Cancel();
                await readThread;
                readThread = null;
            }

            await zkillConnection.ConnectAsync(new Uri("wss://zkillboard.com/websocket/"), cancelToken.Token);

            if (zkillConnection.State == WebSocketState.Open)
            {
                readThread = ReadAsync();
            }
        }

        public async Task DisconnectAsync()
        {
            if (readThread != null)
            {
                cancelToken.Cancel();
                await readThread;
                readThread = null;
            }
            await zkillConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancelToken.Token);
        }

        public async Task ReadAsync()
        {
            string recvStr = string.Empty;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            while (zkillConnection.State == WebSocketState.Open)
            {
                try
                {
                    var result = await zkillConnection.ReceiveAsync(buffer, cancelToken.Token);
                    if (result.MessageType == WebSocketMessageType.Close || cancelToken.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    recvStr += Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        JsonDocument json = JsonDocument.Parse(recvStr);
                        string link = json.RootElement.GetProperty("url").GetString();
                        KillReceived?.Invoke(this, link);
                        recvStr = string.Empty;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        async Task SendWebsocketTextAsync(string text)
        {
            await zkillConnection.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, cancelToken.Token);
        }

        string GetZkillWebsocketCommand(string cmd, IEnumerable<string> systemIds)
        {
            return "{\"action\":\"" + cmd + "\",\"channel\":\"" + string.Join(',', systemIds.Select(id => "system:" + id)) + "\"}";
        }

        public async Task SubscribeAll()
        {
            await SendWebsocketTextAsync("{\"action\":\"sub\",\"channel\":\"killstream\"}");
        }

        public async Task UnsubscribeAll()
        {
            await SendWebsocketTextAsync("{\"action\":\"unsub\",\"channel\":\"killstream\"}");
        }

        public async Task SubscribeSystems(List<string> systemIds)
        {
            foreach (var systemId in systemIds)
            {
                await SendWebsocketTextAsync("{\"action\":\"sub\",\"channel\":\"system:" + systemId + "\"}");
            }
        }

        public async Task UnsubscribeSystems(List<string> systemIds)
        {
            foreach (var systemId in systemIds)
            {
                await SendWebsocketTextAsync("{\"action\":\"unsub\",\"channel\":\"system:" + systemId + "\"}");
            }
        }
    }
}