using AutoMapper;
using MediatR;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Core.Interfaces;

namespace SimRacingSchedule.Application.Queries.ShiftExchange;

public record GetPendingExchangesQuery(Guid EmployeeId) : IRequest<IEnumerable<ShiftExchangeRequestDto>>;

public class GetPendingExchangesQueryHandler : IRequestHandler<GetPendingExchangesQuery, IEnumerable<ShiftExchangeRequestDto>>
{
    private readonly IShiftExchangeRepository _exchangeRepository;
    private readonly IMapper _mapper;

    public GetPendingExchangesQueryHandler(
        IShiftExchangeRepository exchangeRepository,
        IMapper mapper)
    {
        _exchangeRepository = exchangeRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ShiftExchangeRequestDto>> Handle(
        GetPendingExchangesQuery query,
        CancellationToken cancellationToken)
    {
        var requests = await _exchangeRepository.GetPendingRequestsForEmployeeAsync(query.EmployeeId, cancellationToken);
        return _mapper.Map<IEnumerable<ShiftExchangeRequestDto>>(requests);
    }
}
