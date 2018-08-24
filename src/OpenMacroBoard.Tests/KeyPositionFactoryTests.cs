using FluentAssertions;
using OpenMacroBoard.SDK;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class KeyPositionFactoryTests
    {
        [Fact]
        public void SimpleLayoutProducesExcpectedResults()
        {
            //creates a key layout with 2x2 = 4 keys, each 10x10px with 5px in between
            var keys = new KeyPositionCollection(2, 2, 20, 30, 5, 10);

            keys.Count.Should().Be(4);
            keys.Area.Left.Should().Be(0);
            keys.Area.Top.Should().Be(0);
            keys.Area.Width.Should().Be(45, $"because 2 keys each 20px wide and a gap of 5px");
            keys.Area.Height.Should().Be(70, $"because 2 keys each 30px high and a gap of 10px");
        }
    }
}
