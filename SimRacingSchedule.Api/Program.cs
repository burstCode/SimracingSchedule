using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.Mappings;
using SimRacingSchedule.Infrastructure;
using SimRacingSchedule.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
{
    Title = "SimRacing Schedule API",
    Version = "v1"
}));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateShiftExchangeCommandHandler).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ DB migiration completed successfully!");

        var employeesCount = await dbContext.Employees.CountAsync();
        var shiftsCount = await dbContext.Shifts.CountAsync();
        var exchangesCount = await dbContext.ShiftExchangeRequests.CountAsync();

        Console.WriteLine($"📊 Database stats: {employeesCount} employees, {shiftsCount} shifts, {exchangesCount} exchange requests");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"❌ Database migration failed: {exception.Message}");
    }
}
// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast = Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast")
// .WithOpenApi();

app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
