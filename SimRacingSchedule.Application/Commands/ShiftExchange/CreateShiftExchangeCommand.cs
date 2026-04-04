using MediatR;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Commands.ShiftExchange;

public record CreateShiftExchangeCommand(
    Guid RequesterId,
    Guid TargetId,
    Guid RequesterShiftId,
    Guid TargetShiftId,
    string? Message
) : IRequest<CreateShiftExchangeResult>;

public record CreateShiftExchangeResult(
    bool Success,
    Guid? RequestId,
    string? ErrorMessage
);

public class CreateShiftExchangeCommandHandler : IRequestHandler<CreateShiftExchangeCommand, CreateShiftExchangeResult>
{
    private readonly IShiftExchangeRepository _exchangeRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IShiftRepository _shiftRepository;

    public CreateShiftExchangeCommandHandler(
        IShiftExchangeRepository exchangeRepository,
        IEmployeeRepository employeeRepository,
        IShiftRepository shiftRepository)
    {
        _exchangeRepository = exchangeRepository;
        _employeeRepository = employeeRepository;
        _shiftRepository = shiftRepository;
    }

    public async Task<CreateShiftExchangeResult> Handle(
        CreateShiftExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var requester = await _employeeRepository.GetByIdAsync(command.RequesterId, cancellationToken);
        var target = await _employeeRepository.GetByIdAsync(command.TargetId, cancellationToken);
        var requesterShift = await _shiftRepository.GetByIdAsync(command.RequesterShiftId, cancellationToken);
        var targetShift = await _shiftRepository.GetByIdAsync(command.TargetShiftId, cancellationToken);

        if (requester == null || target == null || requesterShift == null || targetShift == null)
        {
            return new CreateShiftExchangeResult(false, null, "Один из объектов не найден");
        }

        try
        {
            var exchangeRequest = new ShiftExchangeRequest(
                requester, target, requesterShift, targetShift, command.Message);

            await _exchangeRepository.AddAsync(exchangeRequest, cancellationToken);
            await _exchangeRepository.SaveChangesAsync(cancellationToken);

            return new CreateShiftExchangeResult(true, exchangeRequest.Id, null);
        }
        catch (InvalidOperationException ex)
        {
            return new CreateShiftExchangeResult(false, null, ex.Message);
        }
    }
}
