using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Interfaces;

public interface IShiftExchangeRepository : IRepository<ShiftExchangeRequest>
{
    Task<IEnumerable<ShiftExchangeRequest>> GetPendingRequestsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftExchangeRequest>> GetRequestsByRequesterAsync(Guid requesterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftExchangeRequest>> GetRequestsByTargetAsync(Guid targetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftExchangeRequest>> GetByStatusAsync(ExchangeRequestStatus status, CancellationToken cancellationToken = default);
    Task<ShiftExchangeRequest?> GetPendingRequestBetweenEmployeesAsync(Guid requesterId, Guid targetId, CancellationToken cancellationToken = default);
}
