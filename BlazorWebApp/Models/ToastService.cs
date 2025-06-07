// ToastService.cs
using Microsoft.AspNetCore.Components;

namespace BlazorWebApp.Services
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = "";
        public ToastType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsVisible { get; set; } = true;
        public int Duration { get; set; } = 5000; // 5 seconds default
    }

    public class ToastService
    {
        public event Action<ToastMessage>? OnToastAdded;
        public event Action<string>? OnToastRemoved;

        public void ShowSuccess(string message, int duration = 5000)
        {
            ShowToast(message, ToastType.Success, duration);
        }

        public void ShowError(string message, int duration = 7000)
        {
            ShowToast(message, ToastType.Error, duration);
        }

        public void ShowWarning(string message, int duration = 6000)
        {
            ShowToast(message, ToastType.Warning, duration);
        }

        public void ShowInfo(string message, int duration = 5000)
        {
            ShowToast(message, ToastType.Info, duration);
        }

        private void ShowToast(string message, ToastType type, int duration)
        {
            var toast = new ToastMessage
            {
                Message = message,
                Type = type,
                Duration = duration
            };

            OnToastAdded?.Invoke(toast);
        }

        public void RemoveToast(string toastId)
        {
            OnToastRemoved?.Invoke(toastId);
        }
    }
}