namespace TelegramBot.Domain.Entities
{
    public class BotUser
    {
        public long Id { get; set; } // ID пользователя Telegram
        public string Role { get; set; } = "User"; // Роль: "User" или "Admin"
        public string AppName { get; set; }
        public string AppBundle { get; set; }
        public string Secret { get; set; }
        public string SecretKeyParam { get; set; }
        public string SftpHost { get; set; }
        public string SftpUsername { get; set; }
        public string SftpPassword { get; set; }
    }

}
