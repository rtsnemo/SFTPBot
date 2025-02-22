using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Application.Handlers;

namespace TelegramBot.Api.Services
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, IServiceProvider serviceProvider)
        {
            _botClient = botClient;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                async (bot, update, token) =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var commandHandler = scope.ServiceProvider.GetRequiredService<TelegramCommandHandler>();
                    await commandHandler.HandleUpdateAsync(update);
                },
                HandleErrorAsync,
                receiverOptions,
                stoppingToken
            );

            _logger.LogInformation("Bot started using Long Polling");
            await Task.Delay(-1, stoppingToken);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            _logger.LogError("Telegram API error ({Source}): {ErrorMessage}", source, exception.Message);
            return Task.CompletedTask;
        }
    }
}
