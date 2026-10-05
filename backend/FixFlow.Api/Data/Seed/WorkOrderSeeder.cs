using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Data.Seed;

public static class WorkOrderSeeder
{
    public static async Task SeedWorkOrdersAsync(FixFlowDbContext context)
    {
        // ================================================================
        // 1. SEED BUSINESS HOURS
        // ================================================================
        if (!await context.Set<BusinessHours>().AnyAsync())
        {
            var businessHours = new List<BusinessHours>
            {
                new BusinessHours
                {
                    DayOfWeek = 0,
                    DayName = "Sunday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = false
                },
                new BusinessHours
                {
                    DayOfWeek = 1,
                    DayName = "Monday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = true
                },
                new BusinessHours
                {
                    DayOfWeek = 2,
                    DayName = "Tuesday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = true
                },
                new BusinessHours
                {
                    DayOfWeek = 3,
                    DayName = "Wednesday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = true
                },
                new BusinessHours
                {
                    DayOfWeek = 4,
                    DayName = "Thursday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = true
                },
                new BusinessHours
                {
                    DayOfWeek = 5,
                    DayName = "Friday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = true
                },
                new BusinessHours
                {
                    DayOfWeek = 6,
                    DayName = "Saturday",
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(13, 0, 0),
                    IsWorkingDay = true
                }
            };

            await context.Set<BusinessHours>().AddRangeAsync(businessHours);
            await context.SaveChangesAsync();
        }


        // ================================================================
        // 2. ENSURE SECOND TECHNICIAN EXISTS
        // ================================================================
        var techRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Technician");

        var tech2User = await context.Users
            .FirstOrDefaultAsync(u => u.Email == "tech2@fixflow.local");

        if (tech2User == null && techRole != null)
        {
            tech2User = new User
            {
                Email = "tech2@fixflow.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                FirstName = "Kamal",
                LastName = "Perera",
                PhoneNumber = "+94770000005",
                RoleId = techRole.Id
            };

            await context.Users.AddAsync(tech2User);
            await context.SaveChangesAsync();

            var plumbingSkill = await context.Skills
                .FirstOrDefaultAsync(s => s.Category == "Plumbing");

            var tech2 = new Technician
            {
                UserId = tech2User.Id,
                EmployeeId = "TECH-002",
                Specialization = "Plumbing & Drainage",
                IsAvailable = true,
                CurrentLatitude = 6.9148,
                CurrentLongitude = 79.9735
            };

            if (plumbingSkill != null)
            {
                tech2.Skills.Add(plumbingSkill);
            }

            await context.Technicians.AddAsync(tech2);
            await context.SaveChangesAsync();
        }


        // ================================================================
        // 3. SEED WORK ORDERS
        // ================================================================
        if (!await context.Set<WorkOrder>().AnyAsync())
        {
            // ------------------------------------------------------------
            // USERS
            // ------------------------------------------------------------
            var primaryTech = await context.Technicians
                .Include(t => t.User)
                .OrderBy(t => t.Id)
                .FirstAsync();

            var secondTech = await context.Technicians
                .Include(t => t.User)
                .OrderByDescending(t => t.Id)
                .FirstAsync();

            var manager = await context.Users
                .FirstAsync(u => u.Email == "manager@fixflow.local");

            var requester = await context.Users
                .FirstAsync(u => u.Email == "requester@fixflow.local");


            // ------------------------------------------------------------
            // ISSUE CATEGORIES
            // ------------------------------------------------------------
            var plumbingCategory = await context.IssueCategories
                .FirstAsync(c => c.Name == "Plumbing");

            var hvacCategory = await context.IssueCategories
                .FirstAsync(c => c.Name == "HVAC");

            var electricalCategory = await context.IssueCategories
                .FirstAsync(c => c.Name == "Electrical");

            var structuralCategory = await context.IssueCategories
                .FirstAsync(c => c.Name == "Structural");

            var elevatorCategory = await context.IssueCategories
                .FirstAsync(c => c.Name == "Elevator/Lift");


            // ------------------------------------------------------------
            // LOCATIONS
            // ------------------------------------------------------------
            // Apartments are locations themselves.
            // We use exact location names instead of:
            //
            // FirstAsync(l => l.Building == "Tower A")
            //
            // because Tower A contains multiple apartments and a lobby.

            var towerAUnit305 = await context.Locations
                .FirstAsync(l => l.Name == "Tower A - Unit 305");

            var towerBUnit204 = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Unit 204");

            var towerBUnit202 = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Unit 202");

            var towerALobby = await context.Locations
                .FirstAsync(l => l.Name == "Tower A - Main Lobby");

            var towerBLobby = await context.Locations
                .FirstAsync(l => l.Name == "Tower B - Main Lobby");

            var basementParking = await context.Locations
                .FirstAsync(l => l.Name == "Common Area - Basement Parking");


            // ------------------------------------------------------------
            // ASSETS
            // ------------------------------------------------------------
            // Asset names are generic.
            // LocationId tells us where the asset is installed.

            var towerBLobbyAirConditioner = await context.Assets
                .FirstAsync(a => a.AssetCode == "HVAC-001");

            var towerALobbyElevator = await context.Assets
                .FirstAsync(a => a.AssetCode == "ELEV-001");

            var towerBUnit204WaterHeater = await context.Assets
                .FirstAsync(a => a.AssetCode == "PLB-WH-001");

            var basementGateMotor = await context.Assets
                .FirstAsync(a => a.AssetCode == "GATE-001");


            // ============================================================
            // 4. CREATE ADDITIONAL MAINTENANCE REQUESTS
            // ============================================================

            // ------------------------------------------------------------
            // REQ-2026-0002
            // Tower A - Unit 305
            // Plumbing
            // ------------------------------------------------------------
            var reqCeiling = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0002",

                Title = "Ceiling Leak in Unit 305",

                Description =
                    "Water is dripping from the bathroom ceiling. " +
                    "An inspection is required to identify and repair the leaking pipe.",

                Status = RequestStatus.Matched,

                LocationId = towerAUnit305.Id,

                RequesterId = requester.Id,

                CategoryId = plumbingCategory.Id
            };


            // ------------------------------------------------------------
            // REQ-2026-0003
            // Tower B Main Lobby
            // HVAC + Air Conditioner
            // ------------------------------------------------------------
            var reqHvac = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0003",

                Title = "Tower B Lobby Air Conditioner Malfunction",

                Description =
                    "The air conditioner in the Tower B main lobby " +
                    "is blowing warm air and making an unusual humming noise.",

                Status = RequestStatus.Scheduled,

                LocationId = towerBLobby.Id,

                AssetId = towerBLobbyAirConditioner.Id,

                RequesterId = requester.Id,

                CategoryId = hvacCategory.Id
            };


