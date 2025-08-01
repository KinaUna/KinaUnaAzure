namespace KinaUna.Data.Models
{
    public class UpdateUserEmailModel
    {
        public string UserId { get; set; }
        public string NewEmail { get; set; }
        public string OldEmail { get; set; }
    }
}
