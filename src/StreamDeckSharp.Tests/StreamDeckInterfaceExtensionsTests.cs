using FluentAssertions;
using Moq;
using OpenMacroBoard.SDK;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class StreamDeckInterfaceExtensionsTests
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(11, true)]
        [InlineData(200, false)]
        public void TestKeyEventArgs(int keyId, bool isDown)
        {
            var eventArg = new KeyEventArgs(keyId, isDown);
            eventArg.Key.Should().Be(keyId);
            eventArg.IsDown.Should().Be(isDown);
        }

        [Fact]
        public void ConnectionEventArgsStoresTheValueAsExpected()
        {
            var eventArg = new ConnectionEventArgs(false);
            eventArg.NewConnectionState.Should().BeFalse();

            eventArg = new ConnectionEventArgs(true);
            eventArg.NewConnectionState.Should().BeTrue();
        }

        [Fact]
        public void KeyPositionEnumeratorWorksAsExpected()
        {
            var r1 = new Rectangle(4, 6, 4, 8);
            var r2 = new Rectangle(4, 6, 3, 7);
            var r3 = new Rectangle(4, 6, 2, 6);
            var r4 = new Rectangle(4, 6, 1, 5);
            var keys = new Rectangle[] { r1, r2, r3, r4 };

            var collection = new KeyPositionCollection(keys);
            collection.SequenceEqual(keys).Should().BeTrue();

            for (var i = 0; i < keys.Length; i++)
            {
                collection[i].Should().BeEquivalentTo(keys[i]);
            }

            var cnt = 0;
            foreach (var element in (IEnumerable)collection)
            {
                element.Should().BeEquivalentTo(keys[cnt]);
                cnt++;
            }
        }

        [Fact(DisplayName = "KeyPositionCollection.ctor throws ArgumentException if area is zero")]
        public void KeyPositionCtorWithAreaZeroThrowsException()
        {
            var r1 = new Rectangle(4, 4, 0, 5);
            var r2 = new Rectangle(4, 4, 5, 0);

            var construct = new Action(() =>
            {
                var collection = new KeyPositionCollection(new Rectangle[] { r1, r2 });
            });

            construct.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "KeyPositionCollection.ctor throws ArgumentException if width or height is less than one.")]
        public void KeyPositionCtorThrowsIfHeightOrWidthLessThanOne()
        {
            var r1 = new Rectangle(4, 4, -5, 10);
            var r2 = new Rectangle(4, 4, 10, -5);

            var construct = new Action(() =>
            {
                var collection = new KeyPositionCollection(new Rectangle[] { r1, r2 });
            });

            construct.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "KeyPositionCollection.ctor throws ArgumentException if left or top is negative.")]
        public void KeyPositionCtorThrowsIfLeftOrTopIsNegative()
        {
            var r1 = new Rectangle(-1, 0, 10, 20);
            var r2 = new Rectangle(5, -5, 20, 10);

            var construct = new Action(() =>
            {
                var collection = new KeyPositionCollection(new Rectangle[] { r1, r2 });
            });

            construct.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetKeys_Calls_SetForEveryKey()
        {
            var mock = new Mock<IMacroBoard>();
            var key = KeyBitmap.Create.FromRgb(1, 2, 3);

            //use a number of keys that can't be ordered into a square
            //to catch hard coded values for stream deck (+mini)
            var keyCnt = 37;
            var keyCollection = new KeyPositionCollection(Enumerable.Range(0, keyCnt).Select(i => new Rectangle(0, 0, 1, 1)));
            var keySetCalled = new int[keyCnt];

            mock.Setup(d => d.Keys).Returns(keyCollection);
            mock.Setup(d =>
                d.SetKeyBitmap(
                    It.IsInRange<int>(0, keyCnt - 1, Moq.Range.Inclusive),
                    It.Is<KeyBitmap>(k => ReferenceEquals(key, k))
                )
            )
            .Callback<int, KeyBitmap>((i, bmp) =>
            {
                keySetCalled[i]++;
            });

            var deck = mock.Object;
            deck.SetKeyBitmap(key);

            for (var i = 0; i < keyCnt; i++)
            {
                keySetCalled[i].Should().Be(1, "because all keys should be called exactly once");
            }
        }

        [Fact]
        public void ClearKeysShouldCallSetForCorrespondingKeys()
        {
            var mock = new Mock<IMacroBoard>();

            //use a number of keys that can't be ordered into a square
            //to catch hard coded values for stream deck (+mini)
            var keyCnt = 37;
            var keyCollection = new KeyPositionCollection(Enumerable.Range(0, keyCnt).Select(i => new Rectangle(0, 0, 1, 1)));
            var keySetCalled = new int[keyCnt];

            mock.Setup(d => d.Keys).Returns(keyCollection);
            mock.Setup(d =>
                d.SetKeyBitmap(
                    It.IsInRange<int>(0, keyCnt - 1, Moq.Range.Inclusive),
                    It.Is<KeyBitmap>(k => KeyBitmap.Black.Equals(k))
                )
            )
            .Callback<int, KeyBitmap>((i, bmp) =>
            {
                keySetCalled[i]++;
            });

            var keyToClear = 7;
            var deck = mock.Object;
            deck.ClearKey(keyToClear);

            for (var i = 0; i < keyCnt; i++)
            {
                if (i == keyToClear)
                {
                    keySetCalled[i].Should().Be(1, $"because the cleared key {i} should be called once.");
                }
                else
                {
                    keySetCalled[i].Should().Be(0, $"because key {i} was not cleared.");
                }
            }


            keySetCalled[keyToClear] = 0;
            deck.ClearKeys();

            for (var i = 0; i < keyCnt; i++)
            {
                keySetCalled[i].Should().Be(1, $"because the cleared key {i} should be called once.");
            }
        }
    }
}

