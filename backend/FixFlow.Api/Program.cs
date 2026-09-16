using System.Text;
using FixFlow.Api.Data;
using FixFlow.Api.Data.Seed;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Middleware;
using FixFlow.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SHARED FOUNDATION SERVICES REGISTRATION
// ============================================================

// DbContext Registration (PostgreSQL / In-Memory Fallback for test execution)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Port=5432;Database=fixflow_db;Username=postgres;Password=12345;";

builder.Services.AddDbContext<FixFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

// Controllers & JSON Formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// FluentValidation Registration
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CORS Policy Registration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Authentication & JWT Security Registration
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "FixFlow_Super_Secret_JWT_Signing_Key_SLIIT_2026_SE3090_Minimum_256_Bits!";
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FixFlow.Api",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FixFlow.Clients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// HttpClient & External Distance Matrix Service Registration
builder.Services.AddHttpClient<IDistanceMatrixService, ExternalDistanceMatrixService>();
builder.Services.AddHttpClient<ITechnicianService, TechnicianService>();

// Shared Infrastructure Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAgentWorkflowService, AgentWorkflowService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();

// Swagger / OpenAPI Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FixFlow AI API Gateway", Version = "v1", Description = "Public Backend API for SE3090 FixFlow AI Platform" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// MEMBER 1 — REQUEST INTAKE & CLASSIFICATION
// ADD YOUR DI REGISTRATIONS ONLY IN THIS SECTION
// ============================================================
// Example: builder.Services.AddScoped<IIssueClassificationService, IssueClassificationService>();


// ============================================================
// MEMBER 2 — RISK & PRIORITY ASSESSMENT
// ADD YOUR DI REGISTRATIONS ONLY IN THIS SECTION
// ============================================================
// Example: builder.Services.AddScoped<IPriorityAssessmentService, PriorityAssessmentService>();


// ============================================================
// MEMBER 3 — TECHNICIAN MATCHING & ASSIGNMENT
// ADD YOUR DI REGISTRATIONS ONLY IN THIS SECTION
// ============================================================
// Example: builder.Services.AddScoped<ITechnicianMatchingService, TechnicianMatchingService>();


// ============================================================
// MEMBER 4 — SCHEDULING & WORK ORDER MANAGEMENT
// ADD YOUR DI REGISTRATIONS ONLY IN THIS SECTION
// ============================================================
// Example: builder.Services.AddScoped<ISchedulingService, SchedulingService>();


// ============================================================
// APPLICATION PIPELINE CONFIGURATION
// ============================================================
var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FixFlow AI Gateway v1"));
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database Automatic Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<FixFlowDbContext>();
        await DatabaseSeeder.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Required for Integration Testing with WebApplicationFactory
public partial class Program { }
