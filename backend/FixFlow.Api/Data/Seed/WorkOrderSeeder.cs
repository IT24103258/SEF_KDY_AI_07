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
        // 1. Seed Business Hours if empty
        if (!await context.Set<BusinessHours>().AnyAsync())
        {
            var businessHours = new List<BusinessHours>
            {
                new BusinessHours { DayOfWeek = 0, DayName = "Sunday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = false },
                new BusinessHours { DayOfWeek = 1, DayName = "Monday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 2, DayName = "Tuesday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 3, DayName = "Wednesday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 4, DayName = "Thursday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 5, DayName = "Friday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 6, DayName = "Saturday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(13, 0, 0), IsWorkingDay = true }
            };
            await context.Set<BusinessHours>().AddRangeAsync(businessHours);
            await context.SaveChangesAsync();
        }

        // 2. Ensure we have a second technician profile for multi-technician schedule demonstration
        var techRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Technician");
        var tech2User = await context.Users.FirstOrDefaultAsync(u => u.Email == "tech2@fixflow.local");
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

            var plumbingSkill = await context.Skills.FirstOrDefaultAsync(s => s.Category == "Plumbing");
            var tech2 = new Technician
            {
                UserId = tech2User.Id,
                EmployeeId = "TECH-002",
                Specialization = "Plumbing & Drainage",
                IsAvailable = true,
                CurrentLatitude = 6.9148,
                CurrentLongitude = 79.9735
            };
            if (plumbingSkill != null) tech2.Skills.Add(plumbingSkill);
            await context.Technicians.AddAsync(tech2);
            await context.SaveChangesAsync();
        }

        // 3. Seed WorkOrders if empty
        if (!await context.Set<WorkOrder>().AnyAsync())
        {
            var primaryTech = await context.Technicians.Include(t => t.User).FirstAsync();
            var secondTech = await context.Technicians.Include(t => t.User).OrderByDescending(t => t.Id).FirstAsync();
            var manager = await context.Users.FirstAsync(u => u.Email == "manager@fixflow.local");
            var requester = await context.Users.FirstAsync(u => u.Email == "requester@fixflow.local");
            var locationTower = await context.Locations.FirstOrDefaultAsync(l => l.Building == "Tower");
            var locationBlockA = await context.Locations.FirstOrDefaultAsync(l => l.Building == "Block A");
            var locationBlockB = await context.Locations.FirstOrDefaultAsync(l => l.Building == "Block B");
            var locationLibrary = await context.Locations.FirstOrDefaultAsync(l => l.Building == "Library");

            var fallbackLocation = await context.Locations.FirstAsync();

            var locationTowerA = locationTower ?? fallbackLocation;
            var locationTowerB = locationBlockB ?? fallbackLocation;
            var locationCommon = locationLibrary ?? fallbackLocation;
            
            var existingReq = await context.MaintenanceRequests.FirstAsync();

            // Create supplementary maintenance requests for realistic work orders
            var reqCeiling = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0002",
                Title = "Ceiling Leak in Unit 305 Bathroom",
                Description = "Water dripping from ceiling drywall above bathtub. Urgent inspection required.",
                Status = RequestStatus.Matched,
                LocationId = locationTowerA.Id,
                RequesterId = requester.Id
            };
            var reqHvac = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0003",
                Title = "Tower B Lobby Air Conditioner Malfunction",
                Description = "Lobby AC blowing warm air and humming loudly.",
                Status = RequestStatus.Scheduled,
                LocationId = locationTowerB.Id,
                RequesterId = requester.Id
            };
            var reqElec = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0004",
                Title = "Corridor Lighting Failure Floor 2",
                Description = "Three light fixtures down in the corridor near emergency exit.",
                Status = RequestStatus.InProgress,
                LocationId = locationTowerB.Id,
                RequesterId = requester.Id
            };
            var reqDoor = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0005",
                Title = "Broken Basement Access Door Handle",
                Description = "Access door handle stuck and latch mechanism broken.",
                Status = RequestStatus.Completed,
                LocationId = locationCommon.Id,
                RequesterId = requester.Id
            };
            var reqElev = new MaintenanceRequest
            {
                RequestNumber = "REQ-2026-0006",
                Title = "Tower A Passenger Elevator Stoppage",
                Description = "Elevator door sensor failing intermittently at Floor 2.",
                Status = RequestStatus.InReview,
                LocationId = locationTowerA.Id,
                RequesterId = requester.Id
            };

            await context.MaintenanceRequests.AddRangeAsync(reqCeiling, reqHvac, reqElec, reqDoor, reqElev);
            await context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;

            // 1. Pending Manager Approval (AI Generated Proposal)
            var wo1 = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0001",
                Title = "Ceiling Leak Inspection & Pipe Sealing",
                Description = "Inspect bathroom ceiling pipe leak in Unit 305 and apply waterproof seal.",
                RequestId = reqCeiling.Id,
                TechnicianId = primaryTech.Id,
                LocationId = locationTowerA.Id,
                Priority = WorkOrderPriority.Critical,
                Status = WorkOrderStatus.PendingManagerApproval,
                ScheduledStartTime = today.AddHours(14),
                ScheduledEndTime = today.AddHours(16),
                EstimatedDurationMinutes = 120,
                SLADeadline = today.AddHours(18),
                ConflictDetected = false,
                AiDecisionSummary = "Selected an available technician slot within business hours and before the SLA deadline. Existing bookings were checked and no overlapping booking was detected."
            };

            // 2. Scheduled Work Order (Today 09:00 - 11:00)
            var wo2 = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0002",
                Title = "Tower B Lobby Air Conditioner Maintenance",
                Description = "Clean condenser coils and check refrigerant pressure.",
                RequestId = reqHvac.Id,
                TechnicianId = primaryTech.Id,
                LocationId = locationTowerB.Id,
                Priority = WorkOrderPriority.High,
                Status = WorkOrderStatus.Scheduled,
                ScheduledStartTime = today.AddHours(9),
                ScheduledEndTime = today.AddHours(11),
                EstimatedDurationMinutes = 120,
                SLADeadline = today.AddDays(1),
                ApprovedById = manager.Id,
                ApprovedAt = DateTime.UtcNow.AddHours(-3),
                ApprovalComments = "Approved for morning service block."
            };

            // 3. In Progress Work Order (Today 11:30 - 12:30)
            var wo3 = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0003",
                Title = "Corridor Lighting Replacement",
                Description = "Replace LED ballasts and lamps in 2nd floor corridor.",
                RequestId = reqElec.Id,
                TechnicianId = primaryTech.Id,
                LocationId = locationTowerB.Id,
                Priority = WorkOrderPriority.Medium,
                Status = WorkOrderStatus.InProgress,
                ScheduledStartTime = today.AddHours(11).AddMinutes(30),
                ScheduledEndTime = today.AddHours(12).AddMinutes(30),
                ActualStartTime = today.AddHours(11).AddMinutes(32),
                EstimatedDurationMinutes = 60,
                SLADeadline = today.AddDays(2),
                ApprovedById = manager.Id,
                ApprovedAt = DateTime.UtcNow.AddDays(-1)
            };

            // 4. Completed Work Order (Yesterday with customer signature)
            var wo4 = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0004",
                Title = "Basement Security Door Repair",
                Description = "Replaced door latch bolt and lubricated hinges.",
                RequestId = reqDoor.Id,
                TechnicianId = secondTech.Id,
                LocationId = locationCommon.Id,
                Priority = WorkOrderPriority.Low,
                Status = WorkOrderStatus.Completed,
                ScheduledStartTime = today.AddDays(-1).AddHours(10),
                ScheduledEndTime = today.AddDays(-1).AddHours(11),
                ActualStartTime = today.AddDays(-1).AddHours(10).AddMinutes(5),
                ActualEndTime = today.AddDays(-1).AddHours(10).AddMinutes(55),
                EstimatedDurationMinutes = 50,
                SLADeadline = today.AddDays(1),
                ApprovedById = manager.Id,
                ApprovedAt = DateTime.UtcNow.AddDays(-2)
            };

            // 5. Conflict Scenario Demo Work Order (Overlaps with WO2 09:00 - 11:00 on primaryTech)
            var woConflict = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0005",
                Title = "Emergency Elevator Sensor Calibration",
                Description = "Recalibrate optical leveling sensor on Tower A elevator.",
                RequestId = reqElev.Id,
                TechnicianId = primaryTech.Id,
                LocationId = locationTowerA.Id,
                Priority = WorkOrderPriority.Critical,
                Status = WorkOrderStatus.PendingManagerApproval,
                ScheduledStartTime = today.AddHours(10), // Overlaps 10:00 - 12:00 with WO2 09:00 - 11:00
                ScheduledEndTime = today.AddHours(12),
                EstimatedDurationMinutes = 120,
                SLADeadline = today.AddHours(15),
                ConflictDetected = true,
                ConflictDetailsJson = JsonSerializer.Serialize(new List<ConflictDetailDto>
                {
                    new ConflictDetailDto
                    {
                        ExistingTitle = "Tower B Lobby Air Conditioner Maintenance",
                        ConflictingStart = today.AddHours(9),
                        ConflictingEnd = today.AddHours(11),
                        Reason = "Direct schedule conflict with existing booking (09:00 - 11:00)"
                    }
                }),
                AiDecisionSummary = "Schedule conflict detected with existing booking WO-202609-0002 (09:00 - 11:00). Alternative slot 13:00 - 15:00 suggested for Manager review."
            };

            // 6. Draft Work Order
            var woDraft = new WorkOrder
            {
                WorkOrderNumber = "WO-202609-0006",
                Title = "Preventive Maintenance: Booster Pump Valves",
                Description = "Inspect pressure release valves and check for micro leaks.",
                RequestId = existingReq.Id,
                TechnicianId = secondTech.Id,
                LocationId = locationCommon.Id,
                Priority = WorkOrderPriority.Medium,
                Status = WorkOrderStatus.Draft,
                EstimatedDurationMinutes = 90
            };

            await context.Set<WorkOrder>().AddRangeAsync(wo1, wo2, wo3, wo4, woConflict, woDraft);
            await context.SaveChangesAsync();

            // Seed Notes & Evidence for Completed WO4
            var evidence = new CompletionEvidence
            {
                WorkOrderId = wo4.Id,
                UploadedById = secondTech.UserId,
                SignerName = "Resident J. Silva",
                SignatureDataUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='200' height='60'><path d='M10 40 Q 50 10 90 40 T 170 30' stroke='black' fill='none' stroke-width='2'/></svg>",
                Caption = "Basement door latch replaced and verified with building security.",
                UploadedAt = today.AddDays(-1).AddHours(11)
            };
            await context.Set<CompletionEvidence>().AddAsync(evidence);

            var note = new WorkNote
            {
                WorkOrderId = wo3.Id,
                AuthorId = primaryTech.UserId,
                NoteText = "Found faulty ballast on unit #2. Replaced with spare from inventory.",
                Timestamp = today.AddHours(12)
            };
            await context.Set<WorkNote>().AddAsync(note);

            await context.SaveChangesAsync();
        }
    }
}
