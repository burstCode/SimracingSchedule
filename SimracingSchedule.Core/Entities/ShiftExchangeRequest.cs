using SimRacingSchedule.Core.Exceptions;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Запрос на обмен сменами.
/// </summary>
public class ShiftExchangeRequest
{
    public Guid Id { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid TargetId { get; private set; }
    public Guid RequesterShiftId { get; private set; }
    public Guid TargetShiftId { get; private set; }
    public ExchangeRequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? ResponseMessage { get; private set; }

    private ShiftExchangeRequest() { }

    public ShiftExchangeRequest(
        Employee requester,
        Employee target,
        Shift requesterShift,
        Shift targetShift)
    {
        if (!requester.CanExchangeShift(requesterShift, target))
            throw new DomainException("Запришвающий сотрудник не может обменять эту смену");

        if (!target.CanExchangeShift(targetShift, requester))
            throw new DomainException("Целевой сотрудник не может обменять эту смену");

        Id = Guid.NewGuid();
        RequesterId = requester.Id;
        TargetId = target.Id;
        RequesterShiftId = requesterShift.Id;
        TargetShiftId = targetShift.Id;
        Status = ExchangeRequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public void Approve(string? message = null)
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new DomainException("Запрос уже обработан");

        Status = ExchangeRequestStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        ResponseMessage = message;
    }

    public void Reject(string? message = null)
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new DomainException("Запрос уже обработан");

        Status = ExchangeRequestStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
        ResponseMessage = message;
    }

    public void Cancel()
    {
        if (Status != ExchangeRequestStatus.Pending)
            throw new DomainException("Невозможно отменить обработанный запрос");

        Status = ExchangeRequestStatus.Cancelled;
    }
}
