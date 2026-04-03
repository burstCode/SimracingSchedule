using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

public class ShiftExchangeRequest
{
    public Guid Id { get; private set; }
    public Guid RequesterId { get; private set; }
    public Employee? Requester { get; private set; }
    public Guid TargetId { get; private set; }
    public Employee? Target { get; private set; }
    public Guid RequesterShiftId { get; private set; }
    public Shift? RequesterShift { get; private set; }
    public Guid TargetShiftId { get; private set; }
    public Shift? TargetShift { get; private set; }
    public ExchangeRequestStatus Status { get; private set; }
    public string? RequestMessage { get; private set; }
    public string? ResponseMessage { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    private ShiftExchangeRequest() { } // EF Core

    public ShiftExchangeRequest(
        Employee requester,
        Employee target,
        Shift requesterShift,
        Shift targetShift,
        string? requestMessage = null)
    {
        if (requester == null) throw new ArgumentNullException(nameof(requester));
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (requesterShift == null) throw new ArgumentNullException(nameof(requesterShift));
        if (targetShift == null) throw new ArgumentNullException(nameof(targetShift));

        if (!requester.CanExchangeShift(requesterShift, target))
            throw new InvalidOperationException("Requester cannot exchange this shift");

        if (!target.CanExchangeShift(targetShift, requester))
            throw new InvalidOperationException("Target cannot exchange this shift");

        Id = Guid.NewGuid();
        RequesterId = requester.Id;
        TargetId = target.Id;
        RequesterShiftId = requesterShift.Id;
        TargetShiftId = targetShift.Id;
        Status = ExchangeRequestStatus.Pending;
        RequestMessage = requestMessage;
        RequestedAt = DateTime.UtcNow;
    }

    public void Approve(string? responseMessage = null)
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve request with status {Status}");

        Status = ExchangeRequestStatus.Approved;
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;

        ExchangeShifts();
    }

    public void Reject(string? responseMessage = null)
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject request with status {Status}");

        Status = ExchangeRequestStatus.Rejected;
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel request with status {Status}");

        Status = ExchangeRequestStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
    }

    private void ExchangeShifts()
    {
        // Сохраняем оригинальные данные
        var requesterShiftId = RequesterShiftId;
        var targetShiftId = TargetShiftId;

        // Здесь должна быть логика обмена сменами
        // В реальном приложении лучше сделать через доменный сервис
        // или использовать event sourcing

        // Для простоты, отметим что обмен произошел
        // В production коде нужно добавить транзакционность
    }
}
