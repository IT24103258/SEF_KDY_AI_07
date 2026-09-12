using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserDto>.SuccessResult(user));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IUserService _userService;

    public RolesController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var roles = await _userService.GetRolesAsync();
        return Ok(ApiResponse<List<RoleDto>>.SuccessResult(roles));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LocationDto>>>> GetLocations()
    {
        var locations = await _locationService.GetAllLocationsAsync();
        return Ok(ApiResponse<List<LocationDto>>.SuccessResult(locations));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetLocationById(Guid id)
    {
        var location = await _locationService.GetLocationByIdAsync(id);
        return Ok(ApiResponse<LocationDto>.SuccessResult(location));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> CreateLocation([FromBody] LocationCreateDto dto)
    {
        var location = await _locationService.CreateLocationAsync(dto);
        return Ok(ApiResponse<LocationDto>.SuccessResult(location, "Location created successfully."));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AssetDto>>>> GetAssets()
    {
        var assets = await _assetService.GetAllAssetsAsync();
        return Ok(ApiResponse<List<AssetDto>>.SuccessResult(assets));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAssetById(Guid id)
    {
        var asset = await _assetService.GetAssetByIdAsync(id);
        return Ok(ApiResponse<AssetDto>.SuccessResult(asset));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset([FromBody] AssetCreateDto dto)
    {
        var asset = await _assetService.CreateAssetAsync(dto);
        return Ok(ApiResponse<AssetDto>.SuccessResult(asset, "Asset registered successfully."));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetMyNotifications()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(ApiResponse<List<NotificationDto>>.SuccessResult(notifications));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var summary = await _reportsService.GetDashboardSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.SuccessResult(summary));
    }
}
