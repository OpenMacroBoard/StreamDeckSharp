using FluentAssertions;
using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests
{
    public class KeyBitmapTests
    {
        [Fact]
        public void NegativeOrZeroWidthCausesConstructorToThrow()
        {
            Action act_zero = () => new KeyBitmap(0, 1, null);
            Action act_negative = () => new KeyBitmap(-1, 1, null);

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
            Action act_zero = () => new KeyBitmap(1, 0, null);
            Action act_negative = () => new KeyBitmap(1, -1, null);

            act_zero.Should()
                .Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("height");

            act_negative.Should()
                .Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("height");
        }

        [Fact]
        public void ImageDataArrayLengthMissmatchThrowsException()
        {
            Action act_correctParams = () => new KeyBitmap(2, 2, new byte[12]);
            Action act_incorrectParams = () => new KeyBitmap(3, 3, new byte[5]);

            act_correctParams.Should().NotThrow();
            act_incorrectParams.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EqualsReturnsFalseIfWidthIsDifferent()
        {
            var key1 = new KeyBitmap(4, 4, null);
            var key2 = new KeyBitmap(3, 4, null);
            var key3 = new KeyBitmap(4, 4, null);

            key1.Should().NotBe(key2);
            key1.Should().NotBeSameAs(key2);

            key1.Should().Be(key3);
            key1.Should().NotBeSameAs(key3);

            KeyBitmap.Equals(key1, key3).Should().BeTrue();
        }

        [Fact]
        public void EqualsReturnsFalseIfHeightIsDifferent()
        {
            var key1 = new KeyBitmap(4, 4, null);
            var key2 = new KeyBitmap(4, 3, null);
            var key3 = new KeyBitmap(4, 4, null);

            key1.Should().NotBe(key2);
            key1.Should().NotBeSameAs(key2);

            key1.Should().Be(key3);
            key1.Should().NotBeSameAs(key3);

            KeyBitmap.Equals(key1, key3).Should().BeTrue();
        }

        [Fact]
        public void EqualsReturnsFalseIfOnlyOneElementIsNull()
        {
            var key = new KeyBitmap(4, 4, null);
            KeyBitmap.Equals(null, null).Should().BeTrue();
            KeyBitmap.Equals(null, key).Should().BeFalse();
            KeyBitmap.Equals(key, null).Should().BeFalse();
        }


        [Fact]
        public void EqualsReturnsFalseIfDataDoesNotMatch()
        {
            var key1 = new KeyBitmap(1, 1, null);
            var key2 = new KeyBitmap(1, 1, new byte[3]);

            key1.Should().NotBe(key2);
            key2.Should().NotBe(key1);
        }

        [Fact]
        public void KeyBitmapsWithDifferentBgrValuesAreNotEqual()
        {
            var key1 = new KeyBitmap(1, 1, new byte[3]);

            var key2Data = new byte[3] { 1, 2, 3 };
            var key2 = new KeyBitmap(1, 1, key2Data);

            key1.Should().NotBe(key2);
            key2.Should().NotBe(key1);
        }

        [Fact(DisplayName = "All equality methods behave the same way.")]
        public void AllEqualityMethodsBehaveTheSameWay()
        {
            var key1 = new KeyBitmap(1, 1, new byte[3]);
            var key2Data = new byte[3] { 1, 2, 3 };
            var key2 = new KeyBitmap(1, 1, key2Data);
            var key3 = new KeyBitmap(1, 1, new byte[3]);

            var equalityMethods = new List<Func<KeyBitmap, KeyBitmap, bool>>()
            {
                KeyBitmap.Equals,
                (a,b) => a == b,
                (a,b) => !(a != b),
                (a,b) => a.Equals(b),
            };

            foreach(var eq in equalityMethods)
            {
                eq(key1, key3).Should().BeTrue();
                eq(key3, key1).Should().BeTrue();
                eq(key1, key2).Should().BeFalse();
                eq(key2, key1).Should().BeFalse();
            }
        }

        [Fact]
        public void HashCodesDontMatchEasilyForDifferentObjects()
        {
            var key1 = new KeyBitmap(1, 1, new byte[3]);
            var key2Data = new byte[3] { 1, 2, 3 };
            var key2 = new KeyBitmap(1, 1, key2Data);
            var key3 = new KeyBitmap(1, 1, null);
            var key4 = new KeyBitmap(100, 100, new byte[100 * 100 * 3]);

            var hash1 = key1.GetHashCode();
            var hash2 = key2.GetHashCode();
            var hash3 = key3.GetHashCode();
            var hash4 = key4.GetHashCode();

            hash1.Should().NotBe(hash2);
            hash1.Should().NotBe(hash3);
            hash1.Should().NotBe(hash4);
        }
    }
}


