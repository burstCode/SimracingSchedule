// Copyright (c) SimRacing Club. All rights reserved.

using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Объект обмена сменами.
/// </summary>
public class ShiftExchangeRequest
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ShiftExchangeRequest"/>.
    /// </summary>
    /// <param name="requester">Объект запрашивающего сотрудника.</param>
    /// <param name="target">Объект целевого сотрудника.</param>
    /// <param name="requesterShift">Объект смены запрашивающего сотрудника.</param>
    /// <param name="targetShift">Целевой объект смены запрашиваюещго сотрудника.</param>
    /// <param name="requestMessage">Сообщение от запрашивающего.</param>
    public ShiftExchangeRequest(
        Employee requester,
        Employee target,
        Shift requesterShift,
        Shift targetShift,
        string? requestMessage = null)
    {
        if (requester == null)
        {
            throw new ArgumentNullException(nameof(requester));
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (requesterShift == null)
        {
            throw new ArgumentNullException(nameof(requesterShift));
        }

        if (targetShift == null)
        {
            throw new ArgumentNullException(nameof(targetShift));
        }

        if (!requester.CanExchangeShift(requesterShift, target))
        {
            throw new InvalidOperationException("Requester cannot exchange this shift");
        }

        if (!target.CanExchangeShift(targetShift, requester))
        {
            throw new InvalidOperationException("Target cannot exchange this shift");
        }

        this.Id = Guid.NewGuid();
        this.RequesterId = requester.Id;
        this.TargetId = target.Id;
        this.RequesterShiftId = requesterShift.Id;
        this.TargetShiftId = targetShift.Id;
        this.Status = ExchangeRequestStatus.Pending;
        this.RequestMessage = requestMessage;
        this.RequestedAt = DateTime.UtcNow;
    }

    private ShiftExchangeRequest()
    {
    }

    /// <summary>
    /// Получает идентификатор.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Получает идентификатор запрашивающего обмен.
    /// </summary>
    public Guid RequesterId { get; private set; }

    /// <summary>
    /// Получает объект запрашивающего сотрудника.
    /// </summary>
    public Employee? Requester { get; }

    /// <summary>
    /// Получает идентификатор целевого сотрудника.
    /// </summary>
    public Guid TargetId { get; private set; }

    /// <summary>
    /// Получает объект целевого сотрудника.
    /// </summary>
    public Employee? Target { get; }

    /// <summary>
    /// Получает идентификатор смены запрашивающего сотрудника.
    /// </summary>
    public Guid RequesterShiftId { get; private set; }

    /// <summary>
    /// Получает объект исходной смены (у запрашивающего).
    /// </summary>
    public Shift? RequesterShift { get; }

    /// <summary>
    /// Получает идентификатор целевой смены.
    /// </summary>
    public Guid TargetShiftId { get; private set; }

    /// <summary>
    /// Получает объект целевой смены.
    /// </summary>
    public Shift? TargetShift { get; }

    /// <summary>
    /// Получает статус обмена.
    /// </summary>
    public ExchangeRequestStatus Status { get; private set; }

    /// <summary>
    /// Получает сообщение от запрашивающего.
    /// </summary>
    public string? RequestMessage { get; private set; }

    /// <summary>
    /// Получает ответное сообщение.
    /// </summary>
    public string? ResponseMessage { get; private set; }

    /// <summary>
    /// Получает время запроса.
    /// </summary>
    public DateTime RequestedAt { get; private set; }

    /// <summary>
    /// Получает время ответа на запрос.
    /// </summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>
    /// Принятие запроса на обмен.
    /// </summary>
    /// <param name="responseMessage">
    /// Ответное сообщение.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// При попытке принять уже обработанный запрос.
    /// </exception>
    public void Approve(string? responseMessage = null)
    {
        if (this.Status != ExchangeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve request with status {this.Status}");
        }

        this.Status = ExchangeRequestStatus.Approved;
        this.ResponseMessage = responseMessage;
        this.RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклонение запроса на обмен.
    /// </summary>
    /// <param name="responseMessage">
    /// Ответное сообщение.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// При попытке отклонить уже обработанный запрос.
    /// </exception>
    public void Reject(string? responseMessage = null)
    {
        if (this.Status != ExchangeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject request with status {this.Status}");
        }

        this.Status = ExchangeRequestStatus.Rejected;
        this.ResponseMessage = responseMessage;
        this.RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отмена запроса на обмен.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// При попытке отменить уже обработанный запрос.
    /// </exception>
    public void Cancel()
    {
        if (this.Status != ExchangeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel request with status {this.Status}");
        }

        this.Status = ExchangeRequestStatus.Cancelled;
        this.RespondedAt = DateTime.UtcNow;
    }
}
