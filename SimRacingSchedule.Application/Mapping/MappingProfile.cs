using AutoMapper;
using SimRacingSchedule.Application.DTOs;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Extensions;

namespace SimRacingSchedule.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        _ = this.CreateMap<ShiftExchangeRequest, ShiftExchangeRequestDto>()
            .ForMember(dest => dest.RequesterName,
                opt => opt.MapFrom(src => $"{src.Requester!.FirstName} {src.Requester.LastName}"))
            .ForMember(dest => dest.TargetName,
                opt => opt.MapFrom(src => $"{src.Target!.FirstName} {src.Target.LastName}"))
            .ForMember(dest => dest.RequesterShiftType,
                opt => opt.MapFrom(src => src.RequesterShift!.Type.GetDisplayName()))
            .ForMember(dest => dest.TargetShiftType,
                opt => opt.MapFrom(src => src.TargetShift!.Type.GetDisplayName()))
            .ForMember(dest => dest.RequesterShiftStart,
                opt => opt.MapFrom(src => src.RequesterShift!.StartTime))
            .ForMember(dest => dest.RequesterShiftEnd,
                opt => opt.MapFrom(src => src.RequesterShift!.EndTime))
            .ForMember(dest => dest.TargetShiftStart,
                opt => opt.MapFrom(src => src.TargetShift!.StartTime))
            .ForMember(dest => dest.TargetShiftEnd,
                opt => opt.MapFrom(src => src.TargetShift!.EndTime))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.GetDisplayName()));
    }
}
