using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Application.Queries.ShiftExchange;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftExchangeController : ControllerBase
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IMediator m_Mediator;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly ILogger<ShiftExchangeController> m_Logger;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public ShiftExchangeController(IMediator mediator, ILogger<ShiftExchangeController> logger)
    {
        this.m_Mediator = mediator;
        this.m_Logger = logger;
    }

    /// <summary>
    /// Получить все ожидающие запросы на обмен для сотрудника
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("pending/{employeeId}")]
    [ProducesResponseType(typeof(IEnumerable<ShiftExchangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests(Guid employeeId)
    {
        GetPendingExchangesQuery query = new (employeeId);
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

        return this.Ok(new { message = "Ответ успешно обработан" });
    }
}
