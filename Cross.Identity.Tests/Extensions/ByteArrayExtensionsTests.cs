namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public class ByteArrayExtensionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullSource_WhenGetBytes_ThenThrowsArgumentNullException()
    {
        byte[]? source = null;

        var act = () => source!.GetBytes(1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenZeroLength_WhenGetBytes_ThenReturnsEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(0);

        result.Should().BeEmpty();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNegativeLength_WhenGetBytes_ThenReturnsEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(-1);

        result.Should().BeEmpty();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenLengthGreaterOrEqualSourceLength_WhenGetBytes_ThenReturnsCopy()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(5);

        result.Should().Equal(1, 2, 3);
        result.Should().NotBeSameAs(source);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenLengthLessThanSourceLength_WhenGetBytes_ThenReturnsFirstLengthBytes()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };

        var result = source.GetBytes(2);

        result.Should().Equal(1, 2);
        result.Should().HaveCount(2);
    }
}
