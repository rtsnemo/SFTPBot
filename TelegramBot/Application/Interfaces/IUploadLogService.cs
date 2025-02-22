using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramBot.Data.Entities;

namespace TelegramBot.Application.Interfaces
{
    public interface IUploadLogService
    {
        Task LogUploadAsync(FileUploadLog log);
        Task<List<FileUploadLog>> GetLastUploadsAsync(int count);
    }
}
