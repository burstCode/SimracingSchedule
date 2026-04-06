using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Employee?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<IEnumerable<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Employee>> GetByRoleAsync(EmployeeRole role, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
