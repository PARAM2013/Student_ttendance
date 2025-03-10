using Student_Attendance.Models.Logging;
using Student_Attendance.Models;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class ActivityLog
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; }
        public string LogType { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Module { get; set; }
        public string? RequestUrl { get; set; }
        public bool IsSuccess { get; set; }
        public string? Status { get; set; }
        public string? IpAddress { get; set; }
        public int? UserId { get; set; }
        public Models.User? User { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceType { get; set; }
        public string? BrowserType { get; set; }
        public string? OperatingSystem { get; set; }
        public string? ScreenResolution { get; set; }
        public string? NetworkType { get; set; }

        // Constructor to convert from Models.Logging.ActivityLog
        public ActivityLog(Models.Logging.ActivityLog log)
        {
            Id = log.Id;
            Timestamp = log.Timestamp;
            LogLevel = log.LogLevel;
            LogType = log.LogType;
            Action = log.Action;
            EntityType = log.EntityType;
            EntityId = log.EntityId;
            Details = log.Details;
            OldValue = log.OldValue;
            NewValue = log.NewValue;
            Module = log.Module;
            RequestUrl = log.RequestUrl;
            IsSuccess = log.IsSuccess;
            Status = log.Status;
            IpAddress = log.IpAddress;
            UserId = log.UserId;
            User = log.User;
            UserAgent = log.UserAgent;
            DeviceType = log.DeviceType;
            BrowserType = log.BrowserType;
            OperatingSystem = log.OperatingSystem;
            ScreenResolution = log.ScreenResolution;
            NetworkType = log.NetworkType;
        }

        // Default constructor
        public ActivityLog() { }
    }
}