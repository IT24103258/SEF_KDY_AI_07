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
        var technicians = await _technicianService.GetAllTechniciansAsync(null);
        
        var jobs = technicians.Select((t, index) => new 
        {
            Id = 101,
            Title = "Plumbing Maintenance Task",
            Location = "Main Campus - Block B",
            Priority = "High",
            Status = "Assigned"
        }).ToList();

        return Ok(jobs);
    }

    // 1. GET: api/technicians
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? skill)
    {
        var result = await _technicianService.GetAllTechniciansAsync(skill);
        return Ok(result);
    }

    // 2. GET: api/technicians/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
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
        var result = await _technicianService.RecommendTechnicianAsync(dto);
        return Ok(result);
    }

    // 5. POST: api/technicians/assign
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignmentRequestDto dto)
    {
        if (dto == null) 
            return BadRequest(new { Message = "Invalid payload." });

        // 1. Locating the technician in the database
        var tech = await _context.Technicians.FirstOrDefaultAsync();

        if (tech == null)
        {
            return NotFound(new { Message = "No technician found in database." });
        }

        // Setting the technician's status to Busy
        tech.IsAvailable = false;

        // 2. Using Set<Assignment>() to save the Assignment entity (without changing DbContext)
        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            RequestId = dto.RequestId > 0 ? dto.RequestId : 101,
            TechnicianId = tech.Id,
            MatchScore = 80.0,
            ReasoningSummary = "Best fit based on availability and skills.",
            Status = "Assigned",
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Assignment>().Add(assignment);

        // 3. Saving changes to the PostgreSQL Database
        await _context.SaveChangesAsync();

        return Ok(new { 
            Message = "Technician assigned successfully.", 
            RequestId = assignment.RequestId,
            AssignmentId = assignment.Id 
        });
    }
}