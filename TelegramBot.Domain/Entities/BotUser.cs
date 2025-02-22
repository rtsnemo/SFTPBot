namespace TelegramBot.Domain.Entities
{
    public class FileUpload
    {
        public int Id { get; set; }
        public long UserId { get; set; } // Telegram ID пользователя
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

}
