using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TelegramBot.Application.Interfaces;
using TelegramBot.Data;
using TelegramBot.Data.Entities;

namespace TelegramBot.Application.Services
{
    public class UploadLogService : IUploadLogService
    {
        private readonly ApplicationDbContext _context;
        public UploadLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogUploadAsync(FileUploadLog log)
        {
            _context.FileUploadLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FileUploadLog>> GetLastUploadsAsync(int count)
        {
            return await _context.FileUploadLogs
                .OrderByDescending(l => l.UploadDate)
                .Take(count)
                .ToListAsync();
        }
    }
}
