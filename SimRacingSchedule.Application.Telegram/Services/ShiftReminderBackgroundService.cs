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

    private async Task CheckAndSendReminders(CancellationToken ct)
    {
        using IServiceScope scope = m_ServiceProvider.CreateScope();

        ITelegramUserSettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ITelegramUserSettingsRepository>();
        IShiftRepository shiftRepository = scope.ServiceProvider.GetRequiredService<IShiftRepository>();
        IEmployeeRepository employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        ITelegramNotificationService notificationService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        IEnumerable<TelegramUserSettings> allSettings = await settingsRepository.GetAllEnabledAsync(ct);
        
        foreach (TelegramUserSettings settings in allSettings)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(settings.EmployeeId, ct);
            if (employee == null) continue;

            // Получаем ближайшие смены сотрудника
            IEnumerable<Shift> upcomingShifts = await shiftRepository.GetByEmployeeIdAsync(
                employee.Id,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7), ct);

            foreach (Shift shift in upcomingShifts)
            {
                double minutesUntilShift = (shift.StartTime - DateTime.UtcNow).TotalMinutes;
                
                // Проверяем, нужно ли отправить напоминание
                if (minutesUntilShift <= settings.NotificationMinutesBefore && 
                    minutesUntilShift > settings.NotificationMinutesBefore - 5) // В пределах 5 минут
                {
                    await notificationService.SendShiftReminderAsync(
                        shift, 
                        employee, 
                        settings.NotificationMinutesBefore, 
                        ct);
                    
                    m_Logger.LogInformation(
                        "Sent shift reminder to {EmployeeName} for shift at {ShiftTime}",
                        $"{employee.FirstName} {employee.LastName}",
                        shift.StartTime);
                }
            }
        }
    }
}
