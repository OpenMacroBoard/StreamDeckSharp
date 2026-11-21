using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapTests
{
    [Fact]
    public void NegativeOrZeroWidthCausesConstructorToThrow()
    {
        Action act_zero = () => _ = KeyBitmap.Create.FromBgr24Array(0, 1, null);
        Action act_negative = () => _ = KeyBitmap.Create.FromBgr24Array(-1, 1, null);

        act_zero.Should()
            .Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("width");

        act_negative.Should()
            .Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("width");
    }

    [Fact]
    public void NegativeOrZeroHeightCausesConstructorToThrow()
    {
        Action act_zero = () => _ = KeyBitmap.Create.FromBgr24Array(1, 0, null);
        Action act_negative = () => _ = KeyBitmap.Create.FromBgr24Array(1, -1, null);

        act_zero.Should()
            .Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("height");

        act_negative.Should()
            .Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("height");
    }

    [Fact]
    public void ImageDataArrayLengthMismatchThrowsException()
    {
        Action act_correctParams = () => _ = KeyBitmap.Create.FromBgr24Array(2, 2, new byte[12]);
        Action act_incorrectParams = () => _ = KeyBitmap.Create.FromBgr24Array(3, 3, new byte[5]);

        act_correctParams.Should().NotThrow();
        act_incorrectParams.Should().Throw<ArgumentException>();
    }
}
