namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public class TimeSpanExtensionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenOnlySeconds_WhenToHumanString_ThenReturnsSeconds()
    {
        TimeSpan.FromSeconds(5).ToHumanString().Should().Be("5 seconds");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenOneSecond_WhenToHumanString_ThenReturnsSingular()
    {
        TimeSpan.FromSeconds(1).ToHumanString().Should().Be("1 second");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMinutes_WhenToHumanString_ThenReturnsMinutes()
    {
        TimeSpan.FromMinutes(10).ToHumanString().Should().Be("10 minutes");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenOneMinute_WhenToHumanString_ThenReturnsSingular()
    {
        TimeSpan.FromMinutes(1).ToHumanString().Should().Be("1 minute");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenHours_WhenToHumanString_ThenReturnsHours()
    {
        TimeSpan.FromHours(2).ToHumanString().Should().Be("2 hours");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenDays_WhenToHumanString_ThenReturnsDays()
    {
        TimeSpan.FromDays(1).ToHumanString().Should().Be("1 day");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenComplexDuration_WhenToHumanString_ThenJoinsParts()
    {
        new TimeSpan(1, 2, 3).ToHumanString().Should().Be("1 hour 2 minutes");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenZeroDuration_WhenToHumanString_ThenReturnsZeroSeconds()
    {
        TimeSpan.Zero.ToHumanString().Should().Be("0 seconds");
    }
}
