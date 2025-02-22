using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;

namespace TelegramBot.Application
{
    public class TelegramCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramCommandHandler> _logger;
        private readonly Dictionary<long, UserSession> _sessions;

        public TelegramCommandHandler(ITelegramBotClient botClient, ILogger<TelegramCommandHandler> logger)
        {
            _botClient = botClient;
            _logger = logger;
            _sessions = new Dictionary<long, UserSession>();
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            long chatId = update.Message.Chat.Id;
            string messageText = update.Message.Text;

            _logger.LogInformation("Получено сообщение: {MessageText} от {ChatId}", messageText, chatId);

            if (!_sessions.ContainsKey(chatId))
                _sessions[chatId] = new UserSession();

            var session = _sessions[chatId];

            switch (messageText.Split(" ")[0])
            {
                case "/start":
                    await _botClient.SendTextMessageAsync(chatId, "Привет! Используй /set_app для указания параметров.");
                    break;

                case "/set_app":
                    session.Step = "set_app";
                    await _botClient.SendTextMessageAsync(chatId, "Введите AppName и AppBundle через пробел:");
                    break;

                case "/generate_secret":
                    session.Secret = Guid.NewGuid().ToString("N").Substring(0, 16);
                    session.SecretKeyParam = $"key{new Random().Next(1000, 9999)}";
                    await _botClient.SendTextMessageAsync(chatId, $"🔑 Секретный ключ: `{session.Secret}`\n🔑 Параметр: `{session.SecretKeyParam}`", parseMode: ParseMode.Markdown);
                    break;

                case "/set_sftp":
                    session.Step = "set_sftp";
                    await _botClient.SendTextMessageAsync(chatId, "Введите SFTP-хост, логин и пароль через пробел:");
                    break;

                case "/upload":
                    if (string.IsNullOrEmpty(session.AppName) || string.IsNullOrEmpty(session.AppBundle) || string.IsNullOrEmpty(session.Secret))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "❌ Сначала настройте AppName, AppBundle и Secret!");
                        return;
                    }

                    string phpScript = GeneratePhpScript(session);
                    await _botClient.SendTextMessageAsync(chatId, $"✅ PHP-скрипт сгенерирован:\n\n```php\n{phpScript}```", parseMode: ParseMode.Markdown);
                    break;

                default:
                    if (session.Step == "set_app")
                    {
                        var parts = messageText.Split(" ");
                        if (parts.Length < 2)
                        {
                            await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка! Введите AppName и AppBundle через пробел.");
                            return;
                        }

                        session.AppName = parts[0];
                        session.AppBundle = parts[1];
                        session.Step = null;
                        await _botClient.SendTextMessageAsync(chatId, $"✅ AppName: `{session.AppName}`\n✅ AppBundle: `{session.AppBundle}`", parseMode: ParseMode.Markdown);
                    }
                    else if (session.Step == "set_sftp")
                    {
                        var parts = messageText.Split(" ");
                        if (parts.Length < 3)
                        {
                            await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка! Введите SFTP-хост, логин и пароль через пробел.");
                            return;
                        }

                        session.SftpHost = parts[0];
                        session.SftpUsername = parts[1];
                        session.SftpPassword = parts[2];
                        session.Step = null;
                        await _botClient.SendTextMessageAsync(chatId, $"✅ SFTP-хост: `{session.SftpHost}`", parseMode: ParseMode.Markdown);
                    }
                    break;
            }
        }

        private string GeneratePhpScript(UserSession session)
        {
            return $@"<?php
$appName = '{session.AppName}';
$appBundle = '{session.AppBundle}';
$secretKey = '{session.Secret}';

if($secretKey == $_GET['{session.SecretKeyParam}']) {{
    echo 'Привет, я приложение ' . $appName . ', моя ссылка: https://play.google.com/store/apps/details?id=' . $appBundle;
}}";
        }
    }

    public class UserSession
    {
        public string AppName { get; set; }
        public string AppBundle { get; set; }
        public string Secret { get; set; }
        public string SecretKeyParam { get; set; }
        public string SftpHost { get; set; }
        public string SftpUsername { get; set; }
        public string SftpPassword { get; set; }
        public string Step { get; set; }
    }
}
