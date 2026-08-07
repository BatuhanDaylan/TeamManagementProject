using TeamManagement.Application.DTOs.Dashboard;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<Result<List<UyeGorevOzetiDto>>> GetDashboardByEkipIdAsync(Guid ekipId);
    }
}
