using Telegram.Bot.Types;
using System.Threading.Tasks;

namespace TelegramBot.Application.Interfaces
{
    public interface ITelegramBotService
    {
        Task HandleUpdateAsync(Update update);
    }
}
