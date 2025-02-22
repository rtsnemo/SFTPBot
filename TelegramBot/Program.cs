using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBot.Api.Services;
using TelegramBot.Application.Handlers;
using TelegramBot.Application.Interfaces;
using TelegramBot.Application.Services;
using TelegramBot.Configurations;
using TelegramBot.Data;
using TelegramBot.Data.Interfaces;

namespace TelegramBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            StartupConfiguration.ConfigureServices(builder);

            var app = builder.Build();

            MigrationManager.ApplyMigrations(app);
/*
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }*/

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
