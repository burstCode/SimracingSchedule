using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Application.Queries.ShiftExchange;
using SimRacingSchedule.Application.Telegram.Services;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftExchangeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShiftExchangeController> _logger;
    private readonly ITelegramNotificationService _notificationService;
    private readonly IShiftExchangeRepository _exchangeRepository;

    public ShiftExchangeController(
        IMediator mediator,
        ILogger<ShiftExchangeController> logger,
        ITelegramNotificationService notificationService,
        IShiftExchangeRepository shiftExchangeRepository)
    {
        this._mediator = mediator;
        this._logger = logger;
        this._notificationService = notificationService;
        this._exchangeRepository = shiftExchangeRepository;
    }

    /// <summary>
    /// Получить все ожидающие запросы на обмен для сотрудника.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("pending/{employeeId}")]
    [ProducesResponseType(typeof(IEnumerable<ShiftExchangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests(Guid employeeId)
    {
        GetPendingExchangesQuery query = new GetPendingExchangesQuery(employeeId);
        IEnumerable<ShiftExchangeRequestDto> requests = await this._mediator.Send(query);
        return this.Ok(requests);
    }

    /// <summary>
    /// Ответить на запрос обмена.
    /// </summary>
    /// <param name="request">Объект запроса.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost("respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondToExchange([FromBody] RespondToShiftExchangeRequestDto request)
    {
        RespondToShiftExchangeCommand command = new RespondToShiftExchangeCommand(
            request.RequestId,
            request.Approve,
            request.Message);

        RespondToShiftExchangeResult result = await this._mediator.Send(command);

        if (!result.Success)
        {
            return this.BadRequest(new { error = result.ErrorMessage });
        }

        this._logger.LogInformation("Responded to exchange request {RequestId} with {Action}",
            request.RequestId, request.Approve ? "APPROVE" : "REJECT");

        // Отправляем уведомление о ответе на запрос
        try
        {
            ShiftExchangeRequest? exchangeRequest = await this.GetExchangeRequestById(request.RequestId);
            if (exchangeRequest != null)
            {
                string action = request.Approve ? "approved" : "rejected";
                await this._notificationService.SendShiftExchangeNotificationAsync(
                    exchangeRequest,
                    action,
                    CancellationToken.None);
                this._logger.LogInformation("Telegram notification sent for exchange response {RequestId}", request.RequestId);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to send Telegram notification for exchange response {RequestId}", request.RequestId);
        }

        return this.Ok(new { message = "Ответ успешно обработан" });
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateShiftExchangeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExchangeRequest([FromBody] CreateShiftExchangeRequestDto request)
    {
        CreateShiftExchangeCommand command = new CreateShiftExchangeCommand(
            request.RequesterId,
            request.TargetId,
            request.RequesterShiftId,
            request.TargetShiftId,
            request.Message);

        CreateShiftExchangeResult result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Created shift exchange request {RequestId} from {RequesterId} to {TargetId}",
            result.RequestId, request.RequesterId, request.TargetId);

        // ========== ДОБАВЬ ЭТОТ БЛОК ==========
        // Отправляем уведомление о создании запроса
        try
        {
            if (result.RequestId.HasValue)
            {
                var exchangeRequest = await _exchangeRepository.GetByIdAsync(result.RequestId.Value);
                if (exchangeRequest != null)
                {
                    await _notificationService.SendShiftExchangeNotificationAsync(
                        exchangeRequest,
                        "created",
                        CancellationToken.None);
                    _logger.LogInformation("✅ Telegram notifications sent for exchange request {RequestId}", result.RequestId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send Telegram notifications for exchange request {RequestId}", result.RequestId);
        }
        // =====================================

        return Ok(result);
    }

    // Вспомогательный метод для получения запроса на обмен
#pragma warning disable S1172 // Unused method parameters should be removed
    private Task<ShiftExchangeRequest?> GetExchangeRequestById(Guid requestId)
#pragma warning restore S1172 // Unused method parameters should be removed
    {
        // Здесь нужно использовать репозиторий
        // Временно заглушка - нужно добавить IShiftExchangeRepository в конструктор
        // Или вызвать через mediator запрос GetExchangeRequestQuery
        // Временное решение - возвращаем null
        // TODO: Добавить нормальную реализацию через репозиторий или CQRS запрос
        return Task.FromResult<ShiftExchangeRequest?>(null);
    }
}
