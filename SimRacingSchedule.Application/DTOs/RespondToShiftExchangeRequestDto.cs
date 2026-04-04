namespace SimRacingSchedule.Application.DTOs;

public record RespondToShiftExchangeRequestDto(
    Guid RequestId,
    bool Approve,
    string? Message
);
