using Microsoft.EntityFrameworkCore;
using TelegramBot.Data.Entities;

namespace TelegramBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<BotUser> Users { get; set; }
        public DbSet<FileUploadLog> FileUploadLogs { get; set; }
    }
}
