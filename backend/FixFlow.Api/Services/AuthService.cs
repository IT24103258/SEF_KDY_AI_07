using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FixFlow.Api.Services;

public class AuthService : IAuthService
{
    private readonly FixFlowDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(FixFlowDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:SecretKey"] ?? "FixFlow_Super_Secret_JWT_Signing_Key_SLIIT_2026_SE3090_Minimum_256_Bits!";
        var key = Encoding.UTF8.GetBytes(secretKey);

        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "480"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Requester")
            }),
            Expires = expiresAt,
            Issuer = _configuration["Jwt:Issuer"] ?? "FixFlow.Api",
            Audience = _configuration["Jwt:Audience"] ?? "FixFlow.Clients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new LoginResponseDto
        {
            Token = tokenHandler.WriteToken(token),
            ExpiresAt = expiresAt,
            User = MapUserToDto(user)
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existingUser)
        {
            throw new ConflictException("User with this email already exists.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower())
                   ?? await _context.Roles.FirstAsync(r => r.Name == "Requester");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RoleId = role.Id
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Role = role;
        return MapUserToDto(user);
    }

    public async Task<UserDto> GetCurrentUserAsync(string userEmail)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        return MapUserToDto(user);
    }

    private static UserDto MapUserToDto(User user)
    {
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
}
