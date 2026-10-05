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
                new Role { Name = "Requester", Description = "Apartment resident or staff submitting requests" }
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
                FirstName = "Property",
                LastName = "Administrator",
                PhoneNumber = "+94770000001",
                RoleId = adminRole.Id
            };

            var managerUser = new User
            {
                Email = "manager@fixflow.local",
                PasswordHash = managerPasswordHash,
                FirstName = "Property",
                LastName = "Manager",
                PhoneNumber = "+94770000002",
                RoleId = managerRole.Id
            };

            var techUser = new User
            {
                Email = "tech@fixflow.local",
                PasswordHash = techPasswordHash,
                FirstName = "Maintenance",
                LastName = "Technician",
                PhoneNumber = "+94770000003",
                RoleId = techRole.Id
            };

            var requesterUser = new User
            {
                Email = "requester@fixflow.local",
                PasswordHash = reqPasswordHash,
                FirstName = "Apartment",
                LastName = "Resident",
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

        // 3. Apartment Complex Locations Seeding (Single Complex Hierarchy)
        if (!await context.Locations.AnyAsync())
        {
            var locations = new List<Location>
            {
                new Location { Name = "Tower A - Unit 305", Building = "Tower A", Floor = "Floor 3", Room = "Unit 305 - Bathroom", Latitude = 6.9147, Longitude = 79.9733 },
                new Location { Name = "Tower A - Unit 204", Building = "Tower A", Floor = "Floor 2", Room = "Unit 204 - Kitchen", Latitude = 6.9147, Longitude = 79.9733 },
                new Location { Name = "Tower B - Lobby", Building = "Tower B", Floor = "Floor 1", Room = "Common Area - Lobby", Latitude = 6.9150, Longitude = 79.9740 },
                new Location { Name = "Tower B - Floor 2 Corridor", Building = "Tower B", Floor = "Floor 2", Room = "Common Area - Corridor", Latitude = 6.9150, Longitude = 79.9740 },
                new Location { Name = "Common Area - Pool", Building = "Common Areas", Floor = "Ground Floor", Room = "Pool & Pump House", Latitude = 6.9142, Longitude = 79.9728 },
                new Location { Name = "Common Area - Parking", Building = "Common Areas", Floor = "Basement", Room = "Parking Area B1", Latitude = 6.9142, Longitude = 79.9728 }
            };
            await context.Locations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }

        // 4. Apartment Complex Assets Seeding
        if (!await context.Assets.AnyAsync())
        {
            var locTowerA = await context.Locations.FirstAsync(l => l.Building == "Tower A");
            var locTowerB = await context.Locations.FirstAsync(l => l.Building == "Tower B");
            var locCommon = await context.Locations.FirstAsync(l => l.Building == "Common Areas");

            var assets = new List<Asset>
            {
                new Asset { Name = "Tower A Passenger Elevator", AssetCode = "ELEV-TWRA-01", Category = "Elevator/Lift", Criticality = "Critical", LocationId = locTowerA.Id },
                new Asset { Name = "Tower B Lobby Air Conditioner", AssetCode = "HVAC-TWRB-01", Category = "HVAC", Criticality = "Medium", LocationId = locTowerB.Id },
                new Asset { Name = "Main Water Booster Pump System", AssetCode = "PUMP-CMN-01", Category = "Water Supply", Criticality = "High", LocationId = locCommon.Id },
                new Asset { Name = "Backup Diesel Generator", AssetCode = "PWR-GEN-01", Category = "Electrical", Criticality = "Critical", LocationId = locCommon.Id }
            };
            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();
        }

        // 5. Issue Categories Seeding
        if (!await context.IssueCategories.AnyAsync())
        {
            var categories = new List<IssueCategory>
            {
                new IssueCategory { Name = "Electrical", Description = "Power outages, short circuits, lighting failures, panel issues", DefaultPriority = "High" },
                new IssueCategory { Name = "HVAC", Description = "Air conditioning cooling failures, ventilation issues, thermostat faults", DefaultPriority = "Medium" },
                new IssueCategory { Name = "Plumbing", Description = "Pipe leaks, drainage blockages, tap faults, water pressure issues", DefaultPriority = "Medium" },
                new IssueCategory { Name = "Elevator/Lift", Description = "Elevator stoppage, abnormal noises, door sensor faults", DefaultPriority = "Critical" },
                new IssueCategory { Name = "Water Supply", Description = "Water pump malfunction, tank overflow, pressure drops", DefaultPriority = "High" },
                new IssueCategory { Name = "Common Area", Description = "Corridor lights, gym equipment, pool maintenance, parking gate", DefaultPriority = "Low" },
                new IssueCategory { Name = "Structural", Description = "Broken doors, windows, locks, ceiling cracks, wall damage", DefaultPriority = "Low" }
            };
            await context.IssueCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 6. Skills Seeding
        if (!await context.Skills.AnyAsync())
        {
            var skills = new List<Skill>
            {
                new Skill { Name = "Residential Electrical Systems", Category = "Electrical" },
                new Skill { Name = "HVAC & AC Maintenance", Category = "HVAC" },
                new Skill { Name = "Residential Plumbing & Drainage Repair", Category = "Plumbing" },
                new Skill { Name = "Elevator & Lift Maintenance", Category = "Elevator/Lift" }
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
            var loc = await context.Locations.FirstAsync(l => l.Building == "Tower A");
            var asset = await context.Assets.FirstAsync(a => a.Category == "HVAC");
            var reqUser = await context.Users.FirstAsync(u => u.Email == "requester@fixflow.local");
            var cat = await context.IssueCategories.FirstAsync(c => c.Name == "HVAC");

            var sampleRequest = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0001",
                Title = "The AC in the Tower A lobby isn't cooling",
                Description = "The main AC unit in the Tower A lobby is not cooling properly and is making an unusual rattling noise.",
                Status = RequestStatus.Submitted,
                LocationId = loc.Id,
                AssetId = asset.Id,
                RequesterId = reqUser.Id,
                CategoryId = cat.Id
            };
            await context.MaintenanceRequests.AddAsync(sampleRequest);
            await context.SaveChangesAsync();
        }

        // 9. Component 4 — Scheduling & Work Orders Seeding
        await WorkOrderSeeder.SeedWorkOrdersAsync(context);
    }
}
