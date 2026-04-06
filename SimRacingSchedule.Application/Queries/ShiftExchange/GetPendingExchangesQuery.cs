using AutoMapper;
using MediatR;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Queries.ShiftExchange;

public record GetPendingExchangesQuery(Guid EmployeeId) : IRequest<IEnumerable<ShiftExchangeRequestDto>>;

public class GetPendingExchangesQueryHandler : IRequestHandler<GetPendingExchangesQuery, IEnumerable<ShiftExchangeRequestDto>>
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IShiftExchangeRepository m_ExchangeRepository;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly IMapper m_Mapper;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public GetPendingExchangesQueryHandler(
        IShiftExchangeRepository exchangeRepository,
        IMapper mapper)
    {
        this.m_ExchangeRepository = exchangeRepository;
        this.m_Mapper = mapper;
    }

    public async Task<IEnumerable<ShiftExchangeRequestDto>> Handle(
        GetPendingExchangesQuery query,
        CancellationToken cancellationToken)
    {
        IEnumerable<ShiftExchangeRequest> requests =
            await this.m_ExchangeRepository.GetPendingRequestsForEmployeeAsync(query.EmployeeId, cancellationToken).ConfigureAwait(false);
        return this.m_Mapper.Map<IEnumerable<ShiftExchangeRequestDto>>(requests);
    }
}
