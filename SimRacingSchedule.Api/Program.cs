using Microsoft.OpenApi.Extensions;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Создание сотрудника
var employee = new Employee(
    "Иван",
    "Петров",
    "ivan@simracing.com",
    "+79001234567",
    "Инструктор",
    EmployeeRole.Employee);

Console.WriteLine($"Сотрудник: {employee.FirstName} {employee.LastName}");
Console.WriteLine($"Роль: {employee.Role.GetDisplayName()}");

// Создание смены
var shiftDate = DateTime.Today.AddDays(7); // Через неделю
var shift = new Shift(employee.Id, shiftDate, ShiftType.FullDay);

Console.WriteLine($"Смена: {shift.Type.GetDisplayName()}");
Console.WriteLine($"Время: {shift.StartTime:HH:mm} - {shift.EndTime:HH:mm}");
Console.WriteLine($"Статус: {shift.Status.GetDisplayName()}");

// Проверка длительности смены
var duration = Shift.GetShiftDuration(ShiftType.FullDay);
Console.WriteLine($"Длительность: {duration.Hours} часов");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
