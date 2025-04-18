using System;
using ipvcr.Scheduling.Shared.Settings;
using Xunit;

namespace ipvcr.Tests
{
    public class SchedulerSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new SchedulerSettings();
            
            // Assert
            Assert.Equal("/media", settings.MediaPath);
            Assert.Equal("/data", settings.DataPath);
            Assert.True(settings.RemoveTaskAfterExecution);
        }
        
        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new SchedulerSettings();
            
            // Act
            settings.MediaPath = "/custom/media";
            settings.DataPath = "/custom/data";
            settings.RemoveTaskAfterExecution = false;
            
            // Assert
            Assert.Equal("/custom/media", settings.MediaPath);
            Assert.Equal("/custom/data", settings.DataPath);
            Assert.False(settings.RemoveTaskAfterExecution);
        }
    }
}