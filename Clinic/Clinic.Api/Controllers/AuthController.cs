using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "Healthy" });
        }
        
// Register User
        [HttpPost("register")]
        public  async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterUserAsync(request);
            if (!result.Success)
            {
                return BadRequest(result.Errors);
            }
            return Ok(result.Success);
        }
        
// Login User
        [HttpPost("login")]
        public async  Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginUserAsync(request);
            if (!result.Success)
            {
                return Unauthorized(result.Errors);
            }
            return Ok(result.Success);
        }
       

    }
}
