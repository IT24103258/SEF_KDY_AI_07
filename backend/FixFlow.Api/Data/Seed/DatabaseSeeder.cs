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
                new Role
                {
                    Name = "Administrator",
                    Description = "System administrator with full access"
                },
                new Role
                {
                    Name = "Manager",
                    Description = "Operations manager with dispatch & approval rights"
                },
                new Role
                {
                    Name = "Technician",
                    Description = "Field technician executing work orders"
                },
                new Role
                {
                    Name = "Requester",
                    Description = "Apartment resident or staff submitting requests"
                }
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

            await context.Users.AddRangeAsync(
                adminUser,
                managerUser,
                techUser,
                requesterUser
            );

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

        // 3. Apartment Complex Locations Seeding
        //
        // Structure:
        // Tower A
        //   Floor 1: Units 101-105
        //   Floor 2: Units 201-205
        //   Floor 3: Units 301-305
        //
        // Tower B
        //   Floor 1: Units 101-105
        //   Floor 2: Units 201-205
        //   Floor 3: Units 301-305
        //
        // Shared locations are intentionally limited to:
        //   - Tower A Main Lobby
        //   - Tower B Main Lobby
        //   - Common Area Pool
        //   - Common Area Basement Parking
        //
        // Apartments themselves are locations. Kitchen/Bathroom are not
        // separate seeded locations.
        if (!await context.Locations.AnyAsync())
        {
            var locations = new List<Location>
            {
                // =========================================================
                // TOWER A - FLOOR 1
                // =========================================================
                new Location
                {
                    Name = "Tower A - Unit 101",
                    Building = "Tower A",
                    Floor = "Floor 1",
                    Room = "Unit 101",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 102",
                    Building = "Tower A",
                    Floor = "Floor 1",
                    Room = "Unit 102",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 103",
                    Building = "Tower A",
                    Floor = "Floor 1",
                    Room = "Unit 103",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 104",
                    Building = "Tower A",
                    Floor = "Floor 1",
                    Room = "Unit 104",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 105",
                    Building = "Tower A",
                    Floor = "Floor 1",
                    Room = "Unit 105",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },

                // =========================================================
                // TOWER A - FLOOR 2
                // =========================================================
                new Location
                {
                    Name = "Tower A - Unit 201",
                    Building = "Tower A",
                    Floor = "Floor 2",
                    Room = "Unit 201",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 202",
                    Building = "Tower A",
                    Floor = "Floor 2",
                    Room = "Unit 202",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 203",
                    Building = "Tower A",
                    Floor = "Floor 2",
                    Room = "Unit 203",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 204",
                    Building = "Tower A",
                    Floor = "Floor 2",
                    Room = "Unit 204",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 205",
                    Building = "Tower A",
                    Floor = "Floor 2",
                    Room = "Unit 205",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },

                // =========================================================
                // TOWER A - FLOOR 3
                // =========================================================
                new Location
                {
                    Name = "Tower A - Unit 301",
                    Building = "Tower A",
                    Floor = "Floor 3",
                    Room = "Unit 301",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 302",
                    Building = "Tower A",
                    Floor = "Floor 3",
                    Room = "Unit 302",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 303",
                    Building = "Tower A",
                    Floor = "Floor 3",
                    Room = "Unit 303",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 304",
                    Building = "Tower A",
                    Floor = "Floor 3",
                    Room = "Unit 304",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },
                new Location
                {
                    Name = "Tower A - Unit 305",
                    Building = "Tower A",
                    Floor = "Floor 3",
                    Room = "Unit 305",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },

                // =========================================================
                // TOWER A - SHARED
                // =========================================================
                new Location
                {
                    Name = "Tower A - Main Lobby",
                    Building = "Tower A",
                    Floor = "Ground Floor",
                    Room = "Main Lobby",
                    Latitude = 6.9147,
                    Longitude = 79.9733
                },

                // =========================================================
                // TOWER B - FLOOR 1
                // =========================================================
                new Location
                {
                    Name = "Tower B - Unit 101",
                    Building = "Tower B",
                    Floor = "Floor 1",
                    Room = "Unit 101",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 102",
                    Building = "Tower B",
                    Floor = "Floor 1",
                    Room = "Unit 102",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 103",
                    Building = "Tower B",
                    Floor = "Floor 1",
                    Room = "Unit 103",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 104",
                    Building = "Tower B",
                    Floor = "Floor 1",
                    Room = "Unit 104",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 105",
                    Building = "Tower B",
                    Floor = "Floor 1",
                    Room = "Unit 105",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },

                // =========================================================
                // TOWER B - FLOOR 2
                // =========================================================
                new Location
                {
                    Name = "Tower B - Unit 201",
                    Building = "Tower B",
                    Floor = "Floor 2",
                    Room = "Unit 201",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 202",
                    Building = "Tower B",
                    Floor = "Floor 2",
                    Room = "Unit 202",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 203",
                    Building = "Tower B",
                    Floor = "Floor 2",
                    Room = "Unit 203",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 204",
                    Building = "Tower B",
                    Floor = "Floor 2",
                    Room = "Unit 204",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 205",
                    Building = "Tower B",
                    Floor = "Floor 2",
                    Room = "Unit 205",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },

                // =========================================================
                // TOWER B - FLOOR 3
                // =========================================================
                new Location
                {
                    Name = "Tower B - Unit 301",
                    Building = "Tower B",
                    Floor = "Floor 3",
                    Room = "Unit 301",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 302",
                    Building = "Tower B",
                    Floor = "Floor 3",
                    Room = "Unit 302",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 303",
                    Building = "Tower B",
                    Floor = "Floor 3",
                    Room = "Unit 303",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 304",
                    Building = "Tower B",
                    Floor = "Floor 3",
                    Room = "Unit 304",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },
                new Location
                {
                    Name = "Tower B - Unit 305",
                    Building = "Tower B",
                    Floor = "Floor 3",
                    Room = "Unit 305",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },

                // =========================================================
                // TOWER B - SHARED
                // =========================================================
                new Location
                {
                    Name = "Tower B - Main Lobby",
                    Building = "Tower B",
                    Floor = "Ground Floor",
                    Room = "Main Lobby",
                    Latitude = 6.9150,
                    Longitude = 79.9740
                },

                // =========================================================
                // COMMON AREAS
                // =========================================================
                new Location
                {
                    Name = "Common Area - Pool",
                    Building = "Common Areas",
                    Floor = "Ground Floor",
                    Room = "Pool",
                    Latitude = 6.9142,
                    Longitude = 79.9728
                },
                new Location
                {
                    Name = "Common Area - Basement Parking",
                    Building = "Common Areas",
                    Floor = "Basement",
                    Room = "Basement Parking",
                    Latitude = 6.9142,
                    Longitude = 79.9728
                }
            };

            await context.Locations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }

        // 4. Apartment Complex Assets Seeding
        //
        // Asset names are intentionally generic.
        // Their LocationId determines where the asset is installed.
        //
        // No location-specific asset names such as:
        // "Tower A Passenger Elevator"
        // "Tower B Lobby Air Conditioner"
        //
        // Instead:
        // "Passenger Elevator"
        // "Air Conditioner"
        if (!await context.Assets.AnyAsync())
        {
            // Apartment assets
            var towerAUnit102 = await context.Locations
                .FirstAsync(l => l.Name == "Tower A - Unit 102");

            var towerBUnit204 = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Unit 204");

            // Lobby assets
            var towerALobby = await context.Locations
                .FirstAsync(l => l.Name == "Tower A - Main Lobby");

            var towerBLobby = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Main Lobby");

            // Common-area assets
            var poolLocation = await context.Locations
                .FirstAsync(l => l.Name == "Common Area - Pool");

            var basementParking = await context.Locations
                .FirstAsync(l => l.Name == "Common Area - Basement Parking");

            var assets = new List<Asset>
            {
                // ---------------------------------------------------------
                // APARTMENT ASSETS
                // ---------------------------------------------------------
                new Asset
                {
                    Name = "Refrigerator",
                    AssetCode = "APP-REF-001",
                    Category = "Appliance",
                    Criticality = "Medium",
                    LocationId = towerAUnit102.Id
                },
                new Asset
                {
                    Name = "Circuit Breaker",
                    AssetCode = "ELEC-CB-001",
                    Category = "Electrical",
                    Criticality = "High",
                    LocationId = towerAUnit102.Id
                },
                new Asset
                {
                    Name = "Washing Machine",
                    AssetCode = "APP-WM-001",
                    Category = "Appliance",
                    Criticality = "Medium",
                    LocationId = towerBUnit204.Id
                },
                new Asset
                {
                    Name = "Water Heater",
                    AssetCode = "PLB-WH-001",
                    Category = "Water Supply",
                    Criticality = "High",
                    LocationId = towerBUnit204.Id
                },

                // ---------------------------------------------------------
                // TOWER A MAIN LOBBY
                // ---------------------------------------------------------
                new Asset
                {
                    Name = "Passenger Elevator",
                    AssetCode = "ELEV-001",
                    Category = "Elevator/Lift",
                    Criticality = "Critical",
                    LocationId = towerALobby.Id
                },
                new Asset
                {
                    Name = "Smoke Detector",
                    AssetCode = "FIRE-SD-001",
                    Category = "Fire Safety",
                    Criticality = "High",
                    LocationId = towerALobby.Id
                },

                // ---------------------------------------------------------
                // TOWER B MAIN LOBBY
                // ---------------------------------------------------------
                new Asset
                {
                    Name = "Air Conditioner",
                    AssetCode = "HVAC-001",
                    Category = "HVAC",
                    Criticality = "Medium",
                    LocationId = towerBLobby.Id
                },
                new Asset
                {
                    Name = "CCTV Camera",
                    AssetCode = "SEC-CCTV-001",
                    Category = "Security",
                    Criticality = "High",
                    LocationId = towerBLobby.Id
                },

                // ---------------------------------------------------------
                // COMMON AREA - POOL
                // ---------------------------------------------------------
                new Asset
                {
                    Name = "Water Booster Pump",
                    AssetCode = "PUMP-001",
                    Category = "Water Supply",
                    Criticality = "High",
                    LocationId = poolLocation.Id
                },

                // ---------------------------------------------------------
                // COMMON AREA - BASEMENT PARKING
                // ---------------------------------------------------------
                new Asset
                {
                    Name = "Main Electrical Panel",
                    AssetCode = "ELEC-MP-001",
                    Category = "Electrical",
                    Criticality = "Critical",
                    LocationId = basementParking.Id
                },
                new Asset
                {
                    Name = "Emergency Generator",
                    AssetCode = "PWR-GEN-001",
                    Category = "Electrical",
                    Criticality = "Critical",
                    LocationId = basementParking.Id
                },
                new Asset
                {
                    Name = "Fire Alarm Panel",
                    AssetCode = "FIRE-FAP-001",
                    Category = "Fire Safety",
                    Criticality = "Critical",
                    LocationId = basementParking.Id
                },
                new Asset
                {
                    Name = "Gate Motor",
                    AssetCode = "GATE-001",
                    Category = "Common Area",
                    Criticality = "High",
                    LocationId = basementParking.Id
                }
            };

            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();
        }

        // 5. Issue Categories Seeding
        if (!await context.IssueCategories.AnyAsync())
        {
            var categories = new List<IssueCategory>
            {
                new IssueCategory
                {
                    Name = "Electrical",
                    Description = "Power outages, short circuits, lighting failures, panel issues",
                    DefaultPriority = "High"
                },
                new IssueCategory
                {
                    Name = "HVAC",
                    Description = "Air conditioning cooling failures, ventilation issues, thermostat faults",
                    DefaultPriority = "Medium"
                },
                new IssueCategory
                {
                    Name = "Plumbing",
                    Description = "Pipe leaks, drainage blockages, tap faults, water pressure issues",
                    DefaultPriority = "Medium"
                },
                new IssueCategory
                {
                    Name = "Elevator/Lift",
                    Description = "Elevator stoppage, abnormal noises, door sensor faults",
                    DefaultPriority = "Critical"
                },
                new IssueCategory
                {
                    Name = "Water Supply",
                    Description = "Water pump malfunction, tank overflow, pressure drops",
                    DefaultPriority = "High"
                },
                new IssueCategory
                {
                    Name = "Common Area",
                    Description = "Corridor lights, gym equipment, pool maintenance, parking gate",
                    DefaultPriority = "Low"
                },
                new IssueCategory
                {
                    Name = "Structural",
                    Description = "Broken doors, windows, locks, ceiling cracks, wall damage",
                    DefaultPriority = "Low"
                }
            };

            await context.IssueCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 6. Skills Seeding
        if (!await context.Skills.AnyAsync())
        {
            var skills = new List<Skill>
            {
                new Skill
                {
                    Name = "Residential Electrical Systems",
                    Category = "Electrical"
                },
                new Skill
                {
                    Name = "HVAC & AC Maintenance",
                    Category = "HVAC"
                },
                new Skill
                {
                    Name = "Residential Plumbing & Drainage Repair",
                    Category = "Plumbing"
                },
                new Skill
                {
                    Name = "Elevator & Lift Maintenance",
                    Category = "Elevator/Lift"
                }
            };

            await context.Skills.AddRangeAsync(skills);
            await context.SaveChangesAsync();
        }

        // 7. SLA Configurations Seeding
        if (!await context.SLAConfigurations.AnyAsync())
        {
            var slas = new List<SLAConfiguration>
            {
                new SLAConfiguration
                {
                    PriorityLevel = "Critical",
                    ResponseTimeHours = 1,
                    ResolutionTimeHours = 4,
                    EscalationEmail = "escalations@fixflow.local"
                },
                new SLAConfiguration
                {
                    PriorityLevel = "High",
                    ResponseTimeHours = 2,
                    ResolutionTimeHours = 8,
                    EscalationEmail = "manager@fixflow.local"
                },
                new SLAConfiguration
                {
                    PriorityLevel = "Medium",
                    ResponseTimeHours = 4,
                    ResolutionTimeHours = 24,
                    EscalationEmail = "helpdesk@fixflow.local"
                },
                new SLAConfiguration
                {
                    PriorityLevel = "Low",
                    ResponseTimeHours = 8,
                    ResolutionTimeHours = 48,
                    EscalationEmail = "helpdesk@fixflow.local"
                }
            };

            await context.SLAConfigurations.AddRangeAsync(slas);
            await context.SaveChangesAsync();
        }

        // 8. Sample Maintenance Request for Initial Workflow Demonstration
        //
        // This request is deliberately consistent:
        // Location -> Tower B Main Lobby
        // Asset    -> Air Conditioner
        // Category -> HVAC
        if (!await context.MaintenanceRequests.AnyAsync())
        {
            var loc = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Main Lobby");

            var asset = await context.Assets
                .FirstAsync(a => a.AssetCode == "HVAC-001");

            var reqUser = await context.Users
                .FirstAsync(u => u.Email == "requester@fixflow.local");

            var cat = await context.IssueCategories
                .FirstAsync(c => c.Name == "HVAC");

            var sampleRequest = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0001",
                Title = "The air conditioner in the Tower B lobby isn't cooling",
                Description = "The air conditioner in the Tower B main lobby is not cooling properly and is making an unusual rattling noise.",
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