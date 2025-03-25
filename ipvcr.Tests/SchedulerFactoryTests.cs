using ipvcr.Scheduling;

namespace ipvcr.Tests;

public class SchedulerFactoryTests
{
    [Fact]
    public void GetScheduler_ShouldReturCorrectSchedulerOrThrow()
    {

        #if WINDOWS
            var scheduler = SchedulerFactory.GetScheduler(PlatformID.Win32NT);
            Assert.NotNull(scheduler);
            Assert.Throws<PlatformNotSupportedException>(() => SchedulerFactory.GetScheduler(PlatformID.Unix));
        #elif LINUX
            var scheduler = SchedulerFactory.GetScheduler(PlatformID.Unix);
            Assert.NotNull(scheduler);
            Assert.Throws<PlatformNotSupportedException>(() => SchedulerFactory.GetScheduler(PlatformID.Win32NT));
        #endif
        Assert.Throws<NotImplementedException>(() => SchedulerFactory.GetScheduler(PlatformID.MacOSX));
    }
}
