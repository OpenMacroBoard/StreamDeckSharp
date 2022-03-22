using FluentAssertions;
using OpenMacroBoard.SDK;
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
    }
}
