﻿using libsignalservice;
using libsignalservice.push.exceptions;
using libsignalservice.websocket;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Signal_Windows.Lib
{
    public class SignalWebSocketFactory : ISignalWebSocketFactory
    {
        public ISignalWebSocket CreateSignalWebSocket(CancellationToken token, Uri uri)
        {
            return new SignalWebSocket(token, uri);
        }
    }

    class SignalWebSocket : ISignalWebSocket
    {
        private readonly ILogger Logger = LibsignalLogging.CreateLogger<SignalWebSocket>();
        private MessageWebSocket WebSocket;
        private readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Uri SignalWSUri;
        private readonly CancellationToken Token;
        public event EventHandler<SignalWebSocketClosedEventArgs> Closed;
        public event EventHandler<SignalWebSocketMessageReceivedEventArgs> MessageReceived;

        public SignalWebSocket(CancellationToken token, Uri uri)
        {
            CreateMessageWebSocket();
            Token = token;
            SignalWSUri = uri;
        }

        private void CreateMessageWebSocket()
        {
            WebSocket = new MessageWebSocket();
            WebSocket.MessageReceived += WebSocket_MessageReceived;
            WebSocket.Closed += WebSocket_Closed;
        }

        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            Closed?.Invoke(sender, new SignalWebSocketClosedEventArgs() { Code = args.Code, Reason = args.Reason });
            Logger.LogWarning("WebSocket_Closed() {0} ({1})", args.Code, args.Reason);
        }

        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            Logger.LogTrace("WebSocket_MessageReceived()");
            try
            {
                using (var data = args.GetDataStream())
                using (var buffer = new MemoryStream())
                {
                    data.AsStreamForRead().CopyTo(buffer);
                    MessageReceived.Invoke(sender, new SignalWebSocketMessageReceivedEventArgs() { Message = buffer.ToArray() });
                }
            }
            catch(Exception e)
            {
                Logger.LogError("WebSocket_MessageReceived failed: {0}\n{1}", e.Message, e.StackTrace);
                Task.Run(ConnectAsync);
            }
        }

        public void Close(ushort code, string reason)
        {
            Logger.LogTrace("Closing SignalWebSocket connection");
            WebSocket.Close(code, reason);
        }

        public async Task ConnectAsync()
        {
            Logger.LogTrace("ConnectAsync()");
            var locked = await SemaphoreSlim.WaitAsync(0, Token); // ensure no threads are reconnecting at the same time
            if (locked)
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        CreateMessageWebSocket();
                        Logger.LogTrace("WebSocket.ConnectAsync()");
                        await WebSocket.ConnectAsync(SignalWSUri).AsTask(Token);
                        SemaphoreSlim.Release();
                        break;
                    }
                    catch (OperationCanceledException) 
                    { 
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("(403)"))
                        {
                            SemaphoreSlim.Release();

                            //throw new AuthorizationFailedException("OWS server rejected authorization.");
                            Debug.WriteLine("[e] Error 403. OWS server rejected authorization.");
                        }
                        Logger.LogError("ConnectAsync() failed: {0}\n{1}", e.Message, e.StackTrace); //System.Runtime.InteropServices.COMException (0x80072EE7)
                        await Task.Delay(10 * 1000);
                    }
                }
            }
            else
            {
                Logger.LogTrace("ConnectAsync() not reconnecting: Reconnect in progress");
            }
        }

        public void Dispose()
        {
            WebSocket.Dispose();
        }

        public async Task SendMessage(byte[] data)
        {
            Logger.LogTrace("SendMessage()");
            try
            {
                using (var dataWriter = new DataWriter(WebSocket.OutputStream))
                {
                    dataWriter.WriteBytes(data);
                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogTrace($"SendMessage() was cancelled");
            }
            catch (Exception e)
            {
                Logger.LogError($"SendMessage() failed: {e.Message}\n{e.StackTrace}");
                var t = Task.Run(ConnectAsync);
            }
        }
    }
}
