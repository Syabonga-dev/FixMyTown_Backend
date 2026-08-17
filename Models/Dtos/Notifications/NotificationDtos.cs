namespace FixMyTownApi.Models.Dtos.Notifications
{
    /// <summary>One notification in the citizen's bell dropdown.</summary>
    public class NotificationReadDto
    {
        public int NotificationID { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
