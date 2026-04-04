using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Interfaces;

public interface IShiftRepository : IRepository<Shift>
{
    Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetAvailableForExchangeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid employeeId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
