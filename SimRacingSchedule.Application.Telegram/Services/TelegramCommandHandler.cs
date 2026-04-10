// SimRacingSchedule.Application.Telegram/Services/TelegramCommandHandler.cs
using Microsoft.Extensions.Logging;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimRacingSchedule.Application.Telegram.Services;

public class TelegramCommandHandler
{
    private readonly ITelegramBotClient m_BotClient;
    private readonly ITelegramUserSettingsRepository m_SettingsRepository;
    private readonly IEmployeeRepository m_EmployeeRepository;
    private readonly ITelegramNotificationService m_NotificationService;
    private readonly ILogger<TelegramCommandHandler> m_Logger;

    public TelegramCommandHandler(
        ITelegramBotClient botClient,
        ITelegramUserSettingsRepository settingsRepository,
        IEmployeeRepository employeeRepository,
        ITelegramNotificationService notificationService,
        ILogger<TelegramCommandHandler> logger)
    {
        m_BotClient = botClient;
        m_SettingsRepository = settingsRepository;
        m_EmployeeRepository = employeeRepository;
        m_NotificationService = notificationService;
        m_Logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message != null)
                        await HandleMessageAsync(update.Message, ct);
                    break;
                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery != null)
                        await HandleCallbackQueryAsync(update.CallbackQuery, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error handling update");
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (message.Text == null) return;

        long chatId = message.Chat.Id;
        string text = message.Text.Trim();

        if (text.StartsWith("/"))
        {
            await HandleCommandAsync(chatId, text, message.From!, ct);
        }
    }

    private async Task HandleCommandAsync(long chatId, string command, User from, CancellationToken ct)
    {
        TelegramUserSettings? settings = await m_SettingsRepository.GetByChatIdAsync(chatId, ct);
        
        switch (command.ToLower())
        {
            case "/start":
                await HandleStartCommand(chatId, from, ct);
                break;
                
            case "/settings":
                if (settings != null)
                    await m_NotificationService.SendSettingsMessageAsync(chatId, settings, ct);
                else
                    await SendNotRegisteredMessage(chatId, ct);
                break;
                
            case "/enable":
                if (settings != null)
                {
                    settings.IsEnabled = true;
                    await m_SettingsRepository.UpdateAsync(settings, ct);
                    await m_SettingsRepository.SaveChangesAsync(ct);
                    await m_BotClient.SendMessage(chatId, "✅ Уведомления включены!", cancellationToken: ct);
                }
                break;
                
            case "/disable":
                if (settings != null)
                {
                    settings.IsEnabled = false;
                    await m_SettingsRepository.UpdateAsync(settings, ct);
                    await m_SettingsRepository.SaveChangesAsync(ct);
                    await m_BotClient.SendMessage(chatId, "❌ Уведомления отключены!", cancellationToken: ct);
                }
                break;
                
            case "/status":
                await SendStatusMessage(chatId, settings, ct);
                break;
                
            case "/help":
                await SendHelpMessage(chatId, ct);
                break;
                
            default:
                if (command.StartsWith("/time "))
                {
                    await HandleTimeCommand(chatId, command, settings, ct);
                }
                else if (command.StartsWith("/timezone "))
                {
                    await HandleTimeZoneCommand(chatId, command, settings, ct);
                }
                else
                {
                    await m_BotClient.SendMessage(chatId, "❓ Неизвестная команда. Используйте /help", cancellationToken: ct);
                }
                break;
        }
    }

    private async Task HandleStartCommand(long chatId, User from, CancellationToken ct)
    {
        // Ищем сотрудника по Telegram ID или имени
        Employee? employee = await FindEmployeeByTelegramInfo(from);
        
        if (employee == null)
        {
            await m_BotClient.SendMessage(
                chatId: chatId,
                text: "❌ Ваш аккаунт не привязан к системе. Обратитесь к администратору.",
                cancellationToken: ct);
            return;
        }

        TelegramUserSettings? existingSettings = await m_SettingsRepository.GetByEmployeeIdAsync(employee.Id, ct);
        
        if (existingSettings == null)
        {
            TelegramUserSettings newSettings = new()
            {
                EmployeeId = employee.Id,
                TelegramChatId = chatId
            };
            await m_SettingsRepository.AddAsync(newSettings, ct);
            await m_SettingsRepository.SaveChangesAsync(ct);
        }
        else if (existingSettings.TelegramChatId != chatId)
        {
            existingSettings.TelegramChatId = chatId;
            await m_SettingsRepository.UpdateAsync(existingSettings, ct);
            await m_SettingsRepository.SaveChangesAsync(ct);
        }

        await m_NotificationService.SendWelcomeMessageAsync(chatId, $"{employee.FirstName} {employee.LastName}", ct);
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        long chatId = callbackQuery.Message!.Chat.Id;
        string? data = callbackQuery.Data;

        TelegramUserSettings? settings = await m_SettingsRepository.GetByChatIdAsync(chatId, ct);
        if (settings == null) return;

        switch (data)
        {
            case "settings":
                await m_NotificationService.SendSettingsMessageAsync(chatId, settings, ct);
                break;
            case "help":
                await SendHelpMessage(chatId, ct);
                break;
            case "enable":
                settings.IsEnabled = true;
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, "✅ Уведомления включены!", cancellationToken: ct);
                break;
            case "disable":
                settings.IsEnabled = false;
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, "❌ Уведомления отключены!", cancellationToken: ct);
                break;
            case "time_30":
                settings.NotificationMinutesBefore = 30;
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, "⏰ Время напоминания установлено: за 30 минут", cancellationToken: ct);
                break;
            case "time_60":
                settings.NotificationMinutesBefore = 60;
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, "⏰ Время напоминания установлено: за 60 минут", cancellationToken: ct);
                break;
            case "time_120":
                settings.NotificationMinutesBefore = 120;
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, "⏰ Время напоминания установлено: за 120 минут", cancellationToken: ct);
                break;
        }

        await m_BotClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }

    private async Task<Employee?> FindEmployeeByTelegramInfo(User from)
    {
        // Пробуем найти по Telegram ID (если сохраняли)
        // Сначала проверяем, есть ли в настройках привязка
        TelegramUserSettings? settings = await m_SettingsRepository.GetByChatIdAsync(from.Id);
        if (settings != null)
        {
            return await m_EmployeeRepository.GetByIdAsync(settings.EmployeeId);
        }

        // Если не нашли, пробуем по имени и фамилии
        IEnumerable<Employee> employees = await m_EmployeeRepository.GetAllAsync();
        
        return employees.FirstOrDefault(e => 
            !string.IsNullOrEmpty(from.FirstName) && 
            e.FirstName.Equals(from.FirstName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(from.LastName) && 
            e.LastName.Equals(from.LastName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendNotRegisteredMessage(long chatId, CancellationToken ct)
    {
        await m_BotClient.SendMessage(
            chatId: chatId,
            text: "❌ Вы не зарегистрированы. Используйте /start для регистрации.",
            cancellationToken: ct);
    }

    private async Task SendStatusMessage(long chatId, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null)
        {
            await SendNotRegisteredMessage(chatId, ct);
            return;
        }

        string status = settings.IsEnabled ? "✅ Включены" : "❌ Отключены";
        await m_BotClient.SendMessage(
            chatId: chatId,
            text: $"""
            📊 *Статус уведомлений:* {status}
            ⏰ *Напоминание за:* {settings.NotificationMinutesBefore} мин.
            📅 *О начале смен:* {(settings.NotifyShiftStart ? "Да" : "Нет")}
            🔄 *Об обмене:* {(settings.NotifyShiftExchange ? "Да" : "Нет")}
            🌍 *Часовой пояс:* {settings.TimeZone}
            """,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task SendHelpMessage(long chatId, CancellationToken ct)
    {
        await m_BotClient.SendMessage(
            chatId: chatId,
            text: """
            🤖 *Доступные команды:*
            
            /start - начать работу с ботом
            /settings - настроить уведомления
            /status - проверить статус
            /enable - включить уведомления
            /disable - отключить уведомления
            /time [минуты] - изменить время напоминания
            /timezone [зона] - изменить часовой пояс
            /help - показать эту справку
            
            *Примеры:*
            /time 30 - напоминать за 30 минут
            /timezone Europe/Moscow - установить московское время
            """,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandleTimeCommand(long chatId, string command, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null)
        {
            await SendNotRegisteredMessage(chatId, ct);
            return;
        }

        string[] parts = command.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[1], out int minutes) && minutes > 0 && minutes <= 1440)
        {
            settings.NotificationMinutesBefore = minutes;
            await m_SettingsRepository.UpdateAsync(settings, ct);
            await m_SettingsRepository.SaveChangesAsync(ct);
            await m_BotClient.SendMessage(chatId, $"⏰ Время напоминания установлено: за {minutes} минут", cancellationToken: ct);
        }
        else
        {
            await m_BotClient.SendMessage(chatId, "❌ Неверный формат. Используйте: /time [минуты] (1-1440)", cancellationToken: ct);
        }
    }

    private async Task HandleTimeZoneCommand(long chatId, string command, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null)
        {
            await SendNotRegisteredMessage(chatId, ct);
            return;
        }

        string[] parts = command.Split(' ');
        if (parts.Length == 2)
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(parts[1]);
                settings.TimeZone = parts[1];
                await m_SettingsRepository.UpdateAsync(settings, ct);
                await m_SettingsRepository.SaveChangesAsync(ct);
                await m_BotClient.SendMessage(chatId, $"🌍 Часовой пояс установлен: {tz.DisplayName}", cancellationToken: ct);
            }
            catch
            {
                await m_BotClient.SendMessage(chatId, "❌ Неверный часовой пояс. Примеры: Europe/Moscow, Asia/Yekaterinburg", cancellationToken: ct);
            }
        }
        else
        {
            await m_BotClient.SendMessage(chatId, "❌ Неверный формат. Используйте: /timezone [зона]", cancellationToken: ct);
        }
    }
}
