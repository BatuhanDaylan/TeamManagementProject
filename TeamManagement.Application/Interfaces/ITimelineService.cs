using TeamManagement.Application.DTOs.Timeline;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface ITimelineService
    {
        Task<Result<List<TimelineGorevDto>>> GetTimelineByEkipIdAsync(Guid ekipId);
    }
}
