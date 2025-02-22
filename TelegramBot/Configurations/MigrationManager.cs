using Microsoft.EntityFrameworkCore;
using TelegramBot.Data;

namespace TelegramBot.Configurations
{
    public static class MigrationManager
    {
        public static void ApplyMigrations(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        }
    }
}
