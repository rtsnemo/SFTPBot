using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;
using TelegramBot.Application.Interfaces;

namespace TelegramBot.Application.Interfaces
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;

        public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Message != null && update.Message.Text != null)
            {
                long chatId = update.Message.Chat.Id;
                string messageText = update.Message.Text;

                _logger.LogInformation("Получено сообщение: {MessageText} от {ChatId}", messageText, chatId);

                if (messageText == "/start")
                {
                    await _botClient.SendTextMessageAsync(chatId, "Привет! Я Telegram-бот.");
                }
            }
        }
    }
}   