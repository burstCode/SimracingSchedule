namespace SimRacingSchedule.Application.DTOs;

public record CreateShiftExchangeRequestDto(
    Guid RequesterId,
    Guid TargetId,
    Guid RequesterShiftId,
    Guid TargetShiftId,
    string? Message);
