﻿using Microsoft.Toolkit.Uwp.Notifications;
using Signal_Windows.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;

namespace Signal_Windows.Lib
{
    public class NotificationsUtils
    {
        public static ToastNotifier Notifier = ToastNotificationManager.CreateToastNotifier();
        public static void Notify(SignalMessage message)
        {
            TryVibrate(true);
            SendMessageNotification(message);
            SendTileNotification(message);
        }

        public static void TryVibrate(bool quick)
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.Devices.Notification.VibrationDevice"))
            {
                Windows.Phone.Devices.Notification.VibrationDevice.GetDefault().Vibrate(TimeSpan.FromMilliseconds(quick ? 100 : 500));
            }
        }

        public static void SendMessageNotification(SignalMessage message)
        {
            // notification tags can only be 16 chars (64 after creators update)
            // https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Notifications.ToastNotification#Windows_UI_Notifications_ToastNotification_Tag
            string notificationId = message.ThreadId;
            ToastBindingGeneric toastBinding = new ToastBindingGeneric()
            {
                AppLogoOverride = new ToastGenericAppLogo()
                {
                    Source = "ms-appx:///Assets/LargeTile.scale-100.png",
                    HintCrop = ToastGenericAppLogoCrop.Circle
                }
            };

            var notificationText = GetNotificationText(message);
            foreach (var item in notificationText)
            {
                toastBinding.Children.Add(item);
            }

            ToastContent toastContent = new ToastContent()
            {
                Launch = notificationId,
                Visual = new ToastVisual()
                {
                    BindingGeneric = toastBinding
                },
                DisplayTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(message.ReceivedTimestamp)
            };

            ToastNotification toastNotification = new ToastNotification(toastContent.GetXml());
            if (message.Author.ExpiresInSeconds > 0)
            {
                toastNotification.ExpirationTime = DateTime.Now.Add(TimeSpan.FromSeconds(message.Author.ExpiresInSeconds));
            }
            toastNotification.Tag = notificationId;
            Notifier.Show(toastNotification);
        }

        private static IList<AdaptiveText> GetNotificationText(SignalMessage message)
        {
            List<AdaptiveText> text = new List<AdaptiveText>();
            if (GlobalSettingsManager.ShowNotificationTextSetting == GlobalSettingsManager.ShowNotificationTextSettings.NameAndMessage)
            {
                text.Add(CreateToastTitle(message.Author.ThreadDisplayName));
                text.Add(CreateToastBody(message.Content.Content));
            }
            else if (GlobalSettingsManager.ShowNotificationTextSetting == GlobalSettingsManager.ShowNotificationTextSettings.NameOnly)
            {
                text.Add(CreateToastTitle(message.Author.ThreadDisplayName));
            }
            else if (GlobalSettingsManager.ShowNotificationTextSetting == GlobalSettingsManager.ShowNotificationTextSettings.NoNameOrMessage)
            {
                text.Add(CreateToastTitle("New message"));
            }
            else
            {
                text.Add(CreateToastTitle("New message"));
            }
            return text;
        }

        private static AdaptiveText CreateToastTitle(string text)
        {
            return new AdaptiveText()
            {
                Text = text,
                HintMaxLines = 1
            };
        }

        private static AdaptiveText CreateToastBody(string text)
        {
            return new AdaptiveText()
            {
                Text = text,
                HintWrap = true
            };
        }

        public static void SendTileNotification(SignalMessage message)
        {
            TileBindingContentAdaptive tileBindingContent = new TileBindingContentAdaptive()
            {
                /*
                PeekImage = new TilePeekImage()
                {
                    Source = "ms-appx:///Assets/gambino.png"
                }
                */
            };
            var notificationText = GetNotificationText(message);
            foreach (var item in notificationText)
            {
                tileBindingContent.Children.Add(item);
            }

            TileBinding tileBinding = new TileBinding()
            {
                Content = tileBindingContent
            };

            TileContent tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = tileBinding,
                    TileWide = tileBinding,
                    TileLarge = tileBinding
                }
            };

            TileNotification tileNotification = new TileNotification(tileContent.GetXml());
            if (message.Author.ExpiresInSeconds > 0)
            {
                tileNotification.ExpirationTime = DateTime.Now.Add(TimeSpan.FromSeconds(message.Author.ExpiresInSeconds));
            }
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);
        }

        public static void Withdraw(string threadId)
        {
            ToastNotificationManager.History.Remove(threadId);
        }
    }
}
