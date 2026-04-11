using Microsoft.Extensions.Logging;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimRacingSchedule.Application.Telegram.Services;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly ITelegramBotClient m_BotClient;
    private readonly ITelegramUserSettingsRepository m_SettingsRepository;
    private readonly ILogger<TelegramNotificationService> m_Logger;

    public TelegramNotificationService(
        ITelegramBotClient botClient,
        ITelegramUserSettingsRepository settingsRepository,
        ILogger<TelegramNotificationService> logger)
    {
        m_BotClient = botClient;
        m_SettingsRepository = settingsRepository;
        m_Logger = logger;
    }

    public async Task SendShiftReminderAsync(Shift shift, Employee employee, int minutesBefore, CancellationToken ct = default)
    {
        TelegramUserSettings? settings = await m_SettingsRepository.GetByEmployeeIdAsync(employee.Id, ct);
        if (settings == null || !settings.IsEnabled || !settings.NotifyShiftReminder)
            return;

        // DateTime localStartTime = ConvertToLocalTime(shift.StartTime, settings.TimeZone);
        // DateTime localEndTime = ConvertToLocalTime(shift.EndTime, settings.TimeZone);

            // Форматируем время правильно
        // string startTimeStr = localStartTime.ToString("HH:mm");
        // string endTimeStr = localEndTime.ToString("HH:mm");
        // string dateStr = localStartTime.ToString("dd.MM.yyyy");

        // Для FullDay смены показываем правильное время
        string shiftHours = shift.Type == ShiftType.FullDay ? "13:00-22:00" : shift.Type.GetDisplayName();

        string message = $"""
        ⏰ *НАПОМИНАНИЕ О СМЕНЕ!*
        
        👤 *Сотрудник:* {employee.LastName} {employee.FirstName}
        📅 *Дата:* {shift.StartTime.ToString("dd.MM.yyyy")}
        🕐 *Время:* {shift.StartTime.ToString("HH:mm")} - {shift.EndTime.ToString("HH:mm")} (МСК)
        📋 *Тип смены:* {shift.Type.GetDisplayName()}
        
        ⚠️ *До начала смены осталось {minutesBefore} минут!*
        
        Хорошей смены! 🏎️
        """;

        await SendMessageAsync(settings.TelegramChatId, message, ct);
    }

    public async Task SendShiftExchangeNotificationAsync(ShiftExchangeRequest request, string action, CancellationToken ct = default)
{
    m_Logger.LogInformation("📨 Sending exchange notification. Action: {Action}, RequesterId: {RequesterId}, TargetId: {TargetId}", 
        action, request.RequesterId, request.TargetId);

        // Уведомление для requester
        TelegramUserSettings? requesterSettings = await m_SettingsRepository.GetByEmployeeIdAsync(request.RequesterId, ct);
    m_Logger.LogInformation("Requester settings: Found={Found}, Enabled={Enabled}, ChatId={ChatId}", 
        requesterSettings != null, requesterSettings?.IsEnabled, requesterSettings?.TelegramChatId);
    
    if (requesterSettings?.IsEnabled == true && requesterSettings.NotifyShiftExchange)
    {
            string message = GetShiftExchangeMessage(request, action, isForRequester: true);
        await SendMessageAsync(requesterSettings.TelegramChatId, message, ct);
        m_Logger.LogInformation("✅ Message sent to requester {ChatId}", requesterSettings.TelegramChatId);
    }

        // Уведомление для target
        TelegramUserSettings? targetSettings = await m_SettingsRepository.GetByEmployeeIdAsync(request.TargetId, ct);
    m_Logger.LogInformation("Target settings: Found={Found}, Enabled={Enabled}, ChatId={ChatId}", 
        targetSettings != null, targetSettings?.IsEnabled, targetSettings?.TelegramChatId);
    
    if (targetSettings?.IsEnabled == true && targetSettings.NotifyShiftExchange)
    {
            string message = GetShiftExchangeMessage(request, action, isForRequester: false);
        await SendMessageAsync(targetSettings.TelegramChatId, message, ct);
        m_Logger.LogInformation("✅ Message sent to target {ChatId}", targetSettings.TelegramChatId);
    }
}

    public async Task SendWelcomeMessageAsync(long chatId, string employeeName, CancellationToken ct = default)
    {
        string message = $"""
        🎉 *Добро пожаловать в SimRacing Club Bot!*
        
        Привет, {employeeName}!
        
        Я буду присылать тебе уведомления о:
        • 📅 Начале смен
        • 🔄 Запросах на обмен сменами
        • ⏰ Напоминаниях о предстоящих сменах
        
        *Настройки:*
        • /settings - настроить уведомления
        • /help - получить помощь
        • /status - проверить статус
        
        Время уведомлений по умолчанию: за 1 час до смены
        """;

        InlineKeyboardMarkup inlineKeyboard = new(
        [
            [
                InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "settings"),
                InlineKeyboardButton.WithCallbackData("ℹ️ Помощь", "help")
            ]
        ]);

        await m_BotClient.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    public async Task SendSettingsMessageAsync(long chatId, TelegramUserSettings settings, CancellationToken ct = default)
    {
        string message = $"""
        ⚙️ *Ваши текущие настройки:*
        
        • 🔔 *Уведомления:* {(settings.IsEnabled ? "✅ Включены" : "❌ Отключены")}
        • ⏰ *За сколько минут:* {settings.NotificationMinutesBefore} мин.
        • 📅 *О начале смен:* {(settings.NotifyShiftStart ? "✅ Да" : "❌ Нет")}
        • 🔄 *Об обмене сменами:* {(settings.NotifyShiftExchange ? "✅ Да" : "❌ Нет")}
        • ⏰ *Напоминания:* {(settings.NotifyShiftReminder ? "✅ Да" : "❌ Нет")}
        
        *Изменить настройки:*
        /enable - включить уведомления
        /disable - отключить уведомления
        /time [минуты] - изменить время напоминания (например /time 30)
        """;

        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("✅ Включить", "enable"),
                InlineKeyboardButton.WithCallbackData("❌ Отключить", "disable")
            ],
            [
                InlineKeyboardButton.WithCallbackData("⏰ 30 мин", "time_30"),
                InlineKeyboardButton.WithCallbackData("⏰ 60 мин", "time_60"),
                InlineKeyboardButton.WithCallbackData("⏰ 120 мин", "time_120")
            ]
        ]);

        await m_BotClient.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    private string GetShiftExchangeMessage(ShiftExchangeRequest request, string action, bool isForRequester)
    {
        string requesterName = $"{request.Requester!.FirstName} {request.Requester.LastName}";
        string targetName = $"{request.Target!.FirstName} {request.Target.LastName}";
        
        return action.ToLower() switch
        {
            "created" when isForRequester => $"""
            🔄 *Запрос на обмен сменами отправлен!*
            
            📤 *Кому:* {targetName}
            📅 *Ваша смена:* {request.RequesterShift!.StartTime:dd.MM.yyyy HH:mm}
            📅 *Смена {targetName}:* {request.TargetShift!.StartTime:dd.MM.yyyy HH:mm}
            
            ⏳ *Ожидайте ответа!*
            """,
            
            "created" when !isForRequester => $"""
            🔄 *Новый запрос на обмен сменами!*
            
            📥 *От:* {requesterName}
            📅 *Смена {requesterName}:* {request.RequesterShift!.StartTime:dd.MM.yyyy HH:mm}
            📅 *Ваша смена:* {request.TargetShift!.StartTime:dd.MM.yyyy HH:mm}
            💬 *Сообщение:* {request.RequestMessage ?? "Без сообщения"}
            
            Используйте кнопки ниже для ответа:
            """,
            
            "approved" => $"""
            ✅ *Запрос на обмен сменами ОДОБРЕН!*
            
            🔄 *С кем:* {(isForRequester ? targetName : requesterName)}
            📅 *Смена успешно обменена!
            
            🎉 Поздравляем! Смены успешно обменены.
            """,
            
            "rejected" => $"""
            ❌ *Запрос на обмен сменами ОТКЛОНЕН*
            
            🔄 *С кем:* {(isForRequester ? targetName : requesterName)}
            💬 *Сообщение:* {request.ResponseMessage ?? "Без объяснения причин"}
            """,
            
            _ => "🔄 Обновление статуса запроса на обмен сменами"
        };
    }

    private async Task SendMessageAsync(long chatId, string message, CancellationToken ct)
    {
        try
        {
            await m_BotClient.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to send message to chat {ChatId}", chatId);
        }
    }

    private DateTime ConvertToLocalTime(DateTime utcTime, string? timeZoneId)
{
    if (string.IsNullOrEmpty(timeZoneId))
        timeZoneId = "Europe/Moscow";
    
    try
    {
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        
        m_Logger.LogDebug("Converted {UtcTime} UTC to {LocalTime} {TimeZone}", 
            utcTime, localTime, timeZoneId);
        
        return localTime;
    }
    catch (Exception ex)
    {
        m_Logger.LogError(ex, "Failed to convert time for timezone {TimeZone}", timeZoneId);
        return utcTime; // fallback to UTC
    }
}
}
