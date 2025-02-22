namespace TelegramBot.Data.Entities
{
    public class BotUser
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; } // "Guest", "User", "Admin"
    }
}
