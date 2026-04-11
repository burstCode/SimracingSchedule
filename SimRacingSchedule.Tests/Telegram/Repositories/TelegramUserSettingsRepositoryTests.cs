using Microsoft.EntityFrameworkCore;
using Moq;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Infrastructure.Data;
using SimRacingSchedule.Infrastructure.Repositories;
using Xunit;
using FluentAssertions;

namespace SimRacingSchedule.Tests.Telegram.Repositories;

public class TelegramUserSettingsRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext m_Context;
    private readonly ITelegramUserSettingsRepository m_Repository;

    public TelegramUserSettingsRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        m_Context = new ApplicationDbContext(options);
        m_Repository = new TelegramUserSettingsRepository(m_Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddSettings_WhenValid()
    {
        // Arrange
        TelegramUserSettings settings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TelegramChatId = 123456789,
            IsEnabled = true,
            NotificationMinutesBefore = 60
        };

        // Act
        await m_Repository.AddAsync(settings);
        await m_Repository.SaveChangesAsync();

        TelegramUserSettings? result = await m_Repository.GetByEmployeeIdAsync(settings.EmployeeId);

        // Assert
        result.Should().NotBeNull();
        result?.TelegramChatId.Should().Be(123456789);
        result?.IsEnabled.Should().BeTrue();
        result?.NotificationMinutesBefore.Should().Be(60);
    }

    [Fact]
    public async Task GetByChatIdAsync_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        long chatId = 987654321L;
        TelegramUserSettings settings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TelegramChatId = chatId,
            IsEnabled = true
        };
        
        await m_Repository.AddAsync(settings);
        await m_Repository.SaveChangesAsync();

        // Act
        TelegramUserSettings? result = await m_Repository.GetByChatIdAsync(chatId);

        // Assert
        result.Should().NotBeNull();
        result?.TelegramChatId.Should().Be(chatId);
    }

    [Fact]
    public async Task GetByEmployeeIdAsync_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        Guid employeeId = Guid.NewGuid();
        TelegramUserSettings settings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TelegramChatId = 555555555,
            IsEnabled = true
        };
        
        await m_Repository.AddAsync(settings);
        await m_Repository.SaveChangesAsync();

        // Act
        TelegramUserSettings? result = await m_Repository.GetByEmployeeIdAsync(employeeId);

        // Assert
        result.Should().NotBeNull();
        result?.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public async Task GetAllEnabledAsync_ShouldReturnOnlyEnabledSettings()
    {
        // Arrange
        TelegramUserSettings enabledSettings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TelegramChatId = 111111111,
            IsEnabled = true
        };

        TelegramUserSettings disabledSettings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TelegramChatId = 222222222,
            IsEnabled = false
        };
        
        await m_Repository.AddAsync(enabledSettings);
        await m_Repository.AddAsync(disabledSettings);
        await m_Repository.SaveChangesAsync();

        // Act
        IEnumerable<TelegramUserSettings> result = await m_Repository.GetAllEnabledAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().TelegramChatId.Should().Be(111111111);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateSettings()
    {
        // Arrange
        TelegramUserSettings settings = new TelegramUserSettings
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TelegramChatId = 123456789,
            IsEnabled = true,
            NotificationMinutesBefore = 60
        };
        
        await m_Repository.AddAsync(settings);
        await m_Repository.SaveChangesAsync();

        // Act
        settings.NotificationMinutesBefore = 120;
        settings.IsEnabled = false;
        await m_Repository.UpdateAsync(settings);
        await m_Repository.SaveChangesAsync();

        TelegramUserSettings? result = await m_Repository.GetByEmployeeIdAsync(settings.EmployeeId);

        // Assert
        result.Should().NotBeNull();
        result?.NotificationMinutesBefore.Should().Be(120);
        result?.IsEnabled.Should().BeFalse();
    }

    public void Dispose()
    {
        m_Context.Database.EnsureDeleted();
        m_Context.Dispose();
    }
}