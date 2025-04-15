using ipvcr.Scheduling;
using ipvcr.Scheduling.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ipvcr.Web.Controllers;

[Authorize]
[Route("api/recordings")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class RecordingApiController : ControllerBase
{
    private readonly ILogger<RecordingApiController> _logger;
    private readonly IRecordingSchedulingContext _context;
    private readonly ISettingsManager _settingsManager;
    private readonly IPlaylistManager _playlistManager;

    public RecordingApiController(
        ILogger<RecordingApiController> logger,
        IRecordingSchedulingContext context,
        ISettingsManager settingsManager,
        IPlaylistManager playlistManager)
    {
        _logger = logger;
        _context = context;
        _settingsManager = settingsManager;
        _playlistManager = playlistManager;
    }

    // GET: api/recordings
    [HttpGet]
    public ActionResult<IEnumerable<ScheduledRecording>> GetAll()
    {
        return Ok(_context.Recordings.ToList());
    }

    // GET: api/recordings/{id}
    [HttpGet("{id}")]
    public ActionResult<ScheduledRecording> Get(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }
        return Ok(recording);
    }

    // POST: api/recordings
    [HttpPost]
    public ActionResult<ScheduledRecording> Create([FromBody] ScheduledRecording recording)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (recording.Id == Guid.Empty)
        {
            recording.Id = Guid.NewGuid();
        }
        if (_context.Recordings.Any(r => r.Id == recording.Id))
        {
            _logger.LogDebug("Recording {recordingId} already exists, removing it first.", recording.Id);
            _context.RemoveRecording(recording.Id);
        }

        _context.AddRecording(recording);
        return CreatedAtAction(nameof(Get), new { id = recording.Id }, recording);
    }

    // PUT: api/recordings/{id}
    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] ScheduledRecording recording)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("ID cannot be empty");
        }
        if (id != recording.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.RemoveRecording(existing.Id);
        _context.AddRecording(recording);

        return NoContent();
    }

    // DELETE: api/recordings/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }

        _context.RemoveRecording(recording.Id);
        return NoContent();
    }

    // GET: api/recordings/task/{id}
    [HttpGet("task/{id}")]
    public ActionResult<TaskDefinitionModel> GetTask(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }

        var taskDefinition = _context.GetTaskDefinition(recording.Id);
        
        return Ok(new TaskDefinitionModel
        {
            Id = recording.Id,
            Name = recording.Name,
            Content = taskDefinition
        });
    }

    // PUT: api/recordings/task/{id}
    [HttpPut("task/{id}")]
    public IActionResult UpdateTask(Guid id, [FromBody] TaskEditModel model)
    {
        if (id != model.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (string.IsNullOrEmpty(model.TaskFile))
        {
            ModelState.AddModelError("TaskFile", "Task file content cannot be empty.");
            return BadRequest(ModelState);
        }
        
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == model.Id);
        if (recording == null)
        {
            return NotFound();
        }
        
        _context.UpdateTaskDefinition(recording.Id, model.TaskFile);
        return NoContent();
    }

    // GET: api/recordings/settings
    [HttpGet("settings")]
    public ActionResult<SchedulerSettings> GetSettings()
    {
        return Ok(_settingsManager.Settings);
    }

    // PUT: api/recordings/settings
    [HttpPut("settings")]
    public IActionResult UpdateSettings([FromBody] SchedulerSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settingsManager.Settings = settings;
        return NoContent();
    }

    // GET: api/recordings/channels
    [HttpGet()]
    [Route("channelcount")]
    public JsonResult ChannelCount()
    {
        return new JsonResult(_playlistManager.ChannelCount);
    }
    
    // GET: api/recordings/channels/search?query={query}
    [HttpGet("channels/search")]
    public ActionResult<IEnumerable<ChannelInfo>> SearchChannels([FromQuery] string query)
    {
        _logger.LogInformation("Search channels endpoint called with query: '{query}'", query);
        
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            _logger.LogInformation("Query is empty or too short, returning empty result");
            return Ok(Array.Empty<ChannelInfo>());
        }
        
        try
        {
            var allChannels = _playlistManager.GetPlaylistItems();
            _logger.LogInformation("Retrieved {count} channels from playlist", allChannels.Count());
            
            var matchingChannels = allChannels
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10) // Limit results to prevent large responses
                .ToList();
                
            _logger.LogInformation("Found {count} matching channels for query '{query}'", matchingChannels.Count, query);
            return Ok(matchingChannels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching channels with query '{query}'", query);
            return StatusCode(500, "An error occurred while searching channels");
        }
    }

    // POST: api/recordings/upload-m3u
    [HttpPost("upload-m3u")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadM3u(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please upload a valid M3U file.");
        }

        var uploadPath = _settingsManager.Settings.DataPath;
        if (string.IsNullOrEmpty(uploadPath))
        {
            return BadRequest("Upload path is not configured.");
        }
        if (!Directory.Exists(uploadPath))
        {
            return BadRequest("Upload path does not exist.");
        }
        if (!Directory.Exists(uploadPath) || !new DirectoryInfo(uploadPath).Attributes.HasFlag(FileAttributes.Directory))
        {
            return BadRequest("Upload path is not a directory.");
        }
        if (!uploadPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            uploadPath += Path.DirectorySeparatorChar;
        }
        // test if we can write to the upload folder
        var testFilePath = Path.Combine(uploadPath, $"{Guid.NewGuid()}.bin");
        try
        {
            using (var stream = new FileStream(testFilePath, FileMode.Create))
            {
                await stream.WriteAsync(new byte[0], 0, 0);
            }
            System.IO.File.Delete(testFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload path is not writable.");
            return BadRequest("Upload path is not writable.");
        }

        var filePath = Path.Combine(uploadPath, file.FileName);

        try
        {
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var settings = _settingsManager.Settings;
            settings.M3uPlaylistPath = filePath;
            _settingsManager.Settings = settings;

            _logger.LogDebug("M3U file uploaded successfully to {filePath}", filePath);
            
            return Ok(new { message = "M3U file uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading M3U file.");
            return StatusCode(500, "An error occurred while uploading the file.");
        }
    }

    // Model for task definition
    public class TaskDefinitionModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Model for task edit
    public class TaskEditModel
    {
        public Guid Id { get; set; }
        
        [JsonPropertyName("taskfile")]
        public string TaskFile { get; set; } = string.Empty;
    }
}