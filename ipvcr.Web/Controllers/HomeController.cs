using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ipvcr.Web.Models;
using ipvcr.Scheduling;

namespace ipvcr.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IRecordingSchedulingContext _context;

    public HomeController(ILogger<HomeController> logger, IRecordingSchedulingContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Recordings()
    {
        var model = new HomeRecordingsViewModel
        {
            RecordingPath = SettingsManager.Instance.Settings.OutputPath,
            Recordings = _context.Recordings.ToList()
        };
        return View(model);
    }

    public IActionResult Settings()
    {
        return View(SettingsManager.Instance.Settings);
    }

    [HttpPost]
    public IActionResult UpdateSettings(SchedulerSettings settings)
    {
        if (ModelState.IsValid)
        {
            SettingsManager.Instance.Settings = settings;
            return RedirectToAction(nameof(Settings));
        }
        return View(settings);
    }

    [HttpPost]
    public IActionResult Create(ScheduledRecording recording)
    {
        if (ModelState.IsValid)
        {
            _context.AddRecording(recording);
            return RedirectToAction(nameof(Recordings));
        }
        return View("Recordings", _context.Recordings.ToList());
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
