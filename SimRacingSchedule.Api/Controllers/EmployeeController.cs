using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly ApplicationDbContext m_Context;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public EmployeesController(ApplicationDbContext context)
    {
        this.m_Context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await this.m_Context.Employees
            .Where(e => e.IsActive)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.Patronymic,
                e.Email,
                e.PhoneNumber,
                e.Position,
                Role = e.Role.ToString(),
                e.IsActive,
            })
            .ToListAsync();

        return this.Ok(employees);
    }

    [HttpGet("{id}/shifts")]
    public async Task<IActionResult> GetEmployeeShifts(
    Guid id,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to)
    {
        try
        {
            DateTime startDate = from.HasValue
                ? DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc)
                : DateTime.UtcNow.Date;

            DateTime endDate = to.HasValue
                ? DateTime.SpecifyKind(to.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                : DateTime.UtcNow.Date.AddDays(30);

            // 1. Все ОДОБРЕННЫЕ обмены
            var approvedExchanges = await this.m_Context.ShiftExchangeRequests
                .Where(r => r.Status == ExchangeRequestStatus.Approved)
                .Select(r => new
                {
                    r.RequesterId,
                    r.TargetId,
                    RequesterShiftId = r.RequesterShiftId,
                    TargetShiftId = r.TargetShiftId,
                    RequesterName = r.Requester!.FirstName + " " + r.Requester.LastName,
                    TargetName = r.Target!.FirstName + " " + r.Target.LastName,
                })
                .ToListAsync();

            // 2. Словари: кто кому какую смену передал
            Dictionary<Guid, Guid> shiftToNewOwner = new Dictionary<Guid, Guid>(); // shiftId -> новый владелец
            Dictionary<Guid, Guid> shiftFromOldOwner = new Dictionary<Guid, Guid>(); // shiftId -> старый владелец

            foreach (var exchange in approvedExchanges)
            {
                // Смена запросившего (Владислав) переходит к цели (Веронике)
                shiftToNewOwner[exchange.RequesterShiftId] = exchange.TargetId;
                shiftFromOldOwner[exchange.RequesterShiftId] = exchange.RequesterId;

                // Смена цели (Вероника) переходит к запросившему (Владиславу)
                shiftToNewOwner[exchange.TargetShiftId] = exchange.RequesterId;
                shiftFromOldOwner[exchange.TargetShiftId] = exchange.TargetId;
            }

            // 3. Получаем ВСЕ смены в нужном диапазоне
            List<Shift> allShifts = await this.m_Context.Shifts
                .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                .ToListAsync();

            List<object> result = new List<object>();

            foreach (Shift? shift in allShifts)
            {
                // Проверяем, кому сейчас принадлежит эта смена
                if (shiftToNewOwner.TryGetValue(shift.Id, out Guid currentOwnerId))
                {
                    // Смена была передана
                    if (currentOwnerId == id)
                    {
                        // Эта смена ПРИНАДЛЕЖИТ текущему сотруднику (получена от другого)
                        Guid originalOwnerId = shiftFromOldOwner[shift.Id];
                        Employee? originalOwner = await this.m_Context.Employees.FindAsync(originalOwnerId);
                        string originalOwnerName = originalOwner != null
                            ? originalOwner.FirstName + " " + originalOwner.LastName
                            : "Unknown";

                        result.Add(new
                        {
                            shift.Id,
                            shift.StartTime,
                            shift.EndTime,
                            Type = shift.Type.ToString(),
                            Status = shift.Status.ToString(),
                            shift.Notes,
                            Source = $"Получена от {originalOwnerName}",
                            ExchangedWithId = originalOwnerId,
                            ExchangedWithName = originalOwnerName,
                        });
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (shift.EmployeeId == id)
                {
                    // Своя смена, которая не участвовала в обменах
                    result.Add(new
                    {
                        shift.Id,
                        shift.StartTime,
                        shift.EndTime,
                        Type = shift.Type.ToString(),
                        Status = shift.Status.ToString(),
                        shift.Notes,
                        Source = "Собственная",
                        ExchangedWithId = (Guid?)null,
                        ExchangedWithName = (string?)null,
                    });
                }
            }

            result = result.OrderBy(x => ((dynamic)x).StartTime).ToList();

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
