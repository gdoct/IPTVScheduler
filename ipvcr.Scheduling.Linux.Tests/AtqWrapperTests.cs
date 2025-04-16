namespace ipvcr.Scheduling.Linux.Tests;

using System.ComponentModel.DataAnnotations;
using ipvcr.Scheduling.Linux;
using ipvcr.Scheduling.Shared;
using Moq;

public class AtqWrapperTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ISettingsManager> _settingsManagerMock;

    public AtqWrapperTests()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _settingsManagerMock = new Mock<ISettingsManager>();
    }

    [Fact]
    public void AtqWrapper_ThrowsIfNotInstalled()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atq")).Returns(("", "", 1));
        Assert.Throws<MissingDependencyException>(() => new AtqWrapper(_processRunnerMock.Object, _settingsManagerMock.Object));
    }

    [Fact]
    public void AtqWrapper_GetScheduledTasks_ShouldReturnJobIds()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        var atqWrapper = new AtqWrapper(_processRunnerMock.Object, _settingsManagerMock.Object);

        var expectedOutput = "1 2023-10-01 12:00 a\n2 2023-10-02 14:00 a\n";
        _processRunnerMock.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((expectedOutput, "", 0));

        // Act
        var result = atqWrapper.GetScheduledTasks().ToList();

        // Assert
        Assert.Equal(new[] { 1, 2 }, result);
    }

    [Fact]
    public void AtqWrapper_GetScheduledTasks_FailsIfOutputLinesFromAtqDontStartWithNumber()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        var atqWrapper = new AtqWrapper(_processRunnerMock.Object, _settingsManagerMock.Object);

        var expectedOutput = "abc 2023-10-01 12:00 a\nxyz 2023-10-02 14:00 a\n";
        _processRunnerMock.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((expectedOutput, "", 0));

        // Act
        var result = atqWrapper.GetScheduledTasks().ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AtqWrapper_GetScheduledTasks_ThrowsOnError()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        var atqWrapper = new AtqWrapper(_processRunnerMock.Object, _settingsManagerMock.Object);

        var expectedError = "Error occurred";
        _processRunnerMock.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns(("", expectedError, 1));
        var tasks = atqWrapper.GetScheduledTasks();
        var exception = Assert.Throws<InvalidOperationException>(() => tasks.ToList());
        Assert.Equal($"Failed to list tasks: {expectedError}", exception.Message);
    }
}