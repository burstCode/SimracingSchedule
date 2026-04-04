using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Application.Commands.ShiftExchange;
using SimRacingSchedule.Application.Queries.ShiftExchange;

namespace SimRacingSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftExchangeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShiftExchangeController> _logger;

    public ShiftExchangeController(IMediator mediator, ILogger<ShiftExchangeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Получить все ожидающие запросы на обмен для сотрудника
    /// </summary>
    [HttpGet("pending/{employeeId}")]
    [ProducesResponseType(typeof(IEnumerable<ShiftExchangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests(Guid employeeId)
    {
        GetPendingExchangesQuery query = new(employeeId);
        var requests = await _mediator.Send(query);
        return Ok(requests);
    }

    /// <summary>
    /// Создать запрос на обмен сменами
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateShiftExchangeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExchangeRequest([FromBody] CreateShiftExchangeRequestDto request)
    {
        var command = new CreateShiftExchangeCommand(
            request.RequesterId,
            request.TargetId,
            request.RequesterShiftId,
            request.TargetShiftId,
            request.Message
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Created shift exchange request {RequestId} from {RequesterId} to {TargetId}",
            result.RequestId, request.RequesterId, request.TargetId);

        return Ok(result);
    }

    /// <summary>
    /// Ответить на запрос обмена
    /// </summary>
    [HttpPost("respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondToExchange([FromBody] RespondToShiftExchangeRequestDto request)
    {
        var command = new RespondToShiftExchangeCommand(
            request.RequestId,
            request.Approve,
            request.Message
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Responded to exchange request {RequestId} with {Action}",
            request.RequestId, request.Approve ? "APPROVE" : "REJECT");

        return Ok(new { message = "Ответ успешно обработан" });
    }
}
