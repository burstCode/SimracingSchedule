using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;
using SimRacingSchedule.Infrastructure.Repositories;

namespace SimRacingSchedule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        _ = services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        _ = services.AddScoped<IShiftRepository, ShiftRepository>();
        _ = services.AddScoped<IShiftExchangeRepository, ShiftExchangeRepository>();

        return services;
    }
}
