using System.ComponentModel;

namespace ipvcr.Scheduling.Linux.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public void RunProcess_WhenCalled_ReturnsOutputErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
#if WINDOWS
        var fileName = "ipconfig";
        var arguments = string.Empty;
#elif LINUX
        var fileName = "echo";
        var arguments = string.Empty;
#endif
        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments);

        // Assert
        Assert.True(output.Length > 0);
#if WINDOWS
        Assert.Equal("\r\n", error);
#else
        Assert.Equal("\n", error);
#endif
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void RunProcess_WhenCalledWithInvalidCommand_ReturnsErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileName = "invalidcommand";
        var arguments = string.Empty;

        // Act
#if WINDOWS
        Assert.Throws<Win32Exception>(() => processRunner.RunProcess(fileName, arguments));

#elif LINUX
        Assert.Throws<Win32Exception>(() => processRunner.RunProcess(fileName, arguments));
#endif
    }

    [Fact]
    public void RunProcess_WhenCalledWithInvalidArguments_ReturnsErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
#if WINDOWS
        var fileName = "ipconfig";
#elif LINUX
        var fileName = "grep";
#endif
        var arguments = "--invalid-argument";
        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments);

        // Assert
#if WINDOWS
        Assert.Contains("Error: unrecognized or incomplete command line", output);
        Assert.Equal(Environment.NewLine, error);
#else
        Assert.Equal(Environment.NewLine, output);
        Assert.Contains("grep: unrecognized option", error);
#endif

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void RunProcess_ShouldTimeout()
    {
        // Arrange
        var processRunner = new ProcessRunner();
#if WINDOWS
        var fileName = "cmd";
        var arguments = "";
#elif LINUX
        var fileName = "sleep";
        var arguments = "10";
#endif

        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments, 1000);

        // Assert
        Assert.Equal(string.Empty, output);
        Assert.Contains("timed out", error);
        Assert.NotEqual(0, exitCode);
    }
}
