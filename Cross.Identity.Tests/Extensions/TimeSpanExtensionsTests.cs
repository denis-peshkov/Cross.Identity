namespace Cross.Identity.Tests.Extensions;

[Category(TestCategory.UNIT)]
[TestFixture]
public class TimeSpanExtensionsTests
{
    [Test]
    public void GivenOnlySeconds_WhenToHumanString_ThenReturnsSeconds()
    {
        TimeSpan.FromSeconds(5).ToHumanString().Should().Be("5 seconds");
    }

    [Test]
    public void GivenOneSecond_WhenToHumanString_ThenReturnsSingular()
    {
        TimeSpan.FromSeconds(1).ToHumanString().Should().Be("1 second");
    }

    [Test]
    public void GivenMinutes_WhenToHumanString_ThenReturnsMinutes()
    {
        TimeSpan.FromMinutes(10).ToHumanString().Should().Be("10 minutes");
    }

    [Test]
    public void GivenOneMinute_WhenToHumanString_ThenReturnsSingular()
    {
        TimeSpan.FromMinutes(1).ToHumanString().Should().Be("1 minute");
    }

    [Test]
    public void GivenHours_WhenToHumanString_ThenReturnsHours()
    {
        TimeSpan.FromHours(2).ToHumanString().Should().Be("2 hours");
    }

    [Test]
    public void GivenDays_WhenToHumanString_ThenReturnsDays()
    {
        TimeSpan.FromDays(1).ToHumanString().Should().Be("1 day");
    }

    [Test]
    public void GivenComplexDuration_WhenToHumanString_ThenJoinsParts()
    {
        new TimeSpan(1, 2, 3).ToHumanString().Should().Be("1 hour 2 minutes");
    }

    [Test]
    public void GivenZeroDuration_WhenToHumanString_ThenReturnsZeroSeconds()
    {
        TimeSpan.Zero.ToHumanString().Should().Be("0 seconds");
    }
}
