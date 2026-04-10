using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace SimRacingSchedule.Application.Telegram.Services;

public class TelegramBotHostedService : BackgroundService
{
    private readonly ITelegramBotClient m_BotClient;
    private readonly IServiceProvider m_ServiceProvider;
    private readonly ILogger<TelegramBotHostedService> m_Logger;

    public TelegramBotHostedService(
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider,
        ILogger<TelegramBotHostedService> logger)
    {
        m_BotClient = botClient;
        m_ServiceProvider = serviceProvider;
        m_Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        m_Logger.LogInformation("Telegram Bot Hosted Service started");

        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        using IServiceScope scope = m_ServiceProvider.CreateScope();
        TelegramCommandHandler commandHandler = scope.ServiceProvider.GetRequiredService<TelegramCommandHandler>();

        m_BotClient.StartReceiving(
            updateHandler: async (bot, update, ct) =>
            {
                await commandHandler.HandleUpdateAsync(update, ct);
            },
            errorHandler: async (bot, exception, ct) =>
            {
                m_Logger.LogError(exception, "Telegram Bot error");
                await Task.CompletedTask;
            },
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        m_Logger.LogInformation("Telegram Bot is listening for updates");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}