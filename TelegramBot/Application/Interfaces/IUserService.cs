using System.Threading.Tasks;
using TelegramBot.Data.Entities;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Application.Interfaces
{
    public interface IUserService
    {
        Task<BotUser> GetUserAsync(long telegramId);
        Task<bool> IsAdminAsync(long telegramId);
        Task RegisterUserAsync(long telegramId, string username);
        Task PromoteToUserAsync(long telegramId);
        Task<bool> SetUserRoleAsync(long telegramId, string newRole);
        Task<List<BotUser>> GetAllUsersAsync();
    }
}
