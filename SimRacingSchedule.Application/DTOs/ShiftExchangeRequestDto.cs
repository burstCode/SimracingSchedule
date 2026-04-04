namespace SimRacingSchedule.Application.DTOs;

public class ShiftExchangeRequestDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public Guid RequesterShiftId { get; set; }
    public DateTime RequesterShiftStart { get; set; }
    public DateTime RequesterShiftEnd { get; set; }
    public string RequesterShiftType { get; set; } = string.Empty;
    public Guid TargetShiftId { get; set; }
    public DateTime TargetShiftStart { get; set; }
    public DateTime TargetShiftEnd { get; set; }
    public string TargetShiftType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RequestMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Конструктор без параметров для AutoMapper
    public ShiftExchangeRequestDto() { }

    // Конструктор со всеми параметрами (опционально)
    public ShiftExchangeRequestDto(
        Guid id,
        Guid requesterId,
        string requesterName,
        Guid targetId,
        string targetName,
        Guid requesterShiftId,
        DateTime requesterShiftStart,
        DateTime requesterShiftEnd,
        string requesterShiftType,
        Guid targetShiftId,
        DateTime targetShiftStart,
        DateTime targetShiftEnd,
        string targetShiftType,
        string status,
        string? requestMessage,
        string? responseMessage,
        DateTime requestedAt,
        DateTime? respondedAt)
    {
        Id = id;
        RequesterId = requesterId;
        RequesterName = requesterName;
        TargetId = targetId;
        TargetName = targetName;
        RequesterShiftId = requesterShiftId;
        RequesterShiftStart = requesterShiftStart;
        RequesterShiftEnd = requesterShiftEnd;
        RequesterShiftType = requesterShiftType;
        TargetShiftId = targetShiftId;
        TargetShiftStart = targetShiftStart;
        TargetShiftEnd = targetShiftEnd;
        TargetShiftType = targetShiftType;
        Status = status;
        RequestMessage = requestMessage;
        ResponseMessage = responseMessage;
        RequestedAt = requestedAt;
        RespondedAt = respondedAt;
    }
}
