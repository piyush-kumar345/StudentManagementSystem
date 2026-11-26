using StudentManagementSystem.API.DTOs.Dashboard;

namespace StudentManagementSystem.API.Interfaces;

/// <summary>
/// Defines aggregated analytics operations for the administrator dashboard.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
