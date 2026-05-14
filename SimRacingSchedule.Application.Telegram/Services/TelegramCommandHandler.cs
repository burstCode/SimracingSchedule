using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Extensions;
using SimRacingSchedule.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimRacingSchedule.Application.Telegram.Services;

public class TelegramCommandHandler
{
    private readonly ITelegramBotClient m_BotClient;
    private readonly ITelegramUserSettingsRepository m_SettingsRepo;
    private readonly IEmployeeRepository m_EmployeeRepo;
    private readonly IShiftRepository m_ShiftRepo;
    private readonly IShiftExchangeRepository m_ExchangeRepo;
    private readonly ITelegramNotificationService m_Notifications;
    private readonly ILogger<TelegramCommandHandler> m_Logger;

    // Multi-step flow state per chat (persists in memory between updates)
    private sealed class FlowState
    {
        // Shift creation
        public Guid? EmpId;
        public int DateOffset;
        public ShiftType? ShiftType;
        // Exchange creation
        public Guid? MyShiftId;
        public Guid? ColleagueId;
        // Employee creation
        public string? Step;
        public int BotMsgId;
        public string? NewFirstName;
        public string? NewLastName;
        public string? NewPatronymic;
        public string? NewPhone;
        public string? NewPosition;
        public EmployeeRole NewRole;
    }

    private static readonly ConcurrentDictionary<long, FlowState> s_Flows = new();

    // chatId → display name for pending Telegram link requests
    private static readonly ConcurrentDictionary<long, string> s_PendingLinks = new();

    public TelegramCommandHandler(
        ITelegramBotClient botClient,
        ITelegramUserSettingsRepository settingsRepository,
        IEmployeeRepository employeeRepository,
        IShiftRepository shiftRepository,
        IShiftExchangeRepository exchangeRepository,
        ITelegramNotificationService notificationService,
        ILogger<TelegramCommandHandler> logger)
    {
        m_BotClient = botClient;
        m_SettingsRepo = settingsRepository;
        m_EmployeeRepo = employeeRepository;
        m_ShiftRepo = shiftRepository;
        m_ExchangeRepo = exchangeRepository;
        m_Notifications = notificationService;
        m_Logger = logger;
    }

