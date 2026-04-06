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
#pragma warning disable SA1201 // Elements should appear in the correct order
    public ShiftExchangeRequestDto() { }
#pragma warning restore SA1201 // Elements should appear in the correct order

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
        this.Id = id;
        this.RequesterId = requesterId;
        this.RequesterName = requesterName;
        this.TargetId = targetId;
        this.TargetName = targetName;
        this.RequesterShiftId = requesterShiftId;
        this.RequesterShiftStart = requesterShiftStart;
        this.RequesterShiftEnd = requesterShiftEnd;
        this.RequesterShiftType = requesterShiftType;
        this.TargetShiftId = targetShiftId;
        this.TargetShiftStart = targetShiftStart;
        this.TargetShiftEnd = targetShiftEnd;
        this.TargetShiftType = targetShiftType;
        this.Status = status;
        this.RequestMessage = requestMessage;
        this.ResponseMessage = responseMessage;
        this.RequestedAt = requestedAt;
        this.RespondedAt = respondedAt;
    }
}
