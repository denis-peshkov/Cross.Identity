namespace Cross.Identity.UnitTests.Extensions;

[TestFixture]
public class TimeSpanExtensionsTests
{
    [Test]
    public void ToHumanString_WhenOnlySeconds_ReturnsSeconds()
    {
        TimeSpan.FromSeconds(5).ToHumanString().Should().Be("5 seconds");
    }

    [Test]
    public void ToHumanString_WhenOneSecond_ReturnsSingular()
    {
        TimeSpan.FromSeconds(1).ToHumanString().Should().Be("1 second");
    }

    [Test]
    public void ToHumanString_WhenMinutes_ReturnsMinutes()
    {
        TimeSpan.FromMinutes(10).ToHumanString().Should().Be("10 minutes");
    }

    [Test]
    public void ToHumanString_WhenOneMinute_ReturnsSingular()
    {
        TimeSpan.FromMinutes(1).ToHumanString().Should().Be("1 minute");
    }

    [Test]
    public void ToHumanString_WhenHours_ReturnsHours()
    {
        TimeSpan.FromHours(2).ToHumanString().Should().Be("2 hours");
    }

    [Test]
    public void ToHumanString_WhenDays_ReturnsDays()
    {
        TimeSpan.FromDays(1).ToHumanString().Should().Be("1 day");
    }

    [Test]
    public void ToHumanString_WhenComplex_JoinsParts()
    {
        new TimeSpan(1, 2, 3).ToHumanString().Should().Be("1 hour 2 minutes");
    }

    [Test]
    public void ToHumanString_WhenZero_ReturnsZeroSeconds()
    {
        TimeSpan.Zero.ToHumanString().Should().Be("0 seconds");
    }
}
