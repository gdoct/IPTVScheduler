namespace ipvcr.Web.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using ipvcr.Auth;
using ipvcr.Scheduling.Shared.Settings;
using ipvcr.Web.Models;
using Microsoft.AspNetCore.Authorization;

[Route("api/login")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class LoginController : ControllerBase
{
    private readonly ITokenManager _tokenManager;
    private readonly ISettingsService _settingsService;

    public LoginController(ITokenManager tokenManager, ISettingsService settingsService)
    {
        _tokenManager = tokenManager;
        _settingsService = settingsService;
    }

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == _settingsService.AdminPasswordSettings.AdminUsername
         &&_settingsService.ValidateAdminPassword(request.Password))
        {   
            var token = _tokenManager.CreateToken(request.Username);
            return Ok(new { Token = token });
        }
        return Unauthorized();
    }

    [Authorize]
    [HttpPost]
    [Route("changepassword")]
    public IActionResult UpdatePassword([FromBody] LoginRequest request)
    {
        if (request.Username == _settingsService.AdminPasswordSettings.AdminUsername)
        {
            _settingsService.UpdateAdminPassword(request.Password);
            return Ok(new { Message = "Password updated successfully." });
        }
        return Unauthorized();
    }

    [Authorize]
    [HttpPost]
    [Route("restart")]
    public IActionResult Restart()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }
        if (_settingsService.AdminPasswordSettings.AdminUsername == username)
        {
            // restart the asp.net server
            Task.Run(Program.RestartAspNetAsync);
            return Ok(new { Message = "Restarting..." });
        }
        return Unauthorized();
    }
}