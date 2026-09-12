using FixFlow.Api.Controllers;
using FixFlow.Api.DTOs;
using FixFlow.Api.Services;
using FixFlow.Api.Validators;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FixFlow.Tests;

public class SecurityTests
{
    [Fact]
    public void BCrypt_HashPassword_ShouldVerifyCorrectly()
    {
        var password = "Admin123!SecurePassword";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword", hash));
    }
}

public class ValidationTests
{
    [Theory]
    [InlineData("user@fixflow.local", "Password123", true)]
    [InlineData("invalid-email", "Password123", false)]
    [InlineData("user@fixflow.local", "123", false)]
    public void LoginRequestDtoValidator_ShouldValidateCorrectly(string email, string password, bool expectedValid)
    {
        var validator = new LoginRequestDtoValidator();
        var model = new LoginRequestDto { Email = email, Password = password };

        var result = validator.Validate(model);
        Assert.Equal(expectedValid, result.IsValid);
    }
}

public class HealthControllerTests
{
    [Fact]
    public void GetHealthStatus_ShouldReturnOkWithHealthyStatus()
    {
        var controller = new HealthController();
        var result = controller.GetHealthStatus() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}

public class DistanceMatrixServiceTests
{
    [Fact]
    public void CalculateHaversineDistance_ShouldReturnAccurateKilometers()
    {
        // Distance between SLIIT Malabe (6.9147, 79.9733) and Colombo Fort (6.9344, 79.8428) is ~14.5 km
        var distance = ExternalDistanceMatrixService.CalculateHaversineDistance(6.9147, 79.9733, 6.9344, 79.8428);

        Assert.InRange(distance, 13.0, 16.0);
    }
}
