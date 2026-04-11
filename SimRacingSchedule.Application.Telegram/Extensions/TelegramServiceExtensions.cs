using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimRacingSchedule.Application.Telegram.Background;
using SimRacingSchedule.Application.Telegram.Services;
using Telegram.Bot;

namespace SimRacingSchedule.Application.Telegram.Extensions;

public static class TelegramServiceExtensions
{
    public static IServiceCollection AddTelegramServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? botToken = configuration["Telegram:BotToken"];
        if (string.IsNullOrEmpty(botToken))
            throw new ArgumentException("Telegram Bot Token is not configured");

        // Register HttpClient and Bot Client
        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>(httpClient 
                => new TelegramBotClient(botToken, httpClient));

        // Register services
        services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
        services.AddScoped<TelegramCommandHandler>();
        
        // Register hosted services
        services.AddHostedService<TelegramBotHostedService>();
        services.AddHostedService<ShiftReminderBackgroundService>();
        
        return services;
    }
}