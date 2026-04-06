using MediatR;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Commands.ShiftExchange;

public record RespondToShiftExchangeCommand(
    Guid RequestId,
    bool Approve,
    string? Message) : IRequest<RespondToShiftExchangeResult>;

public record RespondToShiftExchangeResult(
    bool Success,
    string? ErrorMessage);

public class RespondToShiftExchangeCommandHandler : IRequestHandler<RespondToShiftExchangeCommand, RespondToShiftExchangeResult>
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IShiftExchangeRepository m_ExchangeRepository;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public RespondToShiftExchangeCommandHandler(IShiftExchangeRepository exchangeRepository)
    {
        this.m_ExchangeRepository = exchangeRepository;
    }

    public async Task<RespondToShiftExchangeResult> Handle(
        RespondToShiftExchangeCommand command,
        CancellationToken cancellationToken)
    {
        ShiftExchangeRequest? exchangeRequest = await this.m_ExchangeRepository.GetByIdAsync(command.RequestId, cancellationToken)
        .ConfigureAwait(false);

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

            await this.m_ExchangeRepository.UpdateAsync(exchangeRequest, cancellationToken).ConfigureAwait(false);
            await this.m_ExchangeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RespondToShiftExchangeResult(true, null);
        }
        catch (InvalidOperationException ex)
        {
            return new RespondToShiftExchangeResult(false, ex.Message);
        }
    }
}
