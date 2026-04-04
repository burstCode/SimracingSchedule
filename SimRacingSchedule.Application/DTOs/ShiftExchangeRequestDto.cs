namespace SimRacingSchedule.Application.DTOs;

public record ShiftExchangeRequestDto(
    Guid Id,
    Guid RequesterId,
    string RequesterName,
    Guid TargetId,
    string TargetName,
    Guid RequesterShiftId,
    DateTime RequesterShiftStart,
    DateTime RequesterShiftEnd,
    string RequesterShiftType,
    Guid TargetShiftId,
    DateTime TargetShiftStart,
    DateTime TargetShiftEnd,
    string TargetShiftType,
    string Status,
    string? RequestMessage,
    string? ResponseMessage,
    DateTime RequestedAt,
    DateTime? RespondedAt
);
