namespace Cross.Identity.UnitTests.Extensions;

using Cross.Identity.Extensions;

[TestFixture]
public class ByteArrayExtensions_Tests
{
    [Test]
    public void GetBytes_WhenSourceNull_ShouldThrow()
    {
        byte[]? source = null;

        var act = () => source!.GetBytes(1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Test]
    public void GetBytes_WhenLengthZero_ShouldReturnEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(0);

        result.Should().BeEmpty();
    }

    [Test]
    public void GetBytes_WhenLengthNegative_ShouldReturnEmpty()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(-1);

        result.Should().BeEmpty();
    }

    [Test]
    public void GetBytes_WhenLengthGreaterOrEqualSourceLength_ShouldReturnCopy()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = source.GetBytes(5);

        result.Should().Equal(1, 2, 3);
        result.Should().NotBeSameAs(source);
    }

    [Test]
    public void GetBytes_WhenLengthLessThanSource_ShouldReturnFirstLengthBytes()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };

        var result = source.GetBytes(2);

        result.Should().Equal(1, 2);
        result.Should().HaveCount(2);
    }
}
