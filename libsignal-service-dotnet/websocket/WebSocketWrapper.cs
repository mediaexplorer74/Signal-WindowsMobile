using libsignalservice;
using libsignalservice.push.exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Coe.WebSocketWrapper
{
    /*
    internal class WebSocketWrapper
    {
        //https://gist.github.com/xamlmonkey/4737291
        private readonly ILogger Logger = LibsignalLogging.CreateLogger<WebSocketWrapper>();
        public BlockingCollection<byte[]> OutgoingQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
        private Task HandleOutgoing;
        private Task HandleIncoming;
        private const int ReceiveChunkSize = 1024;
        private volatile ClientWebSocket WebSocket;
        private readonly Uri _uri;
        private Action OnConnectedAction;
        private Action<byte[]> OnMessageAction;

        internal WebSocketWrapper(string uri)
        {
            CreateSocket();
            _uri = new Uri(uri);
        }

        private void CreateSocket()
        {
            WebSocket = new ClientWebSocket();
            WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        }

        public async Task HandleOutgoingWS(CancellationToken token)
        {
            Logger.LogTrace("HandleOutgoingWS()");
            byte[] buf = null;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (buf == null)
                        buf = OutgoingQueue.Take(token);
                    await WebSocket.SendAsync(new ArraySegment<byte>(buf, 0, buf.Length), WebSocketMessageType.Binary, true, token);
                    buf = null; //set to null so we do not retry the same block
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("HandleOutgoingWS task shutting down");
                }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Logger.LogWarning("HandleOutgoingWS send failed ({0})", e.Message);
                        Logger.LogInformation("HandleOutgoingWS reconnecting");
                        await Reconnect(token);
                    }
                }
            }
            //TODO dispose
            Logger.LogTrace("HandleOutgoingWS task finished");
        }

        public async Task Reconnect(CancellationToken token)
        {
            if (WebSocket.State == WebSocketState.Open)
            {
                return;
            }

            var tries = 0;
            try
            {
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "goodbye", token);
            }
            catch (Exception)
            {
                Logger.LogTrace("could not close old websocket gracefully");
            }
            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested)
                        return;
                    tries++;
                    CreateSocket();
                    await WebSocket.ConnectAsync(_uri, token);
                    break;
                }
                catch (Exception e)
                {
                    var delay_length = 15;
                    if (tries > 20)
                        delay_length = 60 * 5;
                    else if (tries > 10)
                        delay_length = 60;
                    else if (tries > 5)
                        delay_length = 30;
                    Logger.LogWarning("Failed to reconnect ({0}). Retrying in {1} seconds", e.Message, delay_length);
                    await Task.Delay(1000 * delay_length, token);
                }
            }
            Logger.LogInformation("Successfully reconnected to the server");
        }

        public async Task HandleIncomingWS(CancellationToken token)
        {
            Logger.LogTrace("HandleIncomingWS()");
            var buffer = new byte[ReceiveChunkSize];
            while (!token.IsCancellationRequested)
            {
                var message = new MemoryStream();
                WebSocketReceiveResult result;
                try
                {
                    do
                    {
                        result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            throw new Exception("Got a close message requesting reconnect");
                        }
                        else
                        {
                            message.Write(buffer, 0, result.Count);
                        }
                    } while (!result.EndOfMessage);
                    CallOnMessage(message.ToArray());
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("HandleIncomingWS shutting down");
                }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Logger.LogWarning("HandleIncomingWS recv failed ({0})", e.Message);
                        Logger.LogInformation("HandleIncomingWS reconnecting");
                        await Reconnect(token);
                    }
                }
            }
            //TODO dispose
            Logger.LogInformation("HandleIncomingWS finished");
        }

        /// <summary>
        /// Set the Action to call when the connection has been established.
        /// </summary>
        /// <param name="onConnect">The Action to call.</param>
        /// <returns></returns>
        public WebSocketWrapper OnConnect(Action onConnect)
        {
            OnConnectedAction = onConnect;
            return this;
        }

        /// <summary>
        /// Set the Action to call when a messages has been received.
        /// </summary>
        /// <param name="onMessage">The Action to call.</param>
        /// <returns></returns>
        public void OnMessage(Action<byte[]> onMessage)
        {
            OnMessageAction = onMessage;
        }

        public async Task Connect(CancellationToken token)
        {
            Logger.LogTrace("Connect()");
            try
            {
                await WebSocket.ConnectAsync(_uri, token);
                CallOnConnected();
            }
            catch (Exception e)
            {
                if(e.InnerException?.InnerException?.Message == "Forbidden")
                {
                    Logger.LogError("Server rejected authentication attempt");
                    throw new AuthorizationFailedException("OWS server rejected authorization.");
                }
                Logger.LogWarning("Connect could not connect to the server");
            }
            HandleOutgoing = Task.Factory.StartNew(async () => await HandleOutgoingWS(token), TaskCreationOptions.LongRunning);
            HandleIncoming = Task.Factory.StartNew(async () => await HandleIncomingWS(token), TaskCreationOptions.LongRunning);
        }

        private void CallOnMessage(byte[] result)
        {
            OnMessageAction?.Invoke(result);
        }

        private void CallOnConnected()
        {
            OnConnectedAction?.Invoke();
        }
    }
    */
}