using FixFlow.Api.DTOs;

namespace FixFlow.Api.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto> RegisterAsync(RegisterRequestDto request);
    Task<UserDto> GetCurrentUserAsync(string userEmail);
}

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<List<RoleDto>> GetRolesAsync();
}

public interface ILocationService
{
    Task<List<LocationDto>> GetAllLocationsAsync();
    Task<LocationDto> GetLocationByIdAsync(Guid id);
    Task<LocationDto> CreateLocationAsync(LocationCreateDto dto);
}

public interface IAssetService
{
    Task<List<AssetDto>> GetAllAssetsAsync();
    Task<AssetDto> GetAssetByIdAsync(Guid id);
    Task<AssetDto> CreateAssetAsync(AssetCreateDto dto);
}

public interface IAgentWorkflowService
{
    Task<List<AgentWorkflowDto>> GetWorkflowsAsync();
    Task<AgentWorkflowDto> GetWorkflowByIdAsync(Guid id);
}

public interface IApprovalService
{
    Task<List<ApprovalActionDto>> GetPendingApprovalsAsync();
    Task<ApprovalActionDto> ProcessApprovalAsync(Guid approvalId, ApprovalDecisionDto decision, Guid approverId);
}

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task SendNotificationAsync(Guid userId, string title, string message, string type = "Info");
}

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string entityName, string entityId, string detailsJson, string ipAddress);
}

public interface IReportsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
}

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<Stream> GetFileAsync(string fileKey);
}

public interface IDistanceMatrixService
{
    Task<double> GetDistanceInKmAsync(double originLat, double originLng, double destLat, double destLng);
}
