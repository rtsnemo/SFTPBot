using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBot.Api.Services;
using TelegramBot.Application.Handlers;
using TelegramBot.Application.Interfaces;
using TelegramBot.Application.Services;
using TelegramBot.Data;
using TelegramBot.Data.Entities;
using TelegramBot.Data.Interfaces;

namespace TelegramBot
{
    public static class StartupConfiguration
    {
        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            IConfiguration configuration = builder.Configuration;
            string connectionString = configuration.GetConnectionString("PostgresDb");

            // Register ApplicationDbContext as Scoped
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Register Telegram Bot Client as Singleton
            string botToken = configuration["TelegramBot:Token"]
                ?? throw new Exception("Telegram Bot Token not found!");
            builder.Services.AddSingleton<ITelegramBotClient>(provider =>
                new TelegramBotClient(botToken));

            // Register repositories and domain services as Scoped
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUploadLogService, UploadLogService>();

            // Register SFTP service as Singleton
            builder.Services.AddSingleton<SftpService>();

            // Register TelegramCommandHandler as Singleton; it uses the root service provider for scoped dependencies
            builder.Services.AddSingleton<TelegramCommandHandler>(provider =>
            {
                var botClient = provider.GetRequiredService<ITelegramBotClient>();
                var logger = provider.GetRequiredService<ILogger<TelegramCommandHandler>>();
                return new TelegramCommandHandler(botClient, logger, provider);
            });

            // Register TelegramBotService as Singleton and Hosted Service
            builder.Services.AddSingleton<TelegramBotService>();
            builder.Services.AddHostedService<TelegramBotService>();

            // Register controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }
    }
}
