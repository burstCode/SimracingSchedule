using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Application.Telegram.Services;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Telegram.Background;

public class ShiftReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider m_ServiceProvider;
    private readonly ILogger<ShiftReminderBackgroundService> m_Logger;

    public ShiftReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ShiftReminderBackgroundService> logger)
    {
        m_ServiceProvider = serviceProvider;
        m_Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        m_Logger.LogInformation("Shift Reminder Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendReminders(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Проверяем каждую минуту
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error in Shift Reminder Background Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    // SimRacingSchedule.Application.Telegram/Background/ShiftReminderBackgroundService.cs
private async Task CheckAndSendReminders(CancellationToken ct)
{
    using IServiceScope scope = m_ServiceProvider.CreateScope();

        ITelegramUserSettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ITelegramUserSettingsRepository>();
        IShiftRepository shiftRepository = scope.ServiceProvider.GetRequiredService<IShiftRepository>();
        IEmployeeRepository employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        ITelegramNotificationService notificationService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        IEnumerable<TelegramUserSettings> allSettings = await settingsRepository.GetAllEnabledAsync(ct);
    m_Logger.LogInformation("🔍 Checking reminders for {Count} enabled users", allSettings.Count());
    
    foreach (TelegramUserSettings settings in allSettings)
    {
            Employee? employee = await employeeRepository.GetByIdAsync(settings.EmployeeId, ct);
        if (employee == null)
        {
            m_Logger.LogWarning("Employee {EmployeeId} not found for settings {SettingsId}", settings.EmployeeId, settings.Id);
            continue;
        }

            // Получаем ближайшие смены сотрудника (сейчас + 24 часа)
            IEnumerable<Shift> upcomingShifts = await shiftRepository.GetByEmployeeIdAsync(
            employee.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), ct); // Проверяем только следующие 24 часа

        m_Logger.LogInformation("📅 Employee {EmployeeName} has {ShiftCount} upcoming shifts in next 24 hours", 
            $"{employee.FirstName} {employee.LastName}", upcomingShifts.Count());

        foreach (Shift shift in upcomingShifts)
        {
                double minutesUntilShift = (shift.StartTime - DateTime.UtcNow).TotalMinutes;
            
            m_Logger.LogInformation("⏰ Shift at {ShiftTime} starts in {Minutes} minutes. Notification threshold: {Threshold} minutes",
                shift.StartTime, minutesUntilShift, settings.NotificationMinutesBefore);
            
            // Проверяем, нужно ли отправить напоминание (в пределах 5 минут от порога)
            if (minutesUntilShift <= settings.NotificationMinutesBefore && 
                minutesUntilShift > settings.NotificationMinutesBefore - 10)
            {
                m_Logger.LogInformation("📨 Sending reminder to {EmployeeName} for shift at {ShiftTime}", 
                    $"{employee.FirstName} {employee.LastName}", shift.StartTime);
                
                try
                {
                    await notificationService.SendShiftReminderAsync(
                        shift, 
                        employee, 
                        settings.NotificationMinutesBefore, 
                        ct);
                    
                    m_Logger.LogInformation("✅ Reminder sent successfully to {EmployeeName}", 
                        $"{employee.FirstName} {employee.LastName}");
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "❌ Failed to send reminder to {EmployeeName}", 
                        $"{employee.FirstName} {employee.LastName}");
                }
            }
        }
    }
}
}
