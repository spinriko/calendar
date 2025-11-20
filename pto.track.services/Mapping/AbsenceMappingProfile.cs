using AutoMapper;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services.Mapping;

/// <summary>
/// AutoMapper profile for absence-related entity to DTO mappings.
/// </summary>
public class AbsenceMappingProfile : Profile
{
    public AbsenceMappingProfile()
    {
        CreateMap<AbsenceRequest, AbsenceRequestDto>()
            .ForMember(dest => dest.EmployeeName,
                opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Name : "Unknown"))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ApproverName,
                opt => opt.MapFrom(src => src.Approver != null ? src.Approver.Name : null));

        CreateMap<CreateAbsenceRequestDto, AbsenceRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => AbsenceStatus.Pending))
            .ForMember(dest => dest.RequestedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Employee, opt => opt.Ignore())
            .ForMember(dest => dest.ApproverId, opt => opt.Ignore())
            .ForMember(dest => dest.Approver, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalComments, opt => opt.Ignore());

        CreateMap<UpdateAbsenceRequestDto, AbsenceRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Employee, opt => opt.Ignore())
            .ForMember(dest => dest.ApproverId, opt => opt.Ignore())
            .ForMember(dest => dest.Approver, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalComments, opt => opt.Ignore());
    }
}
