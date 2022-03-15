﻿using libsignal;
using libsignalservice;
using libsignalservice.crypto;
using libsignalservice.messages;
using libsignalservice.messages.multidevice;
using libsignalservice.push;
using libsignalservice.push.exceptions;
using libsignalservice.util;
using libsignalservicedotnet.crypto;
using Microsoft.Extensions.Logging;
using Signal_Windows.Models;
using Signal_Windows.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Signal_Windows.Lib
{
    interface ISendable
    {
        SignalMessageStatus Status { set; }
        Task Send(SignalServiceMessageSender messageSender, CancellationToken token);
    }

    class SignalServiceSyncMessageSendable : ISendable
    {
        public SignalMessageStatus Status { get; set; }
        private readonly SignalServiceSyncMessage SyncMessage;

        public SignalServiceSyncMessageSendable(SignalServiceSyncMessage message)
        {
            SyncMessage = message;
        }

        public async Task Send(SignalServiceMessageSender messageSender, CancellationToken token)
        {
            await messageSender.SendMessage(token, SyncMessage, null);
        }
    }

    class SignalServiceDataMessageSendable : ISendable
    {
        public SignalMessageStatus Status { get; set; }
        private readonly SignalServiceDataMessage DataMessage;
        private readonly SignalServiceAddress Recipient;

        public SignalServiceDataMessageSendable(SignalServiceDataMessage dataMessage, SignalServiceAddress recipient)
        {
            DataMessage = dataMessage;
            Recipient = recipient;
        }

        public async Task Send(SignalServiceMessageSender messageSender, CancellationToken token)
        {
            await messageSender.SendMessage(token, Recipient, null, DataMessage);
        }
    }

    class SignalMessageSendable : ISendable
    {
        private readonly ILogger Logger = LibsignalLogging.CreateLogger<SignalMessageSendable>();
        public readonly SignalMessage OutgoingSignalMessage;

        public SignalMessageSendable(SignalMessage message)
        {
            OutgoingSignalMessage = message;
        }

        public SignalMessageStatus Status { set => OutgoingSignalMessage.Status = value; }

        public async Task Send(SignalServiceMessageSender messageSender, CancellationToken token)
        {
            List<SignalServiceAttachment> outgoingAttachmentsList = null;
            if (OutgoingSignalMessage.Attachments != null && OutgoingSignalMessage.Attachments.Count > 0)
            {
                outgoingAttachmentsList = new List<SignalServiceAttachment>();
                foreach (var attachment in OutgoingSignalMessage.Attachments)
                {
                    try
                    {
                        var file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(@"Attachments\" + attachment.Id + ".plain");
                        var stream = await file.OpenStreamForReadAsync();
                        outgoingAttachmentsList.Add(SignalServiceAttachment.NewStreamBuilder()
                            .WithContentType(attachment.ContentType)
                            .WithStream(stream)
                            .WithLength(stream.Length)
                            .WithFileName(attachment.SentFileName)
                            .Build());
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"HandleOutgoingMessages() failed to add attachment {attachment.Id}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }

            SignalServiceDataMessage message = new SignalServiceDataMessage()
            {
                Body = OutgoingSignalMessage.Content.Content,
                Timestamp = OutgoingSignalMessage.ComposedTimestamp,
                ExpiresInSeconds = (int)OutgoingSignalMessage.ExpiresAt,
                Attachments = outgoingAttachmentsList
            };

            UpdateExpiresAt(OutgoingSignalMessage);
            DisappearingMessagesManager.QueueForDeletion(OutgoingSignalMessage);

            if (!OutgoingSignalMessage.ThreadId.EndsWith("="))
            {
                if (!token.IsCancellationRequested)
                {
                    await messageSender.SendMessage(token, new SignalServiceAddress(OutgoingSignalMessage.ThreadId), null, message);
                    OutgoingSignalMessage.Status = SignalMessageStatus.Confirmed;
                }
            }
            else
            {
                List<SignalServiceAddress> recipients = new List<SignalServiceAddress>();
                SignalGroup g = await SignalDBContext.GetOrCreateGroupLocked(OutgoingSignalMessage.ThreadId, 0);
                foreach (GroupMembership sc in g.GroupMemberships)
                {
                    if (sc.Contact.ThreadId != SignalLibHandle.Instance.Store.Username)
                    {
                        recipients.Add(new SignalServiceAddress(sc.Contact.ThreadId));
                    }
                }
                message.Group = new SignalServiceGroup()
                {
                    GroupId = Base64.Decode(g.ThreadId),
                    Type = SignalServiceGroup.GroupType.DELIVER
                };
                if (!token.IsCancellationRequested)
                {
                    var uaps = new List<UnidentifiedAccessPair>();
                    foreach (var _ in recipients)
                    {
                        uaps.Add(null);
                    }
                    await messageSender.SendMessage(token, recipients, uaps, message);
                    OutgoingSignalMessage.Status = SignalMessageStatus.Confirmed;
                }
            }
        }

        /// <summary>
        /// Updates a message ExpiresAt to be a timestamp instead of a relative value.
        /// </summary>
        /// <param name="message">The message to update</param>
        private void UpdateExpiresAt(SignalMessage message)
        {
            // We update here instead of earlier because we only want to start the timer once the message is actually sent.
            long messageExpiration;
            if (message.ExpiresAt == 0)
            {
                messageExpiration = 0;
            }
            else
            {
                messageExpiration = Util.CurrentTimeMillis() + (long)TimeSpan.FromSeconds(message.ExpiresAt).TotalMilliseconds;
            }
            message.ExpiresAt = messageExpiration;
        }
    }

    class OutgoingMessages
    {
        private readonly ILogger Logger = LibsignalLogging.CreateLogger<OutgoingMessages>();
        private readonly CancellationToken Token;
        private readonly SignalLibHandle Handle;
        private readonly SignalStore Store;
        private readonly SignalServiceMessagePipe Pipe;

        public OutgoingMessages(CancellationToken token, SignalServiceMessagePipe pipe, SignalStore store, SignalLibHandle handle)
        {
            Token = token;
            Pipe = pipe;
            Store = store;
            Handle = handle;
        }

        public async Task HandleOutgoingMessages()
        {
            Logger.LogDebug("HandleOutgoingMessages()");
            try
            {
                var messageSender = new SignalServiceMessageSender(Token, LibUtils.ServiceConfiguration, Store.Username, Store.Password, (int)Store.DeviceId, new Store(), LibUtils.USER_AGENT, Store.DeviceId != 1, Pipe, null, null);
                while (!Token.IsCancellationRequested)
                {
                    ISendable sendable = null;
                    try
                    {
                        sendable = Handle.OutgoingQueue.Take(Token);
                        Logger.LogTrace($"Sending {sendable.GetType().Name}");
                        await sendable.Send(messageSender, Token);
                    }
                    //catch (OperationCanceledException) { return; }
                    //catch (EncapsulatedExceptions exceptions)
                    catch (Exception exceptions)
                    {
                        sendable.Status = SignalMessageStatus.Confirmed;
                        Logger.LogError("HandleOutgoingMessages() encountered libsignal exceptions");
                        IList<UntrustedIdentityException> identityExceptions = exceptions.UntrustedIdentityExceptions;
                        if (exceptions.NetworkExceptions.Count > 0)
                        {
                            sendable.Status = SignalMessageStatus.Failed_Network;
                        }
                        
                        if (identityExceptions.Count > 0)
                        {
                            sendable.Status = SignalMessageStatus.Failed_Identity;
                        }

                        foreach (UntrustedIdentityException e in identityExceptions)
                        {
                            // TODO: Not sure what to do with this.
                            //await SendMessage(recipients, message);
                            //UpdateExpiresAt(outgoingSignalMessage);
                            //DisappearingMessagesManager.QueueForDeletion(outgoingSignalMessage);
                            //outgoingSignalMessage.Status = SignalMessageStatus.Confirmed;
                            await Handle.HandleOutgoingKeyChangeLocked(e.E164number, Base64.EncodeBytes(e.IdentityKey.serialize()));
                        }
                    }
                    catch (RateLimitException)
                    {
                        Logger.LogError("HandleOutgoingMessages() could not send due to rate limits");
                        sendable.Status = SignalMessageStatus.Failed_Ratelimit;
                    }
                    catch (UntrustedIdentityException e)
                    {
                        Logger.LogError("HandleOutgoingMessages() could not send due to untrusted identities");
                        sendable.Status = SignalMessageStatus.Failed_Identity;
                        await Handle.HandleOutgoingKeyChangeLocked(e.E164number, Base64.EncodeBytes(e.IdentityKey.serialize()));
                    }
                    catch (Exception e)
                    {
                        var line = new StackTrace(e, true).GetFrames()[0].GetFileLineNumber();
                        Logger.LogError("HandleOutgoingMessages() failed in line {0}: {1}\n{2}", line, e.Message, e.StackTrace);
                        sendable.Status = SignalMessageStatus.Failed_Unknown;
                    }
                    await Handle.HandleMessageSentLocked(sendable);
                }
                Logger.LogInformation("HandleOutgoingMessages() stopping: cancellation was requested");
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("HandleOutgoingMessages() stopping: cancellation was requested (OperationCancelledException)");
            }
            catch (Exception e)
            {
                Logger.LogError($"HandleOutgoingMessages() failed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                Logger.LogInformation("HandleOutgoingMessages() finished");
            }
        }
    }
}
