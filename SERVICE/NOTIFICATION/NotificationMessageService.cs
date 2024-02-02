using Radzen;

namespace CORE.SERVICE.NOTIFICATION
{
    public class NotificationMessageService
    {
        NotificationService m_NotifService { get; set; }

        public NotificationMessageService(NotificationService notificationService)
        {
            m_NotifService = notificationService; //will be instance during service registration
        }

        public void ShowNotification(string Message)
        {
            ShowNotification(Message, null, NotificationSeverity.Success, 4);
        }
        public void ShowNotification(string Message, string? MessageDetail, NotificationSeverity Severity, double SecondsToKeepToast = 5)
        {
            //This function will be called to setup and display the notication usind Radzen notication service.

            var notif = new NotificationMessage()
            {
                Severity = Severity,
                Summary = Message,
                Detail = MessageDetail,
                Duration = SecondsToKeepToast * 1000,
            };

            m_NotifService.Notify(notif); //display notification
        }
    }
}
