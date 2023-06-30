using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Zkill
{

    public class ZkillClient : IDisposable, IZkillClient
    {
        ClientWebSocket zkillConnection;
        CancellationToken CancelToken { get; set; }
        Task? readThread = null;

        public event EventHandler<string>? KillReceived;

        ILogger<ZkillClient> Logger { get; }
        public ZkillClient(ILogger<ZkillClient> logger)
        {
            zkillConnection = new ClientWebSocket();
            Logger = logger;
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

        public async Task ConnectAsync(CancellationToken cancelToken)
        {

            CancelToken = cancelToken;

            if (readThread != null)
            {
                await readThread;
                readThread = null;
                if(cancelToken.IsCancellationRequested)
                    return;
            }

            if(zkillConnection.State != WebSocketState.None)
            {
                if(zkillConnection.State == WebSocketState.Open) await zkillConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancelToken);
                zkillConnection.Dispose();
                zkillConnection = new ClientWebSocket();
            }
            try
            {
                await zkillConnection.ConnectAsync(new Uri("wss://zkillboard.com/websocket/"), cancelToken);

                if (zkillConnection.State == WebSocketState.Open)
                    readThread = ReadAsync();
            }
            catch(WebSocketException wsex)
            {
                Logger.LogWarning("Failed to connect to zkill : " + wsex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            if (readThread != null)
            {
                await readThread;
                readThread = null;
            }
            await zkillConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancelToken);
        }

        private async Task ReadAsync()
        {
            List<string> lastKills = new List<string>();
            string recvStr = string.Empty;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            if(buffer.Array == null)
            {
                return;
            }
            while (zkillConnection.State == WebSocketState.Open)
            {
                WebSocketReceiveResult? result = null;
                Logger.LogInformation("Waiting for data from zkill");
                try
                {
                    result = await zkillConnection.ReceiveAsync(buffer, CancelToken);
                    Logger.LogInformation("Data received from zKill");
                }
                catch(TaskCanceledException)
                {
                    Logger.LogInformation("Aborting Read");
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                }

                if (null == result || result.MessageType == WebSocketMessageType.Close || CancelToken.IsCancellationRequested)
                {
                    break;
                }

                recvStr += Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                if (result.EndOfMessage)
                {
                    JsonDocument json = JsonDocument.Parse(recvStr);
                    string? link = json.RootElement.GetProperty("url").GetString();
                    
                    if(link != null && !lastKills.Contains(link))
                    {
                        KillReceived?.Invoke(this, link);
                        if(lastKills.Count == 5)
                        {
                            lastKills.RemoveAt(0);
                        }
                        lastKills.Add(link);
                    }
                    Logger.LogInformation("Kill Received");
                    recvStr = string.Empty;
                }
            }
        }

        async Task SendWebsocketTextAsync(string text)
        {
            try
            {
                await zkillConnection.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancelToken);
            }
            catch(Exception)
            {
            }
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

        public async Task SubscribeCorps(List<string> corpIds)
        {
            foreach(var corpId in corpIds)
            {
                await SendWebsocketTextAsync("{\"action\":\"sub\",\"channel\":\"corporation:" + corpId + "\"}");
            }
        }

        public async Task UnsubscribeCorps(List<string> corpIds)
        {
            foreach(var corpId in corpIds)
            {
                await SendWebsocketTextAsync("{\"action\":\"unsub\",\"channel\":\"corporation:" + corpId + "\"}");
            }
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