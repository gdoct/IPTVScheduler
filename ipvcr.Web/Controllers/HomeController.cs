using ipvcr.Scheduling;
using ipvcr.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ipvcr.Web.Controllers;

public class HomeController(ILogger<HomeController> logger, IRecordingSchedulingContext context, ISettingsManager settingsManager, IPlaylistManager playlistManager) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly IRecordingSchedulingContext _context = context;
    private readonly ISettingsManager _settingsManager = settingsManager;
    private readonly IPlaylistManager _playlistManager = playlistManager;

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Recordings));
    }

    public IActionResult Recordings()
    {
        var model = new HomeRecordingsViewModel
        {
            RecordingPath = _settingsManager.Settings.OutputPath,
            Recordings = _context.Recordings.ToList(),
            Channels = _playlistManager.GetPlaylistItems()
        };
        return View(model);
    }

    public IActionResult Settings()
    {
        return View(_settingsManager.Settings);
    }

    [HttpPost]
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
    public IActionResult Create(ScheduledRecording recording)
    {
        if (ModelState.IsValid)
        {
            if (_context.Recordings.Any(r => r.Id == recording.Id))
            {
                _logger.LogDebug("Recording {recordingId} already exists, removing it first.", recording.Id);
                _context.RemoveRecording(recording.Id);
            }
            _context.AddRecording(recording);
            return RedirectToAction(nameof(Recordings));
        }
        return View("Recordings", _context.Recordings.ToList());
    }

    [HttpGet]
    [Route("Home/Edit/{recordingId}")]
    public IActionResult Edit(Guid recordingId)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == recordingId);
        if (recording == null)
        {
            return NotFound();
        }
        return Json(recording);
    }

    [HttpDelete]
    [Route("Home/Delete/{recordingId}")]
    public IActionResult Delete(Guid recordingId)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == recordingId);
        if (recording == null)
        {
            return NotFound();
        }
        _context.RemoveRecording(recording.Id);
        return RedirectToAction(nameof(Recordings));
    }

    [HttpPost]
    public IActionResult UploadM3u(IFormFile m3uFile)
    {
        if (m3uFile == null || m3uFile.Length == 0)
        {
            ModelState.AddModelError("File", "Please upload a valid M3U file.");
            return RedirectToAction(nameof(Recordings));
        }

        var uploadPath = "/var/lib/iptvscheduler";
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
            return RedirectToAction(nameof(Recordings));
        }

        return RedirectToAction(nameof(Recordings));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
