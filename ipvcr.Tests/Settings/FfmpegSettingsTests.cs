using System;
using ipvcr.Scheduling.Shared.Settings;
using Xunit;

namespace ipvcr.Tests
{
    public class FfmpegSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new FfmpegSettings();

            // Assert
            Assert.Equal("mp4", settings.FileType);
            Assert.Equal("libx264", settings.Codec);
            Assert.Equal("aac", settings.AudioCodec);
            Assert.Equal("1000k", settings.VideoBitrate);
            Assert.Equal("128k", settings.AudioBitrate);
            Assert.Equal("1280x720", settings.Resolution);
            Assert.Equal("30", settings.FrameRate);
            Assert.Equal("16:9", settings.AspectRatio);
            Assert.Equal("mp4", settings.OutputFormat);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new FfmpegSettings();

            // Act
            settings.FileType = "mkv";
            settings.Codec = "libvpx";
            settings.AudioCodec = "libvorbis";
            settings.VideoBitrate = "2000k";
            settings.AudioBitrate = "192k";
            settings.Resolution = "1920x1080";
            settings.FrameRate = "60";
            settings.AspectRatio = "21:9";
            settings.OutputFormat = "webm";

            // Assert
            Assert.Equal("mkv", settings.FileType);
            Assert.Equal("libvpx", settings.Codec);
            Assert.Equal("libvorbis", settings.AudioCodec);
            Assert.Equal("2000k", settings.VideoBitrate);
            Assert.Equal("192k", settings.AudioBitrate);
            Assert.Equal("1920x1080", settings.Resolution);
            Assert.Equal("60", settings.FrameRate);
            Assert.Equal("21:9", settings.AspectRatio);
            Assert.Equal("webm", settings.OutputFormat);
        }
    }
}