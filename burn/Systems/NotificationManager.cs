using Peridot;
using Peridot.UI;
using static Peridot.UI.Toast;

public class NotificationManager
{
    public static void ShowNotification(string message)
    {
        Core.ToastManager.ShowInfo(message);
    }
}