namespace ipvcr.Scheduling.Shared.Settings;
public class FfmpegSettings
{
    // class for managing ffmpeg parameters such as file type, codec, etc.
    public string FileType { get; set; } = "mp4";
    public string Codec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public string VideoBitrate { get; set; } = "1000k";
    public string AudioBitrate { get; set; } = "128k";
    public string Resolution { get; set; } = "1280x720";
    public string FrameRate { get; set; } = "30";
    public string AspectRatio { get; set; } = "16:9";
    public string OutputFormat { get; set; } = "mp4";
}
