using MediatR;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Commands.ShiftExchange;

public record RespondToShiftExchangeCommand(
    Guid RequestId,
    bool Approve,
    string? Message
) : IRequest<RespondToShiftExchangeResult>;

public record RespondToShiftExchangeResult(
    bool Success,
    string? ErrorMessage
);

public class RespondToShiftExchangeCommandHandler : IRequestHandler<RespondToShiftExchangeCommand, RespondToShiftExchangeResult>
{
    private readonly IShiftExchangeRepository _exchangeRepository;

    public RespondToShiftExchangeCommandHandler(IShiftExchangeRepository exchangeRepository)
    {
        _exchangeRepository = exchangeRepository;
    }

    public async Task<RespondToShiftExchangeResult> Handle(
        RespondToShiftExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var exchangeRequest = await _exchangeRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (exchangeRequest == null)
        {
            return new RespondToShiftExchangeResult(false, "Запрос на обмен не найден");
        }

        try
        {
            if (command.Approve)
            {
                exchangeRequest.Approve(command.Message);
            }
            else
            {
                exchangeRequest.Reject(command.Message);
            }

            await _exchangeRepository.UpdateAsync(exchangeRequest, cancellationToken);
            await _exchangeRepository.SaveChangesAsync(cancellationToken);

            return new RespondToShiftExchangeResult(true, null);
        }
        catch (InvalidOperationException ex)
        {
            return new RespondToShiftExchangeResult(false, ex.Message);
        }
    }
}
