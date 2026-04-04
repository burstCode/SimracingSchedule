using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _context.Employees
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
                e.IsActive
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id}/shifts")]
    public async Task<IActionResult> GetEmployeeShifts(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.Date;
        var endDate = to ?? DateTime.UtcNow.Date.AddDays(30);

        var shifts = await _context.Shifts
            .Where(s => s.EmployeeId == id && s.StartTime >= startDate && s.StartTime <= endDate)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.StartTime,
                s.EndTime,
                Type = s.Type.ToString(),
                Status = s.Status.ToString(),
                s.Notes
            })
            .ToListAsync();

        return Ok(shifts);
    }
}
