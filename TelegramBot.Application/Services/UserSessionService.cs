using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TelegramBot.Application.Interfaces;

namespace TelegramBot.Application.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

        public Task<UserSession> GetSessionAsync(long chatId)
        {
            return Task.FromResult(_sessions.GetOrAdd(chatId, new UserSession()));
        }

        public Task SaveSessionAsync(long chatId, UserSession session)
        {
            _sessions[chatId] = session;
            return Task.CompletedTask;
        }
    }

}
