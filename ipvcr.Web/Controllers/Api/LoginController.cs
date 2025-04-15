namespace ipvcr.Web.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using ipvcr.Auth;
using ipvcr.Scheduling.Shared;
using ipvcr.Web.Models;
using System;
using Microsoft.AspNetCore.Authorization;

[Route("api/login")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class LoginController : ControllerBase
{
    private readonly ITokenManager _tokenManager;
    private readonly ISettingsManager _settingsManager;

    public LoginController(ITokenManager tokenManager, ISettingsManager settingsManager)
    {
        _tokenManager = tokenManager;
        _settingsManager = settingsManager;
    }

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == _settingsManager.Settings.AdminUsername
         && _tokenManager.CreateHash(request.Password) == _settingsManager.GetAdminPasswordHash())
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
        if (request.Username == _settingsManager.Settings.AdminUsername)
        {
            _settingsManager.UpdateAdminPassword(request.Password);
            return Ok(new { Message = "Password updated successfully." });
        }
        return Unauthorized();
    }
}