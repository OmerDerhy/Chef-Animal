using System;
using UnityEngine;
using Unity.Notifications.Android;
using Unity.Notifications.iOS;

public class NotificationManager : MonoBehaviour
{
    private static NotificationManager _instance;
    public static NotificationManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                var go = new GameObject("NotificationManager");
                _instance = go.AddComponent<NotificationManager>();
            }
            return _instance;
        } 
    }

    private const string CHANNEL_ID = "daily_gift";

    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNotifications();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "Daily Gift Reminder",
            Importance = Importance.Default,
            Description = "Reminders to claim your daily gifts",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        RequestAndroidPermissions();
#endif
    }

#if UNITY_ANDROID
    private void RequestAndroidPermissions()
    {
        if (!Unity.Notifications.Android.PermissionStatus.Granted.Equals(AndroidNotificationCenter.UserPermissionToPost))
        {
            AndroidNotificationCenter.Initialize();
        }
    }
#endif

    public void ScheduleDailyGiftNotification(TimeSpan delay)
    {
        CancelAllNotifications();

        string title = "Daily Gift Available!";
        string text = "Your daily gift is ready! Come back to claim your rewards!";
        DateTime fireTime = DateTime.Now.Add(delay);

        Debug.Log($"Scheduling Daily Gift Notification for: {fireTime}");

#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = fireTime,
            SmallIcon = "icon_small", // Use default small icon
            LargeIcon = "icon_large"  // Use default large icon
        };
        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = delay,
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "daily_gift_reminder",
            Title = title,
            Body = text,
            ShowInForeground = false,
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
    }
}
