using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Application.Interfaces;
using TelegramBot.Domain.Entities; // Contains BotUser and UserSession
using TelegramBot.Application.Services; // For SftpService
using TelegramBot.Data.Entities;         // For FileUploadLog
using TelegramBot.Data;                  // For ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace TelegramBot.Application.Handlers
{
    public class TelegramCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramCommandHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        // Session state stored per chatId
        private readonly Dictionary<long, UserSession> _sessions;

        public TelegramCommandHandler(ITelegramBotClient botClient, ILogger<TelegramCommandHandler> logger, IServiceProvider serviceProvider)
        {
            _botClient = botClient;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _sessions = new Dictionary<long, UserSession>();
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            long chatId = update.Message.Chat.Id;
            string username = update.Message.Chat.Username ?? $"User_{chatId}";
            string messageText = update.Message.Text.Trim();

            _logger.LogInformation("Received message: {MessageText} from chatId {ChatId}", messageText, chatId);

            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            BotUser botUser = await userService.GetUserAsync(chatId);
            if (botUser == null)
            {
                await userService.RegisterUserAsync(chatId, username);
                await _botClient.SendTextMessageAsync(chatId, "You are registered as Guest. Use /auth to authenticate.");
                return;
            }

            if (!_sessions.ContainsKey(chatId))
                _sessions[chatId] = new UserSession();
            var session = _sessions[chatId];

            // If session is in input mode, handle the input
            if (!string.IsNullOrEmpty(session.Step))
            {
                await HandleStepInput(chatId, session, messageText);
                return;
            }

            var parts = messageText.Split(" ");
            var command = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            switch (command)
            {
                case "/start":
                    await _botClient.SendTextMessageAsync(chatId, "Hello! Use /auth to authenticate.");
                    break;
                case "/auth":
                    if (botUser.Role != "Guest")
                        await _botClient.SendTextMessageAsync(chatId, "You are already authenticated.");
                    else
                    {
                        await userService.PromoteToUserAsync(chatId);
                        await _botClient.SendTextMessageAsync(chatId, "You are now authenticated as a User.");
                    }
                    break;
                case "/set_app":
                    if (botUser.Role == "Guest")
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Access denied. Authenticate using /auth.");
                        return;
                    }
                    session.Step = "set_app";
                    await _botClient.SendTextMessageAsync(chatId, "Enter AppName and AppBundle separated by a space:");
                    break;
                case "/set_sftp":
                    // If parameters are provided in the command
                    if (args.Length >= 3 && args.Length <= 4)
                    {
                        session.SftpHost = args[0];
                        session.SftpUsername = args[1];
                        session.SftpPassword = args[2];
                        session.SftpPort = (args.Length == 4 && int.TryParse(args[3], out int port)) ? port : 22;
                        await _botClient.SendTextMessageAsync(chatId, $"✅ SFTP host: `{session.SftpHost}`, port: `{session.SftpPort}`", parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        session.Step = "set_sftp";
                        await _botClient.SendTextMessageAsync(chatId, "Enter SFTP host, login, password, and optionally port separated by spaces:");
                    }
                    break;
                case "/upload":
                    await HandleUploadCommand(chatId, botUser, session);
                    break;
                case "/set_role":
                    await HandleSetRoleCommand(chatId, args);
                    break;
                case "/last_uploads":
                    await HandleLastUploadsCommand(chatId);
                    break;
                case "/list_users":
                    await HandleListUsersCommand(chatId);
                    break;
                case "/menu":
                    await HandleMenuCommand(chatId, botUser);
                    break;
                case "/admin":
                    await HandleAdminCommand(chatId, botUser);
                    break;
                case "/become_admin":
                    await HandleBecomeAdminCommand(chatId, args, botUser, userService);
                    break;
                default:
                    await _botClient.SendTextMessageAsync(chatId, "Unknown command. Use /menu to see available commands.");
                    break;
            }
        }

        // Handles input when session.Step is set (e.g., after /set_app or /set_sftp)
        private async Task HandleStepInput(long chatId, UserSession session, string input)
        {
            var args = input.Split(" ");
            if (session.Step == "set_app")
            {
                if (args.Length < 2)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Error: Enter AppName and AppBundle separated by a space.");
                    return;
                }
                session.AppName = args[0];
                session.AppBundle = args[1];
                session.Secret = Guid.NewGuid().ToString("N").Substring(0, 16);
                session.SecretKeyParam = $"key{new Random().Next(1000, 9999)}";
                session.Step = null;
                string phpScript = GeneratePhpScript(session);
                await _botClient.SendTextMessageAsync(chatId,
                    $"✅ Parameters set.\n🔑 Secret: `{session.Secret}`\n🔑 Secret-Key-Param: `{session.SecretKeyParam}`\n\nPHP Script:\n```php\n{phpScript}```",
                    parseMode: ParseMode.Markdown);
            }
            else if (session.Step == "set_sftp")
            {
                if (args.Length < 3 || args.Length > 4)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Error: Enter SFTP host, login, password and optionally port separated by spaces.");
                    return;
                }
                session.SftpHost = args[0];
                session.SftpUsername = args[1];
                session.SftpPassword = args[2];
                session.SftpPort = (args.Length == 4 && int.TryParse(args[3], out int port)) ? port : 22;
                session.Step = null;
                await _botClient.SendTextMessageAsync(chatId, $"✅ SFTP host: `{session.SftpHost}`, port: `{session.SftpPort}`", parseMode: ParseMode.Markdown);
            }
        }

        // Handles the /upload command
        private async Task HandleUploadCommand(long chatId, BotUser botUser, UserSession session)
        {
            if (botUser.Role == "Guest")
            {
                await _botClient.SendTextMessageAsync(chatId, "Access denied: Guests cannot upload files.");
                return;
            }
            if (string.IsNullOrEmpty(session.AppName) ||
                string.IsNullOrEmpty(session.AppBundle) ||
                string.IsNullOrEmpty(session.Secret))
            {
                await _botClient.SendTextMessageAsync(chatId, "Error: Set AppName, AppBundle and Secret using /set_app.");
                return;
            }
            if (string.IsNullOrEmpty(session.SftpHost) ||
                string.IsNullOrEmpty(session.SftpUsername) ||
                string.IsNullOrEmpty(session.SftpPassword))
            {
                await _botClient.SendTextMessageAsync(chatId, "Error: Set SFTP parameters using /set_sftp.");
                return;
            }

            string phpScript = GeneratePhpScript(session);
            // Use a relative path; home directory for SFTP user is the root inside the session
            string remotePath = $"upload/{session.AppName}.php";

            using (var sftpScope = _serviceProvider.CreateScope())
            {
                var sftpService = sftpScope.ServiceProvider.GetRequiredService<SftpService>();
                bool uploadResult = await sftpService.UploadFileAsync(
                    session.SftpHost,
                    session.SftpPort,
                    session.SftpUsername,
                    session.SftpPassword,
                    remotePath,
                    phpScript);
                if (uploadResult)
                {
                    await _botClient.SendTextMessageAsync(chatId, "✅ File uploaded to server!");

                    // Log the upload using IUploadLogService
                    using (var logScope = _serviceProvider.CreateScope())
                    {
                        var uploadLogService = logScope.ServiceProvider.GetRequiredService<IUploadLogService>();
                        await uploadLogService.LogUploadAsync(new FileUploadLog
                        {
                            TelegramId = chatId,
                            AppName = session.AppName,
                            AppBundle = session.AppBundle,
                            Secret = session.Secret,
                            SecretKeyParam = session.SecretKeyParam,
                            RemotePath = remotePath,
                            UploadDate = DateTime.UtcNow
                        });
                    }

                    // Save file locally
                    try
                    {
                        string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
                        if (!Directory.Exists(localDirectory))
                        {
                            Directory.CreateDirectory(localDirectory);
                        }
                        string localFilePath = Path.Combine(localDirectory, $"{session.AppName}.php");
                        System.IO.File.WriteAllText(localFilePath, phpScript, Encoding.UTF8);
                        await _botClient.SendTextMessageAsync(chatId, $"✅ File also saved locally: {localFilePath}");
                    }
                    catch (Exception ex)
                    {
                        await _botClient.SendTextMessageAsync(chatId, $"⚠️ File uploaded, but local save failed: {ex.Message}");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Error uploading file via SFTP.");
                }
            }
        }

        // Handles the /set_role command
        private async Task HandleSetRoleCommand(long chatId, string[] args)
        {
            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            BotUser botUser = await userService.GetUserAsync(chatId);
            if (botUser?.Role != "Admin")
            {
                await _botClient.SendTextMessageAsync(chatId, "Access denied: only Admins can set roles.");
                return;
            }
            if (args.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "Usage: /set_role <TelegramId> <Role>");
                return;
            }
            if (!long.TryParse(args[0], out long targetTelegramId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Error: TelegramId must be a number.");
                return;
            }
            string newRole = args[1];
            if (newRole != "User" && newRole != "Admin")
            {
                await _botClient.SendTextMessageAsync(chatId, "Error: Role can only be 'User' or 'Admin'.");
                return;
            }
            bool success = await userService.SetUserRoleAsync(targetTelegramId, newRole);
            if (success)
                await _botClient.SendTextMessageAsync(chatId, $"✅ User {targetTelegramId} role set to {newRole}.");
            else
                await _botClient.SendTextMessageAsync(chatId, $"❌ User with TelegramId {targetTelegramId} not found.");
        }

        // Handles the /last_uploads command
        private async Task HandleLastUploadsCommand(long chatId)
        {
            using var scope = _serviceProvider.CreateScope();
            var uploadLogService = scope.ServiceProvider.GetRequiredService<IUploadLogService>();
            var uploads = await uploadLogService.GetLastUploadsAsync(10);
            if (uploads.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "No uploads to display.");
            }
            else
            {
                string message = "Recent uploads:\n";
                foreach (var upload in uploads)
                {
                    message += $"- {upload.UploadDate:u}: {upload.AppName} ({upload.RemotePath})\n";
                }
                await _botClient.SendTextMessageAsync(chatId, message);
            }
        }

        // Handles the /admin command (simple admin menu welcome)
        private async Task HandleAdminCommand(long chatId, BotUser botUser)
        {
            if (botUser.Role != "Admin")
            {
                await _botClient.SendTextMessageAsync(chatId, "Access denied: you are not an Admin.");
                return;
            }
            await _botClient.SendTextMessageAsync(chatId, "✅ Welcome to the Admin panel.");
        }

        // Command for user to become admin by providing a secret
        private async Task HandleBecomeAdminCommand(long chatId, string[] args, BotUser botUser, IUserService userService)
        {
            if (botUser.Role == "Admin")
            {
                await _botClient.SendTextMessageAsync(chatId, "You are already an Admin.");
                return;
            }
            if (args.Length < 1)
            {
                await _botClient.SendTextMessageAsync(chatId, "Usage: /become_admin <admin_secret>");
                return;
            }
            string providedSecret = args[0];
            const string adminSecret = "SuperSecret"; // Replace or load from configuration
            if (providedSecret == adminSecret)
            {
                bool success = await userService.SetUserRoleAsync(chatId, "Admin");
                if (success)
                    await _botClient.SendTextMessageAsync(chatId, "✅ You are now an Admin.");
                else
                    await _botClient.SendTextMessageAsync(chatId, "❌ Error setting admin role.");
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Incorrect secret.");
            }
        }

        // Command to list all users (admin-only)
        private async Task HandleListUsersCommand(long chatId)
        {
            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            BotUser currentUser = await userService.GetUserAsync(chatId);
            if (currentUser?.Role != "Admin")
            {
                await _botClient.SendTextMessageAsync(chatId, "Access denied: only Admins can view the user list.");
                return;
            }
            // Assume IUserService provides a method to get all users; if not, you might need to inject a repository
            var users = await userService.GetAllUsersAsync();
            if (users == null || !users.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "No users found.");
            }
            else
            {
                string message = "Registered users:\n";
                foreach (var u in users)
                {
                    message += $"- TelegramId: {u.TelegramId}, Username: {u.Username}, Role: {u.Role}\n";
                }
                await _botClient.SendTextMessageAsync(chatId, message);
            }
        }

        // Command to display a simple menu of commands
        private async Task HandleMenuCommand(long chatId, BotUser botUser)
        {
            // Build a menu message based on the user's role
            string menu = "Available commands:\n" +
                          "/start - Start interaction\n" +
                          "/auth - Authenticate\n" +
                          "/set_app - Set application parameters\n" +
                          "/set_sftp - Set SFTP parameters\n" +
                          "/upload - Upload PHP script\n";
            if (botUser.Role == "Admin")
            {
                menu += "/set_role - Set role for a user\n" +
                        "/last_uploads - Show last 10 uploads\n" +
                        "/list_users - List all users\n" +
                        "/admin - Admin panel\n";
            }
            menu += "/become_admin - Become admin (with secret)\n";
            await _botClient.SendTextMessageAsync(chatId, menu);
        }

        private string GeneratePhpScript(UserSession session)
        {
            return $@"<?php
$appName = '{session.AppName}';
$appBundle = '{session.AppBundle}';
$secretKey = '{session.Secret}';

if (isset($_GET['{session.SecretKeyParam}']) && $_GET['{session.SecretKeyParam}'] === $secretKey) {{
    echo 'Hello, I am ' . $appName . ', my link: https://play.google.com/store/apps/details?id=' . $appBundle;
}}";
        }
    }
}
