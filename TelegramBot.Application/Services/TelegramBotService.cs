﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Application
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly TelegramCommandHandler _commandHandler;

        public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, TelegramCommandHandler commandHandler)
        {
            _botClient = botClient;
            _logger = logger;
            _commandHandler = commandHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                async (bot, update, token) => await _commandHandler.HandleUpdateAsync(update),
                async (bot, ex, source, token) => await HandleErrorAsync(bot, ex, source, token),
                receiverOptions,
                stoppingToken
            );

            _logger.LogInformation("Бот запущен через Long Polling");

            await Task.Delay(-1, stoppingToken);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            _logger.LogError("Ошибка Telegram API ({Source}): {ErrorMessage}", source, exception.Message);
            return Task.CompletedTask;
        }
    }

}
