namespace Cross.Identity.Tests.Extensions;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ByteArrayExtensionsTests
{
    [Test]
    public void GivenNullSource_WhenGetBytes_ThenThrowsArgumentNullException()
    {
        byte[]? source = null;

        var act = () => source!.GetBytes(1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Test]
    public void GivenZeroLength_WhenGetBytes_ThenReturnsEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(0);

        result.Should().BeEmpty();
    }

    [Test]
    public void GivenNegativeLength_WhenGetBytes_ThenReturnsEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(-1);

        result.Should().BeEmpty();
    }

    [Test]
    public void GivenLengthGreaterOrEqualSourceLength_WhenGetBytes_ThenReturnsCopy()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(5);

        result.Should().Equal(1, 2, 3);
        result.Should().NotBeSameAs(source);
    }

    [Test]
    public void GivenLengthLessThanSourceLength_WhenGetBytes_ThenReturnsFirstLengthBytes()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };

        var result = source.GetBytes(2);

        result.Should().Equal(1, 2);
        result.Should().HaveCount(2);
    }
}
