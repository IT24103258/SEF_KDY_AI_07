using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(FixFlowDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Roles Seeding
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Administrator", Description = "System administrator with full access" },
                new Role { Name = "Manager", Description = "Operations manager with dispatch & approval rights" },
                new Role { Name = "Technician", Description = "Field technician executing work orders" },
                new Role { Name = "Requester", Description = "Campus staff or student submitting requests" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Administrator");
        var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
        var techRole = await context.Roles.FirstAsync(r => r.Name == "Technician");
        var requesterRole = await context.Roles.FirstAsync(r => r.Name == "Requester");

        // 2. Demo Users Seeding
        if (!await context.Users.AnyAsync())
        {
            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var managerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!");
            var techPasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!");
            var reqPasswordHash = BCrypt.Net.BCrypt.HashPassword("Requester123!");

            var adminUser = new User
            {
                Email = "admin@fixflow.local",
                PasswordHash = defaultPasswordHash,
                FirstName = "System",
                LastName = "Administrator",
                PhoneNumber = "+94770000001",
                RoleId = adminRole.Id
            };

            var managerUser = new User
            {
                Email = "manager@fixflow.local",
                PasswordHash = managerPasswordHash,
                FirstName = "Operations",
                LastName = "Manager",
                PhoneNumber = "+94770000002",
                RoleId = managerRole.Id
            };

            var techUser = new User
            {
                Email = "tech@fixflow.local",
                PasswordHash = techPasswordHash,
                FirstName = "Senior",
                LastName = "Technician",
                PhoneNumber = "+94770000003",
                RoleId = techRole.Id
            };

            var requesterUser = new User
            {
                Email = "requester@fixflow.local",
                PasswordHash = reqPasswordHash,
                FirstName = "Campus",
                LastName = "Requester",
                PhoneNumber = "+94770000004",
                RoleId = requesterRole.Id
            };

            await context.Users.AddRangeAsync(adminUser, managerUser, techUser, requesterUser);
            await context.SaveChangesAsync();

            // Seed Technician Profile
            var technician = new Technician
            {
                UserId = techUser.Id,
                EmployeeId = "TECH-001",
                Specialization = "Electrical & HVAC",
                IsAvailable = true,
                CurrentLatitude = 6.9147,
                CurrentLongitude = 79.9733
            };
            await context.Technicians.AddAsync(technician);
            await context.SaveChangesAsync();
        }

        // 3. Campus Locations Seeding
        if (!await context.Locations.AnyAsync())
        {
            var locations = new List<Location>
            {
                new Location { Name = "Main Computing Building", Building = "Block A", Floor = "2nd Floor", Room = "Lab A201", Latitude = 6.9147, Longitude = 79.9733 },
                new Location { Name = "Engineering Complex", Building = "Block B", Floor = "1st Floor", Room = "Workshop B105", Latitude = 6.9150, Longitude = 79.9740 },
                new Location { Name = "Central Library", Building = "Library Tower", Floor = "Ground Floor", Room = "Study Area", Latitude = 6.9142, Longitude = 79.9728 }
            };
            await context.Locations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }

        // 4. Campus Assets Seeding
        if (!await context.Assets.AnyAsync())
        {
            var locA = await context.Locations.FirstAsync(l => l.Building == "Block A");
            var assets = new List<Asset>
            {
                new Asset { Name = "Central HVAC Chiller A1", AssetCode = "HVAC-BLKA-01", Category = "HVAC", Criticality = "High", LocationId = locA.Id },
                new Asset { Name = "Main Server Room UPS", AssetCode = "PWR-BLKA-02", Category = "Electrical", Criticality = "Critical", LocationId = locA.Id }
            };
            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();
        }

        // 5. Issue Categories Seeding
        if (!await context.IssueCategories.AnyAsync())
        {
            var categories = new List<IssueCategory>
            {
                new IssueCategory { Name = "Electrical Breakdown", Description = "Power outages, short circuits, light failures", DefaultPriority = "High" },
                new IssueCategory { Name = "HVAC & Air Conditioning", Description = "AC cooling failures, ventilation issues", DefaultPriority = "Medium" },
                new IssueCategory { Name = "Plumbing & Water", Description = "Pipe leaks, drainage blockages, tap faults", DefaultPriority = "Medium" },
                new IssueCategory { Name = "Structural & Furniture", Description = "Broken doors, windows, desks, chairs", DefaultPriority = "Low" }
            };
            await context.IssueCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 6. Skills Seeding
        if (!await context.Skills.AnyAsync())
        {
            var skills = new List<Skill>
            {
                new Skill { Name = "High Voltage Electrical Systems", Category = "Electrical" },
                new Skill { Name = "HVAC Chiller Maintenance", Category = "HVAC" },
                new Skill { Name = "Commercial Plumbing Repair", Category = "Plumbing" }
            };
            await context.Skills.AddRangeAsync(skills);
            await context.SaveChangesAsync();
        }

        // 7. SLA Configurations Seeding
        if (!await context.SLAConfigurations.AnyAsync())
        {
            var slas = new List<SLAConfiguration>
            {
                new SLAConfiguration { PriorityLevel = "Critical", ResponseTimeHours = 1, ResolutionTimeHours = 4, EscalationEmail = "escalations@fixflow.local" },
                new SLAConfiguration { PriorityLevel = "High", ResponseTimeHours = 2, ResolutionTimeHours = 8, EscalationEmail = "manager@fixflow.local" },
                new SLAConfiguration { PriorityLevel = "Medium", ResponseTimeHours = 4, ResolutionTimeHours = 24, EscalationEmail = "helpdesk@fixflow.local" },
                new SLAConfiguration { PriorityLevel = "Low", ResponseTimeHours = 8, ResolutionTimeHours = 48, EscalationEmail = "helpdesk@fixflow.local" }
            };
            await context.SLAConfigurations.AddRangeAsync(slas);
            await context.SaveChangesAsync();
        }

        // 8. Sample Maintenance Request for Initial Workflow Demonstration
        if (!await context.MaintenanceRequests.AnyAsync())
        {
            var loc = await context.Locations.FirstAsync();
            var asset = await context.Assets.FirstAsync();
            var reqUser = await context.Users.FirstAsync(u => u.Email == "requester@fixflow.local");
            var cat = await context.IssueCategories.FirstAsync();

            var sampleRequest = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0001",
                Title = "Server Room AC Cooling Failure",
                Description = "The primary AC unit in Lab A201 is making loud abnormal noises and temperature is rising.",
                Status = RequestStatus.Submitted,
                LocationId = loc.Id,
                AssetId = asset.Id,
                RequesterId = reqUser.Id,
                CategoryId = cat.Id
            };
            await context.MaintenanceRequests.AddAsync(sampleRequest);
            await context.SaveChangesAsync();
        }
    }
}
