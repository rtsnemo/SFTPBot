using TelegramBot.Data.Entities;

namespace TelegramBot.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<BotUser> GetUserAsync(long telegramId);
        Task AddUserAsync(BotUser user);
        Task UpdateUserAsync(BotUser user);
        Task<List<BotUser>> GetAllUsersAsync();
    }

}
