using TelegramBot.Data.Interfaces;
using TelegramBot.Data;
using TelegramBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<BotUser> GetUserAsync(long telegramId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }
    public async Task<List<BotUser>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }
    public async Task AddUserAsync(BotUser user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateUserAsync(BotUser user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
