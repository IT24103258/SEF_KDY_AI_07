using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class UserService : IUserService
{
    private readonly FixFlowDbContext _context;

    public UserService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role != null ? u.Role.Name : "Requester"
            })
            .ToListAsync();
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) throw new NotFoundException($"User with ID '{id}' was not found.");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role?.Name ?? "Requester"
        };
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        return await _context.Roles
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync();
    }
}

public class LocationService : ILocationService
{
    private readonly FixFlowDbContext _context;

    public LocationService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<LocationDto>> GetAllLocationsAsync()
    {
        return await _context.Locations
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Building = l.Building,
                Floor = l.Floor,
                Room = l.Room,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            })
            .ToListAsync();
    }

    public async Task<LocationDto> GetLocationByIdAsync(Guid id)
    {
        var loc = await _context.Locations.FindAsync(id);
        if (loc == null) throw new NotFoundException($"Location '{id}' not found.");

        return new LocationDto
        {
            Id = loc.Id,
            Name = loc.Name,
            Building = loc.Building,
            Floor = loc.Floor,
            Room = loc.Room,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude
        };
    }

    public async Task<LocationDto> CreateLocationAsync(LocationCreateDto dto)
    {
        var loc = new Location
        {
            Name = dto.Name,
            Building = dto.Building,
            Floor = dto.Floor,
            Room = dto.Room,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        await _context.Locations.AddAsync(loc);
        await _context.SaveChangesAsync();

        return new LocationDto
        {
            Id = loc.Id,
            Name = loc.Name,
            Building = loc.Building,
            Floor = loc.Floor,
            Room = loc.Room,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude
        };
    }
}

public class AssetService : IAssetService
{
    private readonly FixFlowDbContext _context;

    public AssetService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetDto>> GetAllAssetsAsync()
    {
        return await _context.Assets
            .Include(a => a.Location)
            .Select(a => new AssetDto
            {
                Id = a.Id,
                Name = a.Name,
                AssetCode = a.AssetCode,
                Category = a.Category,
                Criticality = a.Criticality,
                LocationId = a.LocationId,
                LocationName = a.Location != null ? a.Location.Name : string.Empty
            })
            .ToListAsync();
    }

    public async Task<AssetDto> GetAssetByIdAsync(Guid id)
    {
        var asset = await _context.Assets.Include(a => a.Location).FirstOrDefaultAsync(a => a.Id == id);
        if (asset == null) throw new NotFoundException($"Asset '{id}' not found.");

        return new AssetDto
        {
            Id = asset.Id,
            Name = asset.Name,
            AssetCode = asset.AssetCode,
            Category = asset.Category,
            Criticality = asset.Criticality,
            LocationId = asset.LocationId,
            LocationName = asset.Location?.Name ?? string.Empty
        };
    }

    public async Task<AssetDto> CreateAssetAsync(AssetCreateDto dto)
    {
        var asset = new Asset
        {
            Name = dto.Name,
            AssetCode = dto.AssetCode,
            Category = dto.Category,
            Criticality = dto.Criticality,
            LocationId = dto.LocationId
        };

        await _context.Assets.AddAsync(asset);
        await _context.SaveChangesAsync();

        var location = await _context.Locations.FindAsync(dto.LocationId);

        return new AssetDto
        {
            Id = asset.Id,
            Name = asset.Name,
            AssetCode = asset.AssetCode,
            Category = asset.Category,
            Criticality = asset.Criticality,
            LocationId = asset.LocationId,
            LocationName = location?.Name ?? string.Empty
        };
    }
}

public class ReportsService : IReportsService
{
    private readonly FixFlowDbContext _context;

    public ReportsService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var totalRequests = await _context.MaintenanceRequests.CountAsync();
        var pendingApprovals = await _context.ApprovalActions.CountAsync(a => a.Status == ApprovalStatus.Pending);
        var activeWorkflows = await _context.AgentWorkflows.CountAsync(w => w.Status == WorkflowStatus.Running || w.Status == WorkflowStatus.WaitingForApproval);
        var completedWorkOrders = await _context.MaintenanceRequests.CountAsync(r => r.Status == RequestStatus.Completed);
        var highRiskEscalations = await _context.MaintenanceRequests.CountAsync(r => r.Status == RequestStatus.InReview);

        return new DashboardSummaryDto
        {
            TotalRequests = totalRequests,
            PendingApprovals = pendingApprovals,
            ActiveWorkflows = activeWorkflows,
            CompletedWorkOrders = completedWorkOrders,
            HighRiskEscalations = highRiskEscalations
        };
    }
}

public class NotificationService : INotificationService
{
    private readonly FixFlowDbContext _context;

    public NotificationService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, string type = "Info")
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly FixFlowDbContext _context;

    public AuditLogService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid? userId, string action, string entityName, string entityId, string detailsJson, string ipAddress)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            ChangesJson = detailsJson,
            IpAddress = ipAddress
        };

        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _uploadDirectory = configuration["FileStorage:UploadDirectory"] ?? "./uploads";
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

        using (var destinationStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destinationStream);
        }

        return uniqueFileName;
    }

    public Task<Stream> GetFileAsync(string fileKey)
    {
        var filePath = Path.Combine(_uploadDirectory, fileKey);
        if (!File.Exists(filePath)) throw new NotFoundException("File not found.");

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }
}
