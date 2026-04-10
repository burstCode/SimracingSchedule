using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.Mappings;
using SimRacingSchedule.Infrastructure;
using SimRacingSchedule.Infrastructure.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static c => c.SwaggerDoc("v1", new () { Title = "SimRacing Schedule API", Version = "v1" }));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateShiftExchangeCommandHandler).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext db_context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db_context.Database.MigrateAsync();
        Console.WriteLine("✅ DB migiration completed successfully!");

        int employeesCount = await db_context.Employees.CountAsync();
        int shiftsCount = await db_context.Shifts.CountAsync();
        int exchangesCount = await db_context.ShiftExchangeRequests.CountAsync();

        Console.WriteLine($"📊 Database stats: {employeesCount} employees, {shiftsCount} shifts, {exchangesCount} exchange requests");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"❌ Database migration failed: {exception.Message}");
    }
}

// TODO: зарегистрировать сервис для отправки уведомлений в Telegram.

app.Run();
