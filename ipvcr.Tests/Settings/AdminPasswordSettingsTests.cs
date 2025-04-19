using System;
using ipvcr.Scheduling.Shared.Settings;
using Xunit;

namespace ipvcr.Tests
{
    public class AdminPasswordSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new AdminPasswordSettings();

            // Assert
            Assert.Equal("admin", settings.AdminUsername);
            Assert.Equal("MDAwMDAwMDAwMDBpcHZjcg==", settings.AdminPassword);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new AdminPasswordSettings();

            // Act
            settings.AdminUsername = "customadmin";
            settings.AdminPassword = "custompassword";

            // Assert
            Assert.Equal("customadmin", settings.AdminUsername);
            Assert.Equal("custompassword", settings.AdminPassword);
        }
    }
}