namespace TelegramBot.Domain.Entities
{
    public class UserSession
    {
        public string AppName { get; set; }
        public string AppBundle { get; set; }
        public string Secret { get; set; }
        public string SecretKeyParam { get; set; }
        public string SftpHost { get; set; }
        public string SftpUsername { get; set; }
        public string SftpPassword { get; set; }
        public int SftpPort { get; set; } = 22;
        public string Step { get; set; }
    }
}