    // ─── Entry point ───────────────────────────────────────────────────────

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        long? chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
        try
        {
            if (update.Type == UpdateType.Message && update.Message != null)
                await HandleMessageAsync(update.Message, ct);
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await HandleCallbackAsync(update.CallbackQuery, ct);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error handling update");
            if (chatId.HasValue)
                try { await m_BotClient.SendMessage(chatId.Value, "⚠️ Произошла ошибка. Попробуйте позже.", cancellationToken: ct); }
                catch { /* ignored */ }
        }
    }

    // ─── Message handling ──────────────────────────────────────────────────

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (message.Text == null) return;
        long chatId = message.Chat.Id;
        string text = message.Text.Trim();
        if (!text.StartsWith("/"))
        {
            if (s_Flows.TryGetValue(chatId, out FlowState? activeFlow) && activeFlow.Step != null)
                await HandleFlowTextInputAsync(chatId, text, activeFlow, ct);
            return;
        }

        string command = text.Split(' ')[0].ToLower();
        TelegramUserSettings? settings = await m_SettingsRepo.GetByChatIdAsync(chatId, ct);

        switch (command)
        {
            case "/start":
                await HandleStartAsync(chatId, message.From!, ct);
                break;
            case "/help":
                await SendHelpAsync(chatId, ct);
                break;
            case "/settings":
                if (settings != null) await m_Notifications.SendSettingsMessageAsync(chatId, settings, ct);
                else await SendUnregisteredAsync(chatId, ct);
                break;
            case "/enable":
                if (settings != null) { settings.IsEnabled = true; await SaveSettingsAsync(settings, ct); await m_BotClient.SendMessage(chatId, "✅ Уведомления включены!", cancellationToken: ct); }
                break;
            case "/disable":
                if (settings != null) { settings.IsEnabled = false; await SaveSettingsAsync(settings, ct); await m_BotClient.SendMessage(chatId, "❌ Уведомления отключены!", cancellationToken: ct); }
                break;
            case "/status":
                await SendStatusAsync(chatId, settings, ct);
                break;
            case "/time":
                await HandleTimeCommandAsync(chatId, text, settings, ct);
                break;
            case "/timezone":
                await HandleTimezoneCommandAsync(chatId, text, settings, ct);
                break;
            default:
                await m_BotClient.SendMessage(chatId, "❓ Неизвестная команда. Используйте /help", cancellationToken: ct);
                break;
        }
    }

    // ─── /start ────────────────────────────────────────────────────────────

    private async Task HandleStartAsync(long chatId, User from, CancellationToken ct)
    {
        Employee? employee = await FindEmployeeAsync(from, chatId, ct);

        if (employee == null)
        {
            string displayName = from.FirstName + (from.LastName != null ? $" {from.LastName}" : "");
            s_PendingLinks[chatId] = displayName;

            await m_BotClient.SendMessage(chatId,
                $"👋 Привет, {from.FirstName}!\n\n❌ Ваш аккаунт не привязан к системе.\n⏳ Запрос отправлен администратору — ожидайте привязки.",
                cancellationToken: ct);

            await NotifyAdminsOfLinkRequestAsync(chatId, from, ct);
            return;
        }

        TelegramUserSettings? existing = await m_SettingsRepo.GetByEmployeeIdAsync(employee.Id, ct);
        if (existing == null)
        {
            await m_SettingsRepo.AddAsync(new TelegramUserSettings { EmployeeId = employee.Id, TelegramChatId = chatId }, ct);
            await m_SettingsRepo.SaveChangesAsync(ct);
        }
        else if (existing.TelegramChatId != chatId)
        {
            existing.TelegramChatId = chatId;
            await SaveSettingsAsync(existing, ct);
        }

        await ShowMainMenuAsync(chatId, null, employee, ct);
    }

    // ─── Main menu ─────────────────────────────────────────────────────────

    private async Task ShowMainMenuAsync(long chatId, int? msgId, Employee employee, CancellationToken ct)
    {
        string name = $"{employee.FirstName} {employee.LastName}";
        bool isAdmin = employee.Role == EmployeeRole.Administrator;

        string text = isAdmin
            ? $"👋 *{name}*\n🔑 Панель администратора\n\nВыберите раздел:"
            : $"👋 *{name}*\n👷 Панель сотрудника\n\nВыберите раздел:";

        InlineKeyboardMarkup keyboard = isAdmin
            ? new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 Управление сменами", "adm_shifts:0") },
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Запросы на обмен", "adm_exchanges") },
                new[] { InlineKeyboardButton.WithCallbackData("👥 Сотрудники", "adm_employees") },
                new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки уведомлений", "settings") },
            })
            : new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 Моё расписание", "wrk_schedule:0") },
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Обмен сменами", "wrk_exchange") },
                new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки уведомлений", "settings") },
            });

        if (msgId.HasValue)
            await EditMsgAsync(chatId, msgId.Value, text, keyboard, ct);
        else
            await m_BotClient.SendMessage(chatId, text, ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
    }

    // ─── Callback router ───────────────────────────────────────────────────

    private async Task HandleCallbackAsync(CallbackQuery query, CancellationToken ct)
    {
        long chatId = query.Message!.Chat.Id;
        int msgId = query.Message.MessageId;
        string data = query.Data ?? string.Empty;

        await m_BotClient.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        TelegramUserSettings? settings = await m_SettingsRepo.GetByChatIdAsync(chatId, ct);
        Employee? employee = settings != null ? await m_EmployeeRepo.GetByIdAsync(settings.EmployeeId, ct) : null;

        if (employee == null)
        {
            await SendUnregisteredAsync(chatId, ct);
            return;
        }

        if (data == "main")
        {
            s_Flows.TryRemove(chatId, out _);
            await ShowMainMenuAsync(chatId, msgId, employee, ct);
            return;
        }

        if (data is "settings" or "enable" or "disable" or "time_30" or "time_60" or "time_120")
        {
            await HandleSettingsCallbackAsync(chatId, data, settings!, ct);
            return;
        }

        if (data.StartsWith("adm_") && employee.Role == EmployeeRole.Administrator)
        {
            await HandleAdminCallbackAsync(chatId, msgId, data, employee, ct);
            return;
        }

        if (data.StartsWith("wrk_") || data.StartsWith("exc_"))
        {
            await HandleWorkerCallbackAsync(chatId, msgId, data, employee, ct);
            return;
        }
    }

    // ─── Settings callbacks ────────────────────────────────────────────────

    private async Task HandleSettingsCallbackAsync(long chatId, string data, TelegramUserSettings settings, CancellationToken ct)
    {
        switch (data)
        {
            case "settings":
                await m_Notifications.SendSettingsMessageAsync(chatId, settings, ct);
                return;
            case "enable":   settings.IsEnabled = true; break;
            case "disable":  settings.IsEnabled = false; break;
            case "time_30":  settings.NotificationMinutesBefore = 30; break;
            case "time_60":  settings.NotificationMinutesBefore = 60; break;
            case "time_120": settings.NotificationMinutesBefore = 120; break;
        }
        await SaveSettingsAsync(settings, ct);
        await m_Notifications.SendSettingsMessageAsync(chatId, settings, ct);
    }

    // ─── Admin callbacks ───────────────────────────────────────────────────

    private async Task HandleAdminCallbackAsync(long chatId, int msgId, string data, Employee admin, CancellationToken ct)
    {
        if (data.StartsWith("adm_shifts:"))
        {
            int off = int.Parse(data["adm_shifts:".Length..]);
            await ShowAdminShiftsAsync(chatId, msgId, off, ct);
            return;
        }
        if (data == "adm_add_emp")
        {
            await ShowAdminSelectEmployeeAsync(chatId, msgId, ct);
            return;
        }
        if (data.StartsWith("adm_emp:"))
        {
            Guid empId = Guid.ParseExact(data["adm_emp:".Length..], "N");
            s_Flows[chatId] = new FlowState { EmpId = empId };
            await ShowAdminSelectDateAsync(chatId, msgId, 0, ct);
            return;
        }
        if (data.StartsWith("adm_date_pg:"))
        {
            int page = int.Parse(data["adm_date_pg:".Length..]);
            await ShowAdminSelectDateAsync(chatId, msgId, page, ct);
            return;
        }
        if (data.StartsWith("adm_date:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.EmpId == null) return;
            flow.DateOffset = int.Parse(data["adm_date:".Length..]);
            await ShowAdminSelectTypeAsync(chatId, msgId, ct);
            return;
        }
        if (data.StartsWith("adm_type:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.EmpId == null) return;
            flow.ShiftType = Enum.Parse<ShiftType>(data["adm_type:".Length..]);
            await ShowAdminConfirmShiftAsync(chatId, msgId, flow, ct);
            return;
        }
        if (data == "adm_confirm")
        {
            await ConfirmAdminCreateShiftAsync(chatId, msgId, ct);
            return;
        }
        if (data.StartsWith("adm_cancel_shift:"))
        {
            Guid shiftId = Guid.ParseExact(data["adm_cancel_shift:".Length..], "N");
            await AdminCancelShiftAsync(chatId, msgId, shiftId, ct);
            return;
        }
        if (data == "adm_exchanges")
        {
            await ShowAdminExchangesAsync(chatId, msgId, ct);
            return;
        }
        if (data.StartsWith("adm_exc_acc:"))
        {
            Guid reqId = Guid.ParseExact(data["adm_exc_acc:".Length..], "N");
            await AdminHandleExchangeAsync(chatId, msgId, reqId, true, ct);
            return;
        }
        if (data.StartsWith("adm_exc_rej:"))
        {
            Guid reqId = Guid.ParseExact(data["adm_exc_rej:".Length..], "N");
            await AdminHandleExchangeAsync(chatId, msgId, reqId, false, ct);
            return;
        }
        if (data == "adm_employees")
        {
            await ShowAdminEmployeesAsync(chatId, msgId, ct);
            return;
        }
        if (data == "adm_emp_new")
        {
            await StartCreateEmployeeFlowAsync(chatId, msgId, ct);
            return;
        }
        if (data == "adm_emp_skip_patronymic")
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.Step != "emp_patronymic") return;
            flow.NewPatronymic = null;
            flow.Step = "emp_phone";
            await EditMsgAsync(chatId, flow.BotMsgId,
                BuildEmpFormText(flow) + "\n📞 *Введите номер телефона:*",
                new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") } }),
                ct);
            return;
        }
        if (data.StartsWith("adm_emp_role:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.Step != "emp_role") return;
            flow.NewRole = Enum.Parse<EmployeeRole>(data["adm_emp_role:".Length..]);
            flow.Step = "emp_done";
            await ShowEmployeeConfirmAsync(chatId, flow.BotMsgId, flow, ct);
            return;
        }
        if (data == "adm_emp_confirm")
        {
            await ConfirmCreateEmployeeAsync(chatId, msgId, ct);
            return;
        }
        if (data == "adm_pending_links")
        {
            await ShowPendingLinksAsync(chatId, msgId, ct);
            return;
        }
        if (data.StartsWith("adm_link:"))
        {
            string rest = data["adm_link:".Length..];
            int sep = rest.LastIndexOf(':');
            if (sep < 0) return;
            long pendingChatId = long.Parse(rest[..sep]);
            Guid empId = Guid.ParseExact(rest[(sep + 1)..], "N");
            await LinkEmployeeAsync(chatId, msgId, pendingChatId, empId, ct);
            return;
        }
        if (data.StartsWith("adm_link_rej:"))
        {
            long pendingChatId = long.Parse(data["adm_link_rej:".Length..]);
            s_PendingLinks.TryRemove(pendingChatId, out _);
            await EditMsgAsync(chatId, msgId, "❌ Запрос на привязку отклонён.", MainMenuKb(), ct);
            try { await m_BotClient.SendMessage(pendingChatId, "❌ Ваш запрос на привязку был отклонён администратором.", cancellationToken: ct); }
            catch { /* user may have never started */ }
            return;
        }
    }

    // ─── Admin screens ─────────────────────────────────────────────────────

    private async Task ShowAdminShiftsAsync(long chatId, int msgId, int weekOffset, CancellationToken ct)
    {
        DateTime monday = GetMonday(weekOffset);

        List<Shift> allShifts = new();
        for (int d = 0; d < 7; d++)
            allShifts.AddRange(await m_ShiftRepo.GetByDateAsync(monday.AddDays(d), ct));

        string weekLabel = $"{monday:dd.MM} – {monday.AddDays(6):dd.MM.yyyy}";
        string text = $"📅 *Смены: {weekLabel}*\n\n";

        if (!allShifts.Any())
        {
            text += "_Смен на эту неделю нет._";
        }
        else
        {
            foreach (var dayGroup in allShifts.GroupBy(s => s.StartTime.Date).OrderBy(g => g.Key))
            {
                text += $"*{FormatDate(dayGroup.Key)}:*\n";
                foreach (Shift s in dayGroup.OrderBy(s => s.StartTime))
                {
                    string emp = s.Employee != null ? $"{s.Employee.LastName} {s.Employee.FirstName[0]}." : "?";
                    string cancelled = s.Status == ShiftStatus.Cancelled ? " ~~отм.~~" : string.Empty;
                    text += $"  • {emp} — {ShiftTypeShort(s.Type)}{cancelled}\n";
                }
            }
        }

        List<InlineKeyboardButton[]> rows = new()
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("◀", $"adm_shifts:{weekOffset - 1}"),
                InlineKeyboardButton.WithCallbackData(weekOffset == 0 ? "Эта неделя" : weekOffset > 0 ? $"+{weekOffset}н" : $"{weekOffset}н", "adm_shifts:0"),
                InlineKeyboardButton.WithCallbackData("▶", $"adm_shifts:{weekOffset + 1}"),
            },
            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить смену", "adm_add_emp") },
        };

        var cancelable = allShifts
            .Where(s => s.StartTime > DateTime.UtcNow && s.Status == ShiftStatus.Scheduled)
            .OrderBy(s => s.StartTime)
            .Take(5)
            .ToList();

        if (cancelable.Any())
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("─── Отменить смену ───", $"adm_shifts:{weekOffset}") });
            foreach (Shift s in cancelable)
            {
                string emp = s.Employee != null ? $"{s.Employee.LastName} {s.Employee.FirstName[0]}." : "?";
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"❌ {emp} {s.StartTime:dd.MM} {ShiftTypeShort(s.Type)}",
                        $"adm_cancel_shift:{s.Id:N}"),
                });
            }
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") });
        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowAdminSelectEmployeeAsync(long chatId, int msgId, CancellationToken ct)
    {
        IEnumerable<Employee> employees = await m_EmployeeRepo.GetActiveEmployeesAsync(ct);
        List<InlineKeyboardButton[]> rows = employees
            .Select(e => new[] { InlineKeyboardButton.WithCallbackData($"👤 {e.LastName} {e.FirstName}", $"adm_emp:{e.Id:N}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "adm_shifts:0") });
        await EditMsgAsync(chatId, msgId, "👥 *Выберите сотрудника:*", new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowAdminSelectDateAsync(long chatId, int msgId, int page, CancellationToken ct)
    {
        if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.EmpId == null) return;

        Employee? emp = await m_EmployeeRepo.GetByIdAsync(flow.EmpId.Value, ct);
        string empName = emp != null ? $"{emp.LastName} {emp.FirstName}" : "?";

        List<InlineKeyboardButton[]> rows = new();
        int startDay = page * 7;
        for (int i = startDay; i < startDay + 7; i++)
        {
            DateTime date = DateTime.UtcNow.Date.AddDays(i);
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"📅 {FormatDate(date)}", $"adm_date:{i}") });
        }

        List<InlineKeyboardButton> nav = new();
        if (page > 0) nav.Add(InlineKeyboardButton.WithCallbackData("◀ Предыдущие", $"adm_date_pg:{page - 1}"));
        nav.Add(InlineKeyboardButton.WithCallbackData("Следующие ▶", $"adm_date_pg:{page + 1}"));
        rows.Add(nav.ToArray());
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Другой сотрудник", "adm_add_emp") });

        await EditMsgAsync(chatId, msgId, $"👤 *{empName}*\n\n📅 *Выберите дату смены:*", new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowAdminSelectTypeAsync(long chatId, int msgId, CancellationToken ct)
    {
        InlineKeyboardMarkup kb = new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🌅 Утренняя   10:00–14:00", "adm_type:Morning") },
            new[] { InlineKeyboardButton.WithCallbackData("☀️ Дневная    14:00–18:00", "adm_type:Day") },
            new[] { InlineKeyboardButton.WithCallbackData("🌆 Вечерняя   18:00–22:00", "adm_type:Evening") },
            new[] { InlineKeyboardButton.WithCallbackData("🌞 Полный день 13:00–22:00", "adm_type:FullDay") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "adm_add_emp") },
        });
        await EditMsgAsync(chatId, msgId, "🕐 *Выберите тип смены:*", kb, ct);
    }

    private async Task ShowAdminConfirmShiftAsync(long chatId, int msgId, FlowState flow, CancellationToken ct)
    {
        Employee? emp = await m_EmployeeRepo.GetByIdAsync(flow.EmpId!.Value, ct);
        DateTime date = DateTime.UtcNow.Date.AddDays(flow.DateOffset);

        string text = $"✅ *Подтверждение создания смены:*\n\n" +
                      $"👤 {emp?.LastName} {emp?.FirstName}\n" +
                      $"📅 {date:dd.MM.yyyy}\n" +
                      $"🕐 {flow.ShiftType?.GetDisplayName()}";

        InlineKeyboardMarkup kb = new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✅ Создать", "adm_confirm") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Изменить тип", "adm_add_emp") },
        });
        await EditMsgAsync(chatId, msgId, text, kb, ct);
    }

    private async Task ConfirmAdminCreateShiftAsync(long chatId, int msgId, CancellationToken ct)
    {
        if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.EmpId == null || flow.ShiftType == null)
        {
            await EditMsgAsync(chatId, msgId, "⚠️ Сессия истекла. Начните заново.", MainMenuKb(), ct);
            return;
        }

        DateTime date = DateTime.UtcNow.Date.AddDays(flow.DateOffset);
        Shift shift = new(flow.EmpId.Value, date, flow.ShiftType.Value);
        await m_ShiftRepo.AddAsync(shift, ct);
        await m_ShiftRepo.SaveChangesAsync(ct);
        s_Flows.TryRemove(chatId, out _);

        Employee? emp = await m_EmployeeRepo.GetByIdAsync(flow.EmpId.Value, ct);
        string empName = emp != null ? $"{emp.LastName} {emp.FirstName}" : "?";

        await EditMsgAsync(chatId, msgId,
            $"✅ *Смена создана!*\n\n👤 {empName}\n📅 {date:dd.MM.yyyy}\n🕐 {flow.ShiftType.Value.GetDisplayName()}",
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 К расписанию", "adm_shifts:0") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);

        TelegramUserSettings? empSettings = await m_SettingsRepo.GetByEmployeeIdAsync(flow.EmpId.Value, ct);
        if (empSettings?.IsEnabled == true && empSettings.NotifyShiftStart)
        {
            try
            {
                await m_BotClient.SendMessage(empSettings.TelegramChatId,
                    $"📅 *Вам назначена новая смена!*\n\n📅 {date:dd.MM.yyyy}\n🕐 {flow.ShiftType.Value.GetDisplayName()}",
                    ParseMode.Markdown, cancellationToken: ct);
            }
            catch { /* chat not linked yet */ }
        }
    }

    private async Task AdminCancelShiftAsync(long chatId, int msgId, Guid shiftId, CancellationToken ct)
    {
        Shift? shift = await m_ShiftRepo.GetByIdAsync(shiftId, ct);
        if (shift == null)
        {
            await EditMsgAsync(chatId, msgId, "❌ Смена не найдена.", MainMenuKb(), ct);
            return;
        }

        shift.Cancel();
        await m_ShiftRepo.UpdateAsync(shift, ct);
        await m_ShiftRepo.SaveChangesAsync(ct);

        await EditMsgAsync(chatId, msgId,
            $"✅ Смена отменена.\n\n📅 {shift.StartTime:dd.MM.yyyy}\n🕐 {shift.Type.GetDisplayName()}",
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 К расписанию", "adm_shifts:0") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);

        TelegramUserSettings? empSettings = await m_SettingsRepo.GetByEmployeeIdAsync(shift.EmployeeId, ct);
        if (empSettings?.IsEnabled == true)
        {
            try
            {
                await m_BotClient.SendMessage(empSettings.TelegramChatId,
                    $"❌ *Ваша смена отменена!*\n\n📅 {shift.StartTime:dd.MM.yyyy}\n🕐 {shift.Type.GetDisplayName()}",
                    ParseMode.Markdown, cancellationToken: ct);
            }
            catch { /* ignored */ }
        }
    }

    private async Task ShowAdminExchangesAsync(long chatId, int msgId, CancellationToken ct)
    {
        var pending = (await m_ExchangeRepo.GetByStatusAsync(ExchangeRequestStatus.Pending, ct)).ToList();

        if (!pending.Any())
        {
            await EditMsgAsync(chatId, msgId, "🔄 *Запросы на обмен*\n\n_Нет активных запросов._", MainMenuKb(), ct);
            return;
        }

        string text = $"🔄 *Активные запросы ({pending.Count}):*\n\n";
        List<InlineKeyboardButton[]> rows = new();

        foreach (ShiftExchangeRequest req in pending)
        {
            string from = $"{req.Requester?.LastName} {req.Requester?.FirstName?[0]}.";
            string to   = $"{req.Target?.LastName} {req.Target?.FirstName?[0]}.";
            string fd   = req.RequesterShift?.StartTime.ToString("dd.MM") ?? "?";
            string td   = req.TargetShift?.StartTime.ToString("dd.MM") ?? "?";
            text += $"• {from} ({fd}) ↔ {to} ({td})\n";
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"✅ {from}↔{to}", $"adm_exc_acc:{req.Id:N}"),
                InlineKeyboardButton.WithCallbackData("❌", $"adm_exc_rej:{req.Id:N}"),
            });
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") });
        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task AdminHandleExchangeAsync(long chatId, int msgId, Guid reqId, bool approve, CancellationToken ct)
    {
        ShiftExchangeRequest? req = await m_ExchangeRepo.GetByIdAsync(reqId, ct);
        if (req == null || req.Status != ExchangeRequestStatus.Pending)
        {
            await EditMsgAsync(chatId, msgId, "⚠️ Запрос не найден или уже обработан.", MainMenuKb(), ct);
            return;
        }

        if (approve) req.Approve("Одобрено администратором");
        else req.Reject("Отклонено администратором");

        await m_ExchangeRepo.UpdateAsync(req, ct);
        await m_ExchangeRepo.SaveChangesAsync(ct);

        string result = approve ? "✅ одобрен" : "❌ отклонён";
        await EditMsgAsync(chatId, msgId,
            $"Запрос на обмен *{result}* администратором.",
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Все запросы", "adm_exchanges") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);

        await m_Notifications.SendShiftExchangeNotificationAsync(req, approve ? "approved" : "rejected", ct);
    }

    // ─── Admin employee management ────────────────────────────────────────

    private async Task ShowAdminEmployeesAsync(long chatId, int msgId, CancellationToken ct)
    {
        var employees = (await m_EmployeeRepo.GetActiveEmployeesAsync(ct)).ToList();

        string text;
        if (employees.Any())
        {
            text = $"👥 *Сотрудники ({employees.Count}):*\n\n";
            foreach (Employee e in employees)
            {
                TelegramUserSettings? s = await m_SettingsRepo.GetByEmployeeIdAsync(e.Id, ct);
                string roleIcon   = e.Role == EmployeeRole.Administrator ? "🔑" : "👷";
                string linkedIcon = s != null ? "🔗" : "  ";
                string name = $"{e.LastName} {e.FirstName}" + (e.Patronymic != null ? $" {e.Patronymic}" : "");
                text += $"{roleIcon}{linkedIcon} {name} — {e.Position}\n";
            }
            text += "\n_🔗 — Telegram привязан_";
        }
        else
        {
            text = "👥 *Сотрудников нет.*";
        }

        List<InlineKeyboardButton[]> rows = new()
        {
            new[] { InlineKeyboardButton.WithCallbackData("➕ Создать сотрудника", "adm_emp_new") },
        };
        if (s_PendingLinks.Any())
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"🔗 Ожидают привязки ({s_PendingLinks.Count})", "adm_pending_links") });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") });

        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task StartCreateEmployeeFlowAsync(long chatId, int msgId, CancellationToken ct)
    {
        s_Flows[chatId] = new FlowState { Step = "emp_firstname", BotMsgId = msgId };
        await EditMsgAsync(chatId, msgId,
            "👤 *Создание нового сотрудника*\n\n✏️ *Введите имя:*",
            new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") } }),
            ct);
    }

    private async Task HandleFlowTextInputAsync(long chatId, string text, FlowState flow, CancellationToken ct)
    {
        var cancelKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") } });

        switch (flow.Step)
        {
            case "emp_firstname":
                flow.NewFirstName = text;
                flow.Step = "emp_lastname";
                await EditMsgAsync(chatId, flow.BotMsgId,
                    BuildEmpFormText(flow) + "\n✏️ *Введите фамилию:*", cancelKb, ct);
                break;

            case "emp_lastname":
                flow.NewLastName = text;
                flow.Step = "emp_patronymic";
                await EditMsgAsync(chatId, flow.BotMsgId,
                    BuildEmpFormText(flow) + "\n✏️ *Введите отчество* (или пропустите):",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("⏭ Пропустить", "adm_emp_skip_patronymic") },
                        new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") },
                    }),
                    ct);
                break;

            case "emp_patronymic":
                flow.NewPatronymic = text;
                flow.Step = "emp_phone";
                await EditMsgAsync(chatId, flow.BotMsgId,
                    BuildEmpFormText(flow) + "\n📞 *Введите номер телефона:*", cancelKb, ct);
                break;

            case "emp_phone":
                flow.NewPhone = text;
                flow.Step = "emp_position";
                await EditMsgAsync(chatId, flow.BotMsgId,
                    BuildEmpFormText(flow) + "\n💼 *Введите должность:*", cancelKb, ct);
                break;

            case "emp_position":
                flow.NewPosition = text;
                flow.Step = "emp_role";
                await EditMsgAsync(chatId, flow.BotMsgId,
                    BuildEmpFormText(flow) + "\n🎭 *Выберите роль:*",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("👷 Сотрудник", "adm_emp_role:Employee"),
                            InlineKeyboardButton.WithCallbackData("🔑 Администратор", "adm_emp_role:Administrator"),
                        },
                        new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") },
                    }),
                    ct);
                break;
        }
    }

    private static string BuildEmpFormText(FlowState flow)
    {
        string t = "👤 *Создание нового сотрудника*\n\n";
        if (flow.NewFirstName != null) t += $"• Имя: {flow.NewFirstName}\n";
        if (flow.NewLastName  != null) t += $"• Фамилия: {flow.NewLastName}\n";
        string[] patronymicDoneSteps = ["emp_phone", "emp_position", "emp_role", "emp_done"];
        if (patronymicDoneSteps.Contains(flow.Step))
            t += flow.NewPatronymic != null ? $"• Отчество: {flow.NewPatronymic}\n" : "• Отчество: —\n";
        if (flow.NewPhone    != null) t += $"• Телефон: {flow.NewPhone}\n";
        if (flow.NewPosition != null) t += $"• Должность: {flow.NewPosition}\n";
        return t;
    }

    private async Task ShowEmployeeConfirmAsync(long chatId, int msgId, FlowState flow, CancellationToken ct)
    {
        string roleLabel = flow.NewRole == EmployeeRole.Administrator ? "🔑 Администратор" : "👷 Сотрудник";
        string text = BuildEmpFormText(flow) + $"• Роль: {roleLabel}\n\n✅ *Подтвердить создание?*";
        InlineKeyboardMarkup kb = new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✅ Создать", "adm_emp_confirm") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Отмена", "adm_employees") },
        });
        await EditMsgAsync(chatId, msgId, text, kb, ct);
    }

    private async Task ConfirmCreateEmployeeAsync(long chatId, int msgId, CancellationToken ct)
    {
        if (!s_Flows.TryGetValue(chatId, out FlowState? flow) ||
            flow.NewFirstName == null || flow.NewLastName == null ||
            flow.NewPhone == null || flow.NewPosition == null)
        {
            await EditMsgAsync(chatId, msgId, "⚠️ Сессия истекла. Начните заново.", MainMenuKb(), ct);
            return;
        }

        string email = $"{flow.NewLastName.ToLower()}.{flow.NewFirstName.ToLower()}@simracing.local";
        Employee emp = new(flow.NewFirstName, flow.NewLastName, email, flow.NewPhone, flow.NewPosition, flow.NewRole);
        if (!string.IsNullOrEmpty(flow.NewPatronymic))
            emp.Update(flow.NewFirstName, flow.NewLastName, flow.NewPatronymic, email, flow.NewPhone, flow.NewPosition);

        await m_EmployeeRepo.AddAsync(emp, ct);
        await m_EmployeeRepo.SaveChangesAsync(ct);
        s_Flows.TryRemove(chatId, out _);

        string roleLabel = flow.NewRole == EmployeeRole.Administrator ? "🔑 Администратор" : "👷 Сотрудник";
        string fullName  = $"{flow.NewLastName} {flow.NewFirstName}" + (flow.NewPatronymic != null ? $" {flow.NewPatronymic}" : "");
        await EditMsgAsync(chatId, msgId,
            $"✅ *Сотрудник создан!*\n\n👤 {fullName}\n💼 {flow.NewPosition}\n{roleLabel}",
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("👥 К сотрудникам", "adm_employees") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);
    }

    // ─── Telegram account linking ─────────────────────────────────────────

    private async Task NotifyAdminsOfLinkRequestAsync(long pendingChatId, User from, CancellationToken ct)
    {
        IEnumerable<Employee> admins = await m_EmployeeRepo.GetByRoleAsync(EmployeeRole.Administrator, ct);
        IEnumerable<Employee> allActive = await m_EmployeeRepo.GetActiveEmployeesAsync(ct);

        // Only show employees that don't have Telegram linked yet
        var unlinked = new List<Employee>();
        foreach (Employee e in allActive)
        {
            TelegramUserSettings? s = await m_SettingsRepo.GetByEmployeeIdAsync(e.Id, ct);
            if (s == null) unlinked.Add(e);
        }

        if (!unlinked.Any()) return;

        string displayName = from.FirstName + (from.LastName != null ? $" {from.LastName}" : "");
        string username    = from.Username != null ? $" (@{from.Username})" : "";
        string text = $"🔗 *Запрос на привязку аккаунта*\n\n" +
                      $"👤 Telegram: {displayName}{username}\n\n" +
                      $"Выберите сотрудника для привязки:";

        string chatIdStr = pendingChatId.ToString();
        List<InlineKeyboardButton[]> rows = unlinked
            .Select(e => new[] { InlineKeyboardButton.WithCallbackData(
                $"👤 {e.LastName} {e.FirstName}", $"adm_link:{chatIdStr}:{e.Id:N}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"adm_link_rej:{chatIdStr}") });
        InlineKeyboardMarkup keyboard = new(rows);

        foreach (Employee admin in admins)
        {
            TelegramUserSettings? adminSettings = await m_SettingsRepo.GetByEmployeeIdAsync(admin.Id, ct);
            if (adminSettings == null) continue;
            try
            {
                await m_BotClient.SendMessage(adminSettings.TelegramChatId, text,
                    ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to notify admin {AdminId} of link request", admin.Id);
            }
        }
    }

    private async Task ShowPendingLinksAsync(long chatId, int msgId, CancellationToken ct)
    {
        if (!s_PendingLinks.Any())
        {
            await EditMsgAsync(chatId, msgId, "🔗 *Ожидающие привязки*\n\n_Нет новых запросов._", MainMenuKb(), ct);
            return;
        }

        IEnumerable<Employee> allActive = await m_EmployeeRepo.GetActiveEmployeesAsync(ct);
        var unlinked = new List<Employee>();
        foreach (Employee e in allActive)
        {
            TelegramUserSettings? s = await m_SettingsRepo.GetByEmployeeIdAsync(e.Id, ct);
            if (s == null) unlinked.Add(e);
        }

        string text = $"🔗 *Ожидают привязки ({s_PendingLinks.Count}):*\n\n";
        List<InlineKeyboardButton[]> rows = new();

        foreach ((long pendingChatId, string displayName) in s_PendingLinks)
        {
            text += $"• {displayName}\n";
            string chatIdStr = pendingChatId.ToString();
            foreach (Employee e in unlinked)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(
                    $"🔗 {displayName} → {e.LastName} {e.FirstName}", $"adm_link:{chatIdStr}:{e.Id:N}") });
            }
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(
                $"❌ Отклонить {displayName}", $"adm_link_rej:{chatIdStr}") });
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") });
        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task LinkEmployeeAsync(long adminChatId, int msgId, long pendingChatId, Guid empId, CancellationToken ct)
    {
        Employee? emp = await m_EmployeeRepo.GetByIdAsync(empId, ct);
        if (emp == null)
        {
            await EditMsgAsync(adminChatId, msgId, "❌ Сотрудник не найден.", MainMenuKb(), ct);
            return;
        }

        TelegramUserSettings? existing = await m_SettingsRepo.GetByEmployeeIdAsync(empId, ct);
        if (existing != null)
        {
            existing.TelegramChatId = pendingChatId;
            existing.IsEnabled = true;
            await m_SettingsRepo.UpdateAsync(existing, ct);
        }
        else
        {
            await m_SettingsRepo.AddAsync(new TelegramUserSettings
            {
                EmployeeId = empId,
                TelegramChatId = pendingChatId,
                IsEnabled = true,
            }, ct);
        }
        await m_SettingsRepo.SaveChangesAsync(ct);

        s_PendingLinks.TryRemove(pendingChatId, out _);

        string empName = $"{emp.LastName} {emp.FirstName}";
        await EditMsgAsync(adminChatId, msgId,
            $"✅ *Аккаунт привязан!*\n\n👤 {empName}",
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("👥 К сотрудникам", "adm_employees") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);

        try
        {
            await m_Notifications.SendWelcomeMessageAsync(pendingChatId, $"{emp.FirstName} {emp.LastName}", ct);
        }
        catch (Exception ex)
        {
            m_Logger.LogWarning(ex, "Could not send welcome message after linking to {ChatId}", pendingChatId);
        }
    }

    // ─── Worker callbacks ──────────────────────────────────────────────────

    private async Task HandleWorkerCallbackAsync(long chatId, int msgId, string data, Employee worker, CancellationToken ct)
    {
        if (data.StartsWith("wrk_schedule:"))
        {
            int off = int.Parse(data["wrk_schedule:".Length..]);
            await ShowWorkerScheduleAsync(chatId, msgId, worker, off, ct);
            return;
        }

        switch (data)
        {
            case "wrk_exchange":
                await ShowWorkerExchangeMenuAsync(chatId, msgId, worker, ct);
                return;
            case "wrk_exc_new":
                await ShowWorkerMyShiftsAsync(chatId, msgId, worker, ct);
                return;
            case "wrk_exc_in":
                await ShowWorkerIncomingAsync(chatId, msgId, worker, ct);
                return;
            case "wrk_exc_out":
                await ShowWorkerOutgoingAsync(chatId, msgId, worker, ct);
                return;
        }

        if (data.StartsWith("wrk_my_shift:"))
        {
            Guid shiftId = Guid.ParseExact(data["wrk_my_shift:".Length..], "N");
            s_Flows[chatId] = new FlowState { MyShiftId = shiftId };
            await ShowWorkerSelectColleagueAsync(chatId, msgId, worker, ct);
            return;
        }
        if (data.StartsWith("wrk_col:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow)) return;
            Guid colId = Guid.ParseExact(data["wrk_col:".Length..], "N");
            flow.ColleagueId = colId;
            await ShowWorkerColleagueShiftsAsync(chatId, msgId, colId, ct);
            return;
        }
        if (data.StartsWith("wrk_their_shift:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.MyShiftId == null) return;
            Guid theirShiftId = Guid.ParseExact(data["wrk_their_shift:".Length..], "N");
            await ShowWorkerConfirmExchangeAsync(chatId, msgId, flow.MyShiftId.Value, theirShiftId, ct);
            return;
        }
        if (data.StartsWith("wrk_exc_confirm:"))
        {
            if (!s_Flows.TryGetValue(chatId, out FlowState? flow) || flow.MyShiftId == null) return;
            Guid theirShiftId = Guid.ParseExact(data["wrk_exc_confirm:".Length..], "N");
            await ConfirmWorkerExchangeAsync(chatId, msgId, worker, flow.MyShiftId.Value, theirShiftId, ct);
            return;
        }
        if (data.StartsWith("wrk_exc_acc:") || data.StartsWith("exc_acc:"))
        {
            string raw = data.StartsWith("wrk_exc_acc:") ? data["wrk_exc_acc:".Length..] : data["exc_acc:".Length..];
            Guid reqId = Guid.ParseExact(raw, "N");
            await RespondToExchangeAsync(chatId, msgId, reqId, true, worker, ct);
            return;
        }
        if (data.StartsWith("wrk_exc_rej:") || data.StartsWith("exc_rej:"))
        {
            string raw = data.StartsWith("wrk_exc_rej:") ? data["wrk_exc_rej:".Length..] : data["exc_rej:".Length..];
            Guid reqId = Guid.ParseExact(raw, "N");
            await RespondToExchangeAsync(chatId, msgId, reqId, false, worker, ct);
            return;
        }
        if (data.StartsWith("wrk_exc_can:"))
        {
            Guid reqId = Guid.ParseExact(data["wrk_exc_can:".Length..], "N");
            await CancelWorkerExchangeAsync(chatId, msgId, reqId, ct);
            return;
        }
    }

    // ─── Worker screens ────────────────────────────────────────────────────

    private async Task ShowWorkerScheduleAsync(long chatId, int msgId, Employee worker, int weekOffset, CancellationToken ct)
    {
        DateTime monday = GetMonday(weekOffset);
        IEnumerable<Shift> shifts = await m_ShiftRepo.GetByEmployeeIdAsync(worker.Id, monday, monday.AddDays(7), ct);

        string weekLabel = $"{monday:dd.MM} – {monday.AddDays(6):dd.MM.yyyy}";
        string text = $"📅 *Расписание: {weekLabel}*\n\n";

        if (!shifts.Any())
        {
            text += "_Смен на эту неделю нет._";
        }
        else
        {
            foreach (Shift s in shifts.OrderBy(s => s.StartTime))
            {
                string flag = s.Status == ShiftStatus.Cancelled ? " ❌"
                    : s.StartTime < DateTime.UtcNow ? " ✔️"
                    : " 🔜";
                text += $"• {FormatDate(s.StartTime.Date)} — {ShiftTypeShort(s.Type)}{flag}\n";
            }
        }

        InlineKeyboardMarkup kb = new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("◀", $"wrk_schedule:{weekOffset - 1}"),
                InlineKeyboardButton.WithCallbackData(weekOffset == 0 ? "Эта неделя" : weekOffset > 0 ? $"+{weekOffset}н" : $"{weekOffset}н", "wrk_schedule:0"),
                InlineKeyboardButton.WithCallbackData("▶", $"wrk_schedule:{weekOffset + 1}"),
            },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
        });
        await EditMsgAsync(chatId, msgId, text, kb, ct);
    }

    private async Task ShowWorkerExchangeMenuAsync(long chatId, int msgId, Employee worker, CancellationToken ct)
    {
        int incoming = (await m_ExchangeRepo.GetPendingRequestsForEmployeeAsync(worker.Id, ct)).Count();
        int outgoing = (await m_ExchangeRepo.GetRequestsByRequesterAsync(worker.Id, ct))
            .Count(r => r.Status == ExchangeRequestStatus.Pending);

        string text = $"🔄 *Обмен сменами*\n\n📥 Входящих: {incoming}\n📤 Исходящих: {outgoing}";
        InlineKeyboardMarkup kb = new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData($"📥 Входящие ({incoming})", "wrk_exc_in") },
            new[] { InlineKeyboardButton.WithCallbackData($"📤 Мои запросы ({outgoing})", "wrk_exc_out") },
            new[] { InlineKeyboardButton.WithCallbackData("➕ Предложить обмен", "wrk_exc_new") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
        });
        await EditMsgAsync(chatId, msgId, text, kb, ct);
    }

    private async Task ShowWorkerMyShiftsAsync(long chatId, int msgId, Employee worker, CancellationToken ct)
    {
        var shifts = (await m_ShiftRepo.GetAvailableForExchangeAsync(worker.Id, ct)).Take(10).ToList();

        if (!shifts.Any())
        {
            await EditMsgAsync(chatId, msgId, "📭 *Нет доступных смен для обмена.*",
                BackKb("wrk_exchange"), ct);
            return;
        }

        List<InlineKeyboardButton[]> rows = shifts
            .Select(s => new[] { InlineKeyboardButton.WithCallbackData($"📅 {s.StartTime:dd.MM} {ShiftTypeShort(s.Type)}", $"wrk_my_shift:{s.Id:N}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "wrk_exchange") });
        await EditMsgAsync(chatId, msgId, "📤 *Выберите свою смену:*", new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowWorkerSelectColleagueAsync(long chatId, int msgId, Employee worker, CancellationToken ct)
    {
        var colleagues = (await m_EmployeeRepo.GetActiveEmployeesAsync(ct)).Where(e => e.Id != worker.Id).ToList();
        List<InlineKeyboardButton[]> rows = colleagues
            .Select(e => new[] { InlineKeyboardButton.WithCallbackData($"👤 {e.LastName} {e.FirstName}", $"wrk_col:{e.Id:N}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "wrk_exc_new") });
        await EditMsgAsync(chatId, msgId, "👥 *Выберите коллегу:*", new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowWorkerColleagueShiftsAsync(long chatId, int msgId, Guid colleagueId, CancellationToken ct)
    {
        Employee? colleague = await m_EmployeeRepo.GetByIdAsync(colleagueId, ct);
        var shifts = (await m_ShiftRepo.GetAvailableForExchangeAsync(colleagueId, ct)).Take(10).ToList();
        string colName = colleague != null ? $"{colleague.LastName} {colleague.FirstName}" : "?";

        if (!shifts.Any())
        {
            await EditMsgAsync(chatId, msgId, $"📭 *У {colName} нет доступных смен.*",
                BackKb("wrk_exc_new"), ct);
            return;
        }

        List<InlineKeyboardButton[]> rows = shifts
            .Select(s => new[] { InlineKeyboardButton.WithCallbackData($"📅 {s.StartTime:dd.MM} {ShiftTypeShort(s.Type)}", $"wrk_their_shift:{s.Id:N}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Другой коллега", "wrk_exc_new") });
        await EditMsgAsync(chatId, msgId, $"📅 *Смены {colName}:*", new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowWorkerConfirmExchangeAsync(long chatId, int msgId, Guid myShiftId, Guid theirShiftId, CancellationToken ct)
    {
        Shift? my    = await m_ShiftRepo.GetByIdAsync(myShiftId, ct);
        Shift? their = await m_ShiftRepo.GetByIdAsync(theirShiftId, ct);
        string colName = their?.Employee != null ? $"{their.Employee.LastName} {their.Employee.FirstName}" : "?";

        string text = "🔄 *Подтверждение обмена:*\n\n" +
                      $"📤 *Ваша смена:* {my?.StartTime:dd.MM.yyyy} {(my != null ? ShiftTypeShort(my.Type) : "?")}\n" +
                      $"📥 *Смена {colName}:* {their?.StartTime:dd.MM.yyyy} {(their != null ? ShiftTypeShort(their.Type) : "?")}";

        InlineKeyboardMarkup kb = new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✅ Отправить запрос", $"wrk_exc_confirm:{theirShiftId:N}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "wrk_exc_new") },
        });
        await EditMsgAsync(chatId, msgId, text, kb, ct);
    }

    private async Task ConfirmWorkerExchangeAsync(long chatId, int msgId, Employee worker, Guid myShiftId, Guid theirShiftId, CancellationToken ct)
    {
        Shift? myShift    = await m_ShiftRepo.GetByIdAsync(myShiftId, ct);
        Shift? theirShift = await m_ShiftRepo.GetByIdAsync(theirShiftId, ct);

        if (myShift == null || theirShift == null)
        {
            await EditMsgAsync(chatId, msgId, "❌ Смена не найдена.", MainMenuKb(), ct);
            return;
        }

        Employee? target = await m_EmployeeRepo.GetByIdAsync(theirShift.EmployeeId, ct);
        if (target == null)
        {
            await EditMsgAsync(chatId, msgId, "❌ Сотрудник не найден.", MainMenuKb(), ct);
            return;
        }

        try
        {
            ShiftExchangeRequest req = new(worker, target, myShift, theirShift, "Запрос через Telegram-бот");
            await m_ExchangeRepo.AddAsync(req, ct);
            await m_ExchangeRepo.SaveChangesAsync(ct);
            s_Flows.TryRemove(chatId, out _);

            await EditMsgAsync(chatId, msgId,
                $"✅ *Запрос отправлен!*\n\n" +
                $"{myShift.StartTime:dd.MM} {ShiftTypeShort(myShift.Type)} ↔ {theirShift.StartTime:dd.MM} {ShiftTypeShort(theirShift.Type)}\n\n" +
                "⏳ Ожидайте ответа.",
                new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("📤 Мои запросы", "wrk_exc_out") },
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
                }),
                ct);

            await m_Notifications.SendExchangeRequestWithButtonsAsync(req, target, ct);
        }
        catch (InvalidOperationException ex)
        {
            await EditMsgAsync(chatId, msgId, $"❌ {ex.Message}", MainMenuKb(), ct);
        }
    }

    private async Task ShowWorkerIncomingAsync(long chatId, int msgId, Employee worker, CancellationToken ct)
    {
        var requests = (await m_ExchangeRepo.GetPendingRequestsForEmployeeAsync(worker.Id, ct)).ToList();

        if (!requests.Any())
        {
            await EditMsgAsync(chatId, msgId, "📭 *Нет входящих запросов.*", BackKb("wrk_exchange"), ct);
            return;
        }

        string text = $"📥 *Входящие запросы ({requests.Count}):*\n\n";
        List<InlineKeyboardButton[]> rows = new();

        foreach (ShiftExchangeRequest req in requests)
        {
            string from   = $"{req.Requester?.LastName} {req.Requester?.FirstName}";
            string theirD = req.RequesterShift?.StartTime.ToString("dd.MM") ?? "?";
            string myD    = req.TargetShift?.StartTime.ToString("dd.MM") ?? "?";
            text += $"*{from}*: {theirD} ↔ {myD}\n_{req.RequestMessage ?? "Без сообщения"}_\n\n";
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Принять", $"wrk_exc_acc:{req.Id:N}"),
                InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"wrk_exc_rej:{req.Id:N}"),
            });
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "wrk_exchange") });
        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task ShowWorkerOutgoingAsync(long chatId, int msgId, Employee worker, CancellationToken ct)
    {
        var pending = (await m_ExchangeRepo.GetRequestsByRequesterAsync(worker.Id, ct))
            .Where(r => r.Status == ExchangeRequestStatus.Pending)
            .ToList();

        if (!pending.Any())
        {
            await EditMsgAsync(chatId, msgId, "📭 *Нет исходящих запросов.*", BackKb("wrk_exchange"), ct);
            return;
        }

        string text = $"📤 *Мои запросы ({pending.Count}):*\n\n";
        List<InlineKeyboardButton[]> rows = new();

        foreach (ShiftExchangeRequest req in pending)
        {
            string to  = $"{req.Target?.LastName} {req.Target?.FirstName}";
            string myD = req.RequesterShift?.StartTime.ToString("dd.MM") ?? "?";
            string thD = req.TargetShift?.StartTime.ToString("dd.MM") ?? "?";
            text += $"*{to}*: {myD} ↔ {thD} — ⏳\n\n";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"🗑 Отменить ({to})", $"wrk_exc_can:{req.Id:N}") });
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "wrk_exchange") });
        await EditMsgAsync(chatId, msgId, text, new InlineKeyboardMarkup(rows), ct);
    }

    private async Task RespondToExchangeAsync(long chatId, int msgId, Guid reqId, bool approve, Employee worker, CancellationToken ct)
    {
        ShiftExchangeRequest? req = await m_ExchangeRepo.GetByIdAsync(reqId, ct);
        if (req == null || req.Status != ExchangeRequestStatus.Pending || req.TargetId != worker.Id)
        {
            await EditMsgAsync(chatId, msgId, "⚠️ Запрос не найден или уже обработан.", BackKb("wrk_exchange"), ct);
            return;
        }

        if (approve) req.Approve();
        else req.Reject();

        await m_ExchangeRepo.UpdateAsync(req, ct);
        await m_ExchangeRepo.SaveChangesAsync(ct);

        string result = approve ? "✅ *Обмен принят!* Смены обменяны." : "❌ *Обмен отклонён.*";
        await EditMsgAsync(chatId, msgId, result,
            new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Обмен сменами", "wrk_exchange") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") },
            }),
            ct);

        await m_Notifications.SendShiftExchangeNotificationAsync(req, approve ? "approved" : "rejected", ct);
    }

    private async Task CancelWorkerExchangeAsync(long chatId, int msgId, Guid reqId, CancellationToken ct)
    {
        ShiftExchangeRequest? req = await m_ExchangeRepo.GetByIdAsync(reqId, ct);
        if (req == null || req.Status != ExchangeRequestStatus.Pending)
        {
            await EditMsgAsync(chatId, msgId, "⚠️ Запрос не найден или уже обработан.", BackKb("wrk_exchange"), ct);
            return;
        }

        req.Cancel();
        await m_ExchangeRepo.UpdateAsync(req, ct);
        await m_ExchangeRepo.SaveChangesAsync(ct);

        await EditMsgAsync(chatId, msgId, "🗑 *Запрос отменён.*", BackKb("wrk_exchange"), ct);
    }

    // ─── Shared helpers ────────────────────────────────────────────────────

    private async Task<Employee?> FindEmployeeAsync(User from, long chatId, CancellationToken ct)
    {
        TelegramUserSettings? settings = await m_SettingsRepo.GetByChatIdAsync(chatId, ct);
        if (settings != null)
            return await m_EmployeeRepo.GetByIdAsync(settings.EmployeeId, ct);

        IEnumerable<Employee> all = await m_EmployeeRepo.GetAllAsync(ct);
        return all.FirstOrDefault(e =>
            !string.IsNullOrEmpty(from.FirstName) &&
            e.FirstName.Equals(from.FirstName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(from.LastName) &&
            e.LastName.Equals(from.LastName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task EditMsgAsync(long chatId, int msgId, string text, InlineKeyboardMarkup kb, CancellationToken ct)
    {
        try
        {
            await m_BotClient.EditMessageText(chatId, msgId, text, ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
        }
        catch (Exception ex) when (ex.Message.Contains("message is not modified"))
        {
            // content unchanged — safe to ignore
        }
    }

    private async Task SaveSettingsAsync(TelegramUserSettings settings, CancellationToken ct)
    {
        await m_SettingsRepo.UpdateAsync(settings, ct);
        await m_SettingsRepo.SaveChangesAsync(ct);
    }

    private static InlineKeyboardMarkup MainMenuKb()
        => new(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main") } });

    private static InlineKeyboardMarkup BackKb(string callbackData)
        => new(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", callbackData) } });

    private static DateTime GetMonday(int weekOffset)
    {
        DateTime today = DateTime.UtcNow.Date;
        int diff = ((int)today.DayOfWeek - 1 + 7) % 7;
        return today.AddDays(-diff + weekOffset * 7);
    }

    private static string FormatDate(DateTime date)
    {
        string dow = date.DayOfWeek switch
        {
            DayOfWeek.Monday    => "Пн",
            DayOfWeek.Tuesday   => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday  => "Чт",
            DayOfWeek.Friday    => "Пт",
            DayOfWeek.Saturday  => "Сб",
            _                   => "Вс",
        };
        return $"{dow} {date:dd.MM}";
    }

    private static string ShiftTypeShort(ShiftType type) => type switch
    {
        ShiftType.Morning => "Утро",
        ShiftType.Day     => "День",
        ShiftType.Evening => "Вечер",
        ShiftType.FullDay => "Полный день",
        _                 => type.ToString(),
    };

    private async Task SendUnregisteredAsync(long chatId, CancellationToken ct)
        => await m_BotClient.SendMessage(chatId, "❌ Вы не зарегистрированы. Используйте /start.", cancellationToken: ct);

    private async Task SendStatusAsync(long chatId, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null) { await SendUnregisteredAsync(chatId, ct); return; }
        string status = settings.IsEnabled ? "✅ Включены" : "❌ Отключены";
        await m_BotClient.SendMessage(chatId,
            $"📊 *Статус:* {status}\n⏰ *Напоминание за:* {settings.NotificationMinutesBefore} мин.",
            ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task SendHelpAsync(long chatId, CancellationToken ct)
        => await m_BotClient.SendMessage(chatId,
            "*Команды:*\n\n/start — главное меню\n/settings — настройки\n/status — статус\n/enable — включить уведомления\n/disable — отключить уведомления\n/time [мин] — время напоминания\n/help — справка",
            ParseMode.Markdown, cancellationToken: ct);

    private async Task HandleTimeCommandAsync(long chatId, string command, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null) { await SendUnregisteredAsync(chatId, ct); return; }
        string[] parts = command.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[1], out int min) && min > 0 && min <= 1440)
        {
            settings.NotificationMinutesBefore = min;
            await SaveSettingsAsync(settings, ct);
            await m_BotClient.SendMessage(chatId, $"⏰ Напоминание за {min} мин.", cancellationToken: ct);
        }
        else
        {
            await m_BotClient.SendMessage(chatId, "❌ Используйте: /time [1-1440]", cancellationToken: ct);
        }
    }

    private async Task HandleTimezoneCommandAsync(long chatId, string command, TelegramUserSettings? settings, CancellationToken ct)
    {
        if (settings == null) { await SendUnregisteredAsync(chatId, ct); return; }
        string[] parts = command.Split(' ');
        if (parts.Length == 2)
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(parts[1]);
                settings.TimeZone = parts[1];
                await SaveSettingsAsync(settings, ct);
                await m_BotClient.SendMessage(chatId, $"🌍 Часовой пояс: {tz.DisplayName}", cancellationToken: ct);
            }
            catch
            {
                await m_BotClient.SendMessage(chatId, "❌ Неверный пояс. Пример: Europe/Moscow", cancellationToken: ct);
            }
        }
        else
        {
            await m_BotClient.SendMessage(chatId, "❌ Используйте: /timezone [зона]", cancellationToken: ct);
        }
    }
}
