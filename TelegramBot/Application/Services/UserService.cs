using Microsoft.EntityFrameworkCore;
using TelegramBot.Application.Interfaces;
using TelegramBot.Data.Entities;
using TelegramBot.Data.Interfaces;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public async Task<BotUser> GetUserAsync(long telegramId)
    {
        return await _userRepository.GetUserAsync(telegramId);
    }
    public async Task<bool> IsAdminAsync(long telegramId)
    {
        var user = await GetUserAsync(telegramId);
        return user?.Role == "Admin";
    }
    public async Task RegisterUserAsync(long telegramId, string username)
    {
        var user = await GetUserAsync(telegramId);
        if (user == null)
        {
            user = new BotUser { TelegramId = telegramId, Username = username, Role = "Guest" };
            await _userRepository.AddUserAsync(user);
        }
    }
    public async Task PromoteToUserAsync(long telegramId)
    {
        var user = await GetUserAsync(telegramId);
        if (user != null && user.Role == "Guest")
        {
            user.Role = "User";
            await _userRepository.UpdateUserAsync(user);
        }
    }
    public async Task<List<BotUser>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllUsersAsync();
    }
    public async Task<bool> SetUserRoleAsync(long telegramId, string newRole)
    {
        var user = await GetUserAsync(telegramId);
        if (user != null)
        {
            user.Role = newRole;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }
        return false;
    }
}
