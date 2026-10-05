using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    [HttpGet("")]
    public IActionResult GetRoot()
    {
        return Ok(new 
        { 
            message = "Welcome to FixFlow AI API Gateway!", 
            status = "Online", 
            documentation = "/swagger" 
        });
    }
}