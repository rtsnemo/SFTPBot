using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Application.Interfaces
{
    public interface IUserSessionService
    {
        Task<UserSession> GetSessionAsync(long chatId);
        Task SaveSessionAsync(long chatId, UserSession session);
    }

}