            // ------------------------------------------------------------
            // REQ-2026-0004
            // Tower B - Unit 202
            // Electrical
            // ------------------------------------------------------------
            var reqElec = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0004",

                Title = "Electrical Panel Issue in Unit 202",

                Description =
                    "The electrical breaker is repeatedly tripping in the apartment. " +
                    "The electrical system should be inspected.",

                Status = RequestStatus.InProgress,

                LocationId = towerBUnit202.Id,

                RequesterId = requester.Id,

                CategoryId = electricalCategory.Id
            };


            // ------------------------------------------------------------
            // REQ-2026-0005
            // Common Area - Basement Parking
            // Gate Motor
            // ------------------------------------------------------------
            var reqDoor = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0005",

                Title = "Basement Parking Gate Motor Problem",

                Description =
                    "The basement parking entrance gate is operating slowly " +
                    "and the gate motor requires inspection.",

                Status = RequestStatus.Completed,

                LocationId = basementParking.Id,

                AssetId = basementGateMotor.Id,

                RequesterId = requester.Id,

                CategoryId = structuralCategory.Id
            };


            // ------------------------------------------------------------
            // REQ-2026-0006
            // Tower A Main Lobby
            // Elevator
            // ------------------------------------------------------------
            var reqElev = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0006",

                Title = "Tower A Passenger Elevator Sensor Fault",

                Description =
                    "The passenger elevator door sensor is failing intermittently " +
                    "and requires inspection and calibration.",

                Status = RequestStatus.InReview,

                LocationId = towerALobby.Id,

                AssetId = towerALobbyElevator.Id,

                RequesterId = requester.Id,

                CategoryId = elevatorCategory.Id
            };


            // ------------------------------------------------------------
            // REQ-2026-0007
            // Tower B - Unit 204
            // Water Heater
            // ------------------------------------------------------------
            var reqWaterHeater = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0007",

                Title = "Water Heater Inspection in Unit 204",

                Description =
                    "The water heater in Unit 204 requires preventive inspection " +
                    "and connection checks.",

                Status = RequestStatus.Submitted,

                LocationId = towerBUnit204.Id,

                AssetId = towerBUnit204WaterHeater.Id,

                RequesterId = requester.Id,

                CategoryId = plumbingCategory.Id
            };


            await context.MaintenanceRequests.AddRangeAsync(
                reqCeiling,
                reqHvac,
                reqElec,
                reqDoor,
                reqElev,
                reqWaterHeater
            );

            await context.SaveChangesAsync();


            // ============================================================
            // 5. DATE/TIME
            // ============================================================
            var today = DateTime.UtcNow.Date;


            // ============================================================
            // 6. WORK ORDER 1
            // PENDING MANAGER APPROVAL
            // ============================================================
            var wo1 = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0001",

                Title = "Ceiling Leak Inspection & Pipe Sealing",

                Description =
                    "Inspect the bathroom ceiling pipe leak in Unit 305 " +
                    "and apply the required waterproof seal.",

                RequestId = reqCeiling.Id,

                TechnicianId = primaryTech.Id,

                LocationId = towerAUnit305.Id,

                Priority = WorkOrderPriority.Critical,

                Status = WorkOrderStatus.PendingManagerApproval,

                ScheduledStartTime = today.AddHours(14),

                ScheduledEndTime = today.AddHours(16),

                EstimatedDurationMinutes = 120,

                SLADeadline = today.AddHours(18),

                ConflictDetected = false,

                AiDecisionSummary =
                    "Selected an available technician slot within business hours " +
                    "and before the SLA deadline. Existing bookings were checked " +
                    "and no overlapping booking was detected."
            };


            // ============================================================
            // 7. WORK ORDER 2
            // SCHEDULED
            // ============================================================
            var wo2 = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0002",

                Title = "Tower B Lobby Air Conditioner Maintenance",

                Description =
                    "Inspect the lobby air conditioner, clean the condenser coils " +
                    "and check refrigerant pressure.",

                RequestId = reqHvac.Id,

                TechnicianId = primaryTech.Id,

                LocationId = towerBLobby.Id,

                Priority = WorkOrderPriority.High,

                Status = WorkOrderStatus.Scheduled,

                ScheduledStartTime = today.AddHours(9),

                ScheduledEndTime = today.AddHours(11),

                EstimatedDurationMinutes = 120,

                SLADeadline = today.AddDays(1),

                ApprovedById = manager.Id,

                ApprovedAt = DateTime.UtcNow.AddHours(-3),

                ApprovalComments =
                    "Approved for morning service block."
            };


            // ============================================================
            // 8. WORK ORDER 3
            // IN PROGRESS
            // ============================================================
            var wo3 = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0003",

                Title = "Unit 202 Electrical Panel Inspection",

                Description =
                    "Inspect the electrical panel and identify the cause " +
                    "of the repeated breaker trips.",

                RequestId = reqElec.Id,

                TechnicianId = primaryTech.Id,

                LocationId = towerBUnit202.Id,

                Priority = WorkOrderPriority.Medium,

                Status = WorkOrderStatus.InProgress,

                ScheduledStartTime =
                    today.AddHours(11).AddMinutes(30),

                ScheduledEndTime =
                    today.AddHours(12).AddMinutes(30),

                ActualStartTime =
                    today.AddHours(11).AddMinutes(32),

                EstimatedDurationMinutes = 60,

                SLADeadline = today.AddDays(2),

                ApprovedById = manager.Id,

                ApprovedAt = DateTime.UtcNow.AddDays(-1)
            };


            // ============================================================
            // 9. WORK ORDER 4
            // COMPLETED
            // ============================================================
            var wo4 = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0004",

                Title = "Basement Parking Gate Motor Repair",

                Description =
                    "Inspect the parking gate motor, lubricate the mechanism " +
                    "and verify normal gate operation.",

                RequestId = reqDoor.Id,

                TechnicianId = secondTech.Id,

                LocationId = basementParking.Id,

                Priority = WorkOrderPriority.Low,

                Status = WorkOrderStatus.Completed,

                ScheduledStartTime =
                    today.AddDays(-1).AddHours(10),

                ScheduledEndTime =
                    today.AddDays(-1).AddHours(11),

                ActualStartTime =
                    today.AddDays(-1).AddHours(10).AddMinutes(5),

                ActualEndTime =
                    today.AddDays(-1).AddHours(10).AddMinutes(55),

                EstimatedDurationMinutes = 50,

                SLADeadline = today.AddDays(1),

                ApprovedById = manager.Id,

                ApprovedAt = DateTime.UtcNow.AddDays(-2)
            };


            // ============================================================
            // 10. WORK ORDER 5
            // CONFLICT SCENARIO
            // ============================================================
            var woConflict = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0005",

                Title = "Emergency Elevator Sensor Calibration",

                Description =
                    "Recalibrate the passenger elevator optical door " +
                    "and leveling sensors.",

                RequestId = reqElev.Id,

                TechnicianId = primaryTech.Id,

                LocationId = towerALobby.Id,

                Priority = WorkOrderPriority.Critical,

                Status = WorkOrderStatus.PendingManagerApproval,

                // WO-0002 = 09:00 - 11:00
                // WO-0005 = 10:00 - 12:00
                // Therefore they overlap.
                ScheduledStartTime = today.AddHours(10),

                ScheduledEndTime = today.AddHours(12),

                EstimatedDurationMinutes = 120,

                SLADeadline = today.AddHours(15),

                ConflictDetected = true,

                ConflictDetailsJson =
                    JsonSerializer.Serialize(
                        new List<ConflictDetailDto>
                        {
                            new ConflictDetailDto
                            {
                                ExistingTitle =
                                    "Tower B Lobby Air Conditioner Maintenance",

                                ConflictingStart =
                                    today.AddHours(9),

                                ConflictingEnd =
                                    today.AddHours(11),

                                Reason =
                                    "Direct schedule conflict with existing " +
                                    "booking (09:00 - 11:00)"
                            }
                        }
                    ),

                AiDecisionSummary =
                    "Schedule conflict detected with existing booking " +
                    "WO-202610-0002 (09:00 - 11:00). " +
                    "Alternative slot 13:00 - 15:00 suggested for Manager review."
            };


            // ============================================================
            // 11. WORK ORDER 6
            // DRAFT
            // ============================================================
            var woDraft = new WorkOrder
            {
                WorkOrderNumber = "WO-202610-0006",

                Title = "Preventive Maintenance: Water Heater",

                Description =
                    "Inspect the water heater, check connections and " +
                    "verify normal heating operation.",

                RequestId = reqWaterHeater.Id,

                TechnicianId = secondTech.Id,

                LocationId = towerBUnit204.Id,

                Priority = WorkOrderPriority.Medium,

                Status = WorkOrderStatus.Draft,

                EstimatedDurationMinutes = 90
            };


            // ============================================================
            // 12. SAVE WORK ORDERS
            // ============================================================
            await context.Set<WorkOrder>().AddRangeAsync(
                wo1,
                wo2,
                wo3,
                wo4,
                woConflict,
                woDraft
            );

            await context.SaveChangesAsync();


            // ============================================================
            // 13. COMPLETION EVIDENCE
            // ============================================================
            var evidence = new CompletionEvidence
            {
                WorkOrderId = wo4.Id,

                UploadedById = secondTech.UserId,

                SignerName = "Resident J. Silva",

                SignatureDataUrl =
                    "data:image/svg+xml;utf8," +
                    "<svg xmlns='http://www.w3.org/2000/svg' " +
                    "width='200' height='60'>" +
                    "<path d='M10 40 Q 50 10 90 40 T 170 30' " +
                    "stroke='black' fill='none' stroke-width='2'/>" +
                    "</svg>",

                Caption =
                    "Basement parking gate motor repaired and " +
                    "operation verified.",

                UploadedAt =
                    today.AddDays(-1).AddHours(11)
            };

            await context.Set<CompletionEvidence>()
                .AddAsync(evidence);


            // ============================================================
            // 14. WORK NOTE
            // ============================================================
            var note = new WorkNote
            {
                WorkOrderId = wo3.Id,

                AuthorId = primaryTech.UserId,

                NoteText =
                    "Inspected the electrical panel and found a loose " +
                    "connection causing repeated breaker trips.",

                Timestamp =
                    today.AddHours(12)
            };

            await context.Set<WorkNote>()
                .AddAsync(note);


            // ============================================================
            // 15. FINAL SAVE
            // ============================================================
            await context.SaveChangesAsync();
        }
    }
}


