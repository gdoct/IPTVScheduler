using ipvcr.Scheduling;
using ipvcr.Scheduling.Shared;
using ipvcr.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ipvcr.Web.Controllers;

[Route(ControllerRoutes.RecordingsController)]
public class RecordingsController(ILogger<RecordingsController> logger, IRecordingSchedulingContext context, ISettingsManager settingsManager, IPlaylistManager playlistManager) : Controller
{
    private readonly ILogger<RecordingsController> _logger = logger;
private readonly IRecordingSchedulingContext _context = context;
private readonly ISettingsManager _settingsManager = settingsManager;
private readonly IPlaylistManager _playlistManager = playlistManager;

[Route(ActionRoutes.Index)]
[HttpGet]
public IActionResult Index()
{
    var model = new HomeRecordingsViewModel
    {
        RecordingPath = _settingsManager.Settings.MediaPath,
        Recordings = _context.Recordings.ToList(),
        Channels = _playlistManager.GetPlaylistItems()
    };
    return View(model);
}

[HttpGet]
[Route(ActionRoutes.Settings)]
public IActionResult Settings()
{
    return View(_settingsManager.Settings);
}

[HttpPost]
[Route(ActionRoutes.UpdateSettings)]
public IActionResult UpdateSettings(SchedulerSettings settings)
{
    if (ModelState.IsValid)
    {
        _settingsManager.Settings = settings;
        return RedirectToAction(nameof(Settings));
    }
    return View(settings);
}

[HttpPost]
[Route(ActionRoutes.Create)]
public IActionResult Create([FromBody] ScheduledRecording recording)
{
    if (ModelState.IsValid)
    {
        if (_context.Recordings.Any(r => r.Id == recording.Id))
        {
            _logger.LogDebug("Recording {recordingId} already exists, removing it first.", recording.Id);
            _context.RemoveRecording(recording.Id);
        }
        _context.AddRecording(recording);
        return RedirectToAction(nameof(Index));
    }
    var model = new HomeRecordingsViewModel
    {
        RecordingPath = _settingsManager.Settings.MediaPath,
        Recordings = _context.Recordings.ToList(),
        Channels = _playlistManager.GetPlaylistItems()
    };
    return View(ActionRoutes.Index, model);
}

[HttpGet]
[Route(ActionRoutes.Id)]
public IActionResult Read(Guid id)
{
    var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
    if (recording == null)
    {
        return NotFound();
    }
    return Json(recording);
}

[HttpPost]
[Route(ActionRoutes.Update)]
public IActionResult Update([FromBody] ScheduledRecording recording)
{
    if (ModelState.IsValid)
    {
        var existing = _context.Recordings.FirstOrDefault(r => r.Id == recording.Id);
        if (existing == null)
        {
            return NotFound();
        }
        _context.RemoveRecording(existing.Id);
        _context.AddRecording(recording);

        return Ok();
    }
    return BadRequest(ModelState);
}

[HttpPost]
[Route(ActionRoutes.Delete + "/" + ActionRoutes.Id)]
public IActionResult Delete(Guid id)
{
    var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
    if (recording == null)
    {
        return NotFound();
    }
    _context.RemoveRecording(recording.Id);
    return RedirectToAction(ActionRoutes.Index);
}

[HttpPost]
[Route(ActionRoutes.UploadM3u)]
public IActionResult UploadM3u(IFormFile m3uFile)
{
    if (m3uFile == null || m3uFile.Length == 0)
    {
        ModelState.AddModelError("File", "Please upload a valid M3U file.");
        return RedirectToAction(nameof(Index));
    }

    var uploadPath = "/data";
    var filePath = Path.Combine(uploadPath, m3uFile.FileName);

    try
    {
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            m3uFile.CopyTo(stream);
        }

        var settings = _settingsManager.Settings;
        settings.M3uPlaylistPath = filePath;
        _settingsManager.Settings = settings;

        _logger.LogDebug("M3U file uploaded successfully to {filePath}", filePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading M3U file.");
        ModelState.AddModelError("File", "An error occurred while uploading the file.");
        return RedirectToAction(nameof(Index));
    }

    return RedirectToAction(nameof(Index));
}
}
