using MediatR;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Commands.ShiftExchange;

public record CreateShiftExchangeCommand(
    Guid RequesterId,
    Guid TargetId,
    Guid RequesterShiftId,
    Guid TargetShiftId,
    string? Message) : IRequest<CreateShiftExchangeResult>;

public record CreateShiftExchangeResult(
    bool Success,
    Guid? RequestId,
    string? ErrorMessage);

public class CreateShiftExchangeCommandHandler : IRequestHandler<CreateShiftExchangeCommand, CreateShiftExchangeResult>
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IShiftExchangeRepository m_ExchangeRepository;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IEmployeeRepository m_EmployeeRepository;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IShiftRepository m_ShiftRepository;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public CreateShiftExchangeCommandHandler(
        IShiftExchangeRepository exchangeRepository,
        IEmployeeRepository employeeRepository,
        IShiftRepository shiftRepository)
    {
        this.m_ExchangeRepository = exchangeRepository;
        this.m_EmployeeRepository = employeeRepository;
        this.m_ShiftRepository = shiftRepository;
    }

    public async Task<CreateShiftExchangeResult> Handle(
        CreateShiftExchangeCommand command,
        CancellationToken cancellationToken)
    {
        Employee? requester = await this.m_EmployeeRepository.GetByIdAsync(command.RequesterId, cancellationToken).ConfigureAwait(false);
        Employee? target = await this.m_EmployeeRepository.GetByIdAsync(command.TargetId, cancellationToken).ConfigureAwait(false);
        Shift? requesterShift = await this.m_ShiftRepository.GetByIdAsync(command.RequesterShiftId, cancellationToken).ConfigureAwait(false);
        Shift? targetShift = await this.m_ShiftRepository.GetByIdAsync(command.TargetShiftId, cancellationToken).ConfigureAwait(false);

        if (requester == null || target == null || requesterShift == null || targetShift == null)
        {
            return new CreateShiftExchangeResult(false, null, "Один из объектов не найден");
        }

        try
        {
            ShiftExchangeRequest exchangeRequest = new ShiftExchangeRequest(
                requester, target, requesterShift, targetShift, command.Message);

            await this.m_ExchangeRepository.AddAsync(exchangeRequest, cancellationToken).ConfigureAwait(false);
            await this.m_ExchangeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new CreateShiftExchangeResult(true, exchangeRequest.Id, null);
        }
        catch (InvalidOperationException ex)
        {
            return new CreateShiftExchangeResult(false, null, ex.Message);
        }
    }
}
