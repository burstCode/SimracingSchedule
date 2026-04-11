using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Application.Queries.ShiftExchange;
using SimRacingSchedule.Application.Telegram.Services;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftExchangeController : ControllerBase
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IMediator m_Mediator;
    private readonly ILogger<ShiftExchangeController> m_Logger;
    private readonly ITelegramNotificationService m_NotificationService;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public ShiftExchangeController(
        IMediator mediator, 
        ILogger<ShiftExchangeController> logger,
        ITelegramNotificationService notificationService)
    {
        this.m_Mediator = mediator;
        this.m_Logger = logger;
        this.m_NotificationService = notificationService;
    }

    /// <summary>
    /// Получить все ожидающие запросы на обмен для сотрудника
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("pending/{employeeId}")]
    [ProducesResponseType(typeof(IEnumerable<ShiftExchangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests(Guid employeeId)
    {
        GetPendingExchangesQuery query = new GetPendingExchangesQuery(employeeId);
        IEnumerable<ShiftExchangeRequestDto> requests = await this.m_Mediator.Send(query);
        return this.Ok(requests);
    }

    /// <summary>
    /// Создать запрос на обмен сменами.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

        CreateShiftExchangeResult result = await this.m_Mediator.Send(command);

        if (!result.Success)
        {
            return this.BadRequest(new { error = result.ErrorMessage });
        }

        this.m_Logger.LogInformation("Created shift exchange request {RequestId} from {RequesterId} to {TargetId}",
            result.RequestId, request.RequesterId, request.TargetId);

        // Отправляем уведомление через Telegram
        try
        {
            // Получаем созданный запрос из базы
            ShiftExchangeRequest? exchangeRequest = await this.GetExchangeRequestById(result.RequestId!.Value);
            if (exchangeRequest != null)
            {
                await this.m_NotificationService.SendShiftExchangeNotificationAsync(
                    exchangeRequest,
                    "created",
                    CancellationToken.None);
                this.m_Logger.LogInformation("Telegram notification sent for exchange request {RequestId}", result.RequestId);
            }
        }
        catch (Exception ex)
        {
            this.m_Logger.LogError(ex, "Failed to send Telegram notification for exchange request {RequestId}", result.RequestId);
        }

        return this.Ok(result);
    }

    /// <summary>
    /// Ответить на запрос обмена
    /// </summary>
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

        RespondToShiftExchangeResult result = await this.m_Mediator.Send(command);

        if (!result.Success)
        {
            return this.BadRequest(new { error = result.ErrorMessage });
        }

        this.m_Logger.LogInformation("Responded to exchange request {RequestId} with {Action}",
            request.RequestId, request.Approve ? "APPROVE" : "REJECT");

        // Отправляем уведомление о ответе на запрос
        try
        {
            ShiftExchangeRequest? exchangeRequest = await this.GetExchangeRequestById(request.RequestId);
            if (exchangeRequest != null)
            {
                string action = request.Approve ? "approved" : "rejected";
                await this.m_NotificationService.SendShiftExchangeNotificationAsync(
                    exchangeRequest,
                    action,
                    CancellationToken.None);
                this.m_Logger.LogInformation("Telegram notification sent for exchange response {RequestId}", request.RequestId);
            }
        }
        catch (Exception ex)
        {
            this.m_Logger.LogError(ex, "Failed to send Telegram notification for exchange response {RequestId}", request.RequestId);
        }

        return this.Ok(new { message = "Ответ успешно обработан" });
    }

    // Вспомогательный метод для получения запроса на обмен
#pragma warning disable S1172 // Unused method parameters should be removed
    private async Task<ShiftExchangeRequest?> GetExchangeRequestById(Guid requestId)
#pragma warning restore S1172 // Unused method parameters should be removed
    {
        // Здесь нужно использовать репозиторий
        // Временно заглушка - нужно добавить IShiftExchangeRepository в конструктор
        // Или вызвать через mediator запрос GetExchangeRequestQuery
        // Временное решение - возвращаем null
        // TODO: Добавить нормальную реализацию через репозиторий или CQRS запрос
        return null;
    }
}
