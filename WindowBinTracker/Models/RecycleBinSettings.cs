using System;

namespace WindowBinTracker.Models
{
    public class RecycleBinSettings
    {
        public long SizeThresholdBytes { get; set; } = 10737418240; // 10GB default
        public int CheckIntervalMs { get; set; } = 30000; // 30 seconds default
        public bool NotificationsEnabled { get; set; } = true;
        public DateTime? MuteUntil { get; set; } = null;
        public bool MinimizeToTray { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool ShowBalloonTips { get; set; } = true;

        public bool IsMuted => MuteUntil.HasValue && DateTime.Now < MuteUntil.Value;

        public void MuteForHours(int hours)
        {
            MuteUntil = DateTime.Now.AddHours(hours);
        }

        public void MuteForDays(int days)
        {
            MuteUntil = DateTime.Now.AddDays(days);
        }

        public void Unmute()
        {
            MuteUntil = null;
        }

        public string GetMuteStatusText()
        {
            if (!IsMuted)
                return "Notifications enabled";

            var timeRemaining = MuteUntil.Value - DateTime.Now;
            if (timeRemaining.TotalHours < 24)
                return $"Muted for {timeRemaining.Hours}h {timeRemaining.Minutes}m";
            else
                return $"Muted for {timeRemaining.Days}d {timeRemaining.Hours}h";
        }
    }
}
