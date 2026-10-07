using FixFlow.Api.Data; // DB Context namespace
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;
    private readonly FixFlowDbContext _context; // Add DB Context

    public TechniciansController(ITechnicianService technicianService, FixFlowDbContext context)
    {
        _technicianService = technicianService;
        _context = context;
    }

    // 0. GET: api/technicians/my-jobs
    [HttpGet("my-jobs")]
    public async Task<IActionResult> GetMyJobs([FromQuery] string? email)
    {
        try
        {
            Console.WriteLine($"--- DEBUG: Received Email Query from Frontend: '{email}'");

            if (string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("--- DEBUG: Email is empty or null!");
                return Ok(new List<object>());
            }

            var technician = await _context.Technicians
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.User != null && t.User.Email.ToLower() == email.Trim().ToLower());

            if (technician == null)
            {
                Console.WriteLine($"--- DEBUG: No technician found in database for email: '{email}'");
                return Ok(new List<object>());
            }

            Console.WriteLine($"--- DEBUG: Found Technician ID: {technician.Id} for email: {email}");

            var assignments = await _context.Set<Assignment>()
                .Where(a => a.TechnicianId == technician.Id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            Console.WriteLine($"--- DEBUG: Total jobs found for this technician: {assignments.Count}");

            var jobs = assignments.Select(a => new 
            {
                assignmentId = a.Id,
                requestId = a.MaintenanceRequestId,
                requiredSkill = a.ReasoningSummary ?? "General Maintenance",
                priorityLevel = "Medium",
                status = a.Status ?? "Assigned",
                assignedAt = a.AssignedAt
            }).ToList();

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching jobs: {ex.Message}");
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

    // 1. GET: api/technicians
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? skill)
    {
        var result = await _technicianService.GetAllTechniciansAsync(skill);
        return Ok(result);
    }

    // 2. GET: api/technicians/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tech = await _technicianService.GetTechnicianByIdAsync(id);
        if (tech == null) return NotFound();
        return Ok(tech);
    }

    // 3. POST: api/technicians
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTechnicianDto dto)
    {
        var created = await _technicianService.CreateTechnicianAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // 4. POST: api/technicians/assignment-recommendation
    [HttpPost("assignment-recommendation")]
    public async Task<IActionResult> GetRecommendation([FromBody] AssignmentRequestDto dto)
    {
        try
        {
            var result = await _technicianService.RecommendTechnicianAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // 5. POST: api/technicians/assign
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignmentRequestDto dto)
    {
        if (dto.RequestId == Guid.Empty || dto.TechnicianId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid requestId and technicianId (GUID) are required." });
        }

        try
        {
            var success = await _technicianService.AssignTechnicianAsync(dto.RequestId, dto.TechnicianId);
            if (!success)
            {
                return BadRequest(new { message = "Assignment could not be completed." });
            }

            return Ok(new { message = "Technician assigned successfully!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // 6. PUT: api/technicians/update-status
    [HttpPut("update-status")]
    public async Task<IActionResult> UpdateJobStatus([FromBody] UpdateJobStatusDto dto)
    {
        try
        {
            Console.WriteLine($"--- DEBUG: Updating job status. JobId: {dto.JobId}, Status: {dto.Status}");

            // Retrieving the assignment from the database (using assignmentId)
            var assignment = await _context.Set<Assignment>()
                .FirstOrDefaultAsync(a => a.Id == dto.JobId);

            if (assignment == null)
            {
                return NotFound(new { message = $"Assignment with ID {dto.JobId} was not found." });
            }

            // Updating the status
            assignment.Status = dto.Status;
            
            // In the case of a rejection, the reason can also be saved (under 'ReasoningSummary' or in another column).
            if (!string.IsNullOrEmpty(dto.RejectionReason))
            {
                assignment.ReasoningSummary = $"Rejected Reason: {dto.RejectionReason}";
            }

            // Saving permanently to the database
            await _context.SaveChangesAsync();

            Console.WriteLine($"--- DEBUG: Job status successfully updated to {dto.Status} in database.");
            return Ok(new { message = "Job status updated successfully!" });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            Console.WriteLine($"Status Update Error: {innerMessage}");
            return StatusCode(500, new { message = $"Internal server error: {innerMessage}" });
        }
    }
}

// The DTO class can be placed directly under the Controller or in a 'DTOs' folder.
public class UpdateJobStatusDto
{
    public Guid JobId { get; set; } // If the Assignment ID is a GUID
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}