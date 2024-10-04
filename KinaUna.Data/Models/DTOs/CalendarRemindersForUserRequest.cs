namespace KinaUna.Data.Models.DTOs
{
    public class CalendarRemindersForUserRequest
    {
        public string UserId { get; set; }
        public int EventId { get; set; }
        public bool FilterNotified { get; set; }
    }
}
