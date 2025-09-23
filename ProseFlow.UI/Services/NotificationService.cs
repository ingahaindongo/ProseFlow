using Avalonia.Threading;
using ShadUI;
using Notification = ShadUI.Notification;
using NotificationType = ProseFlow.Application.Events.NotificationType;

namespace ProseFlow.UI.Services;

public class NotificationService(ToastManager toastManager)
{
    public void Show(string message, NotificationType type)
    {
        var notificationType = type switch
        {
            NotificationType.Success => Notification.Success,
            NotificationType.Warning => Notification.Warning,
            NotificationType.Error => Notification.Error,
            _ => Notification.Info
        };

        Dispatcher.UIThread.Post(() =>
            toastManager.CreateToast(message).WithDelay(5).DismissOnClick().Show(notificationType));
    }
}