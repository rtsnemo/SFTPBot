using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBot.Data.Entities
{
    [Table("FileUploadLogs")]
    public class FileUploadLog
    {
        [Key]
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string AppName { get; set; }
        public string AppBundle { get; set; }
        public string Secret { get; set; }
        public string SecretKeyParam { get; set; }
        public string RemotePath { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
