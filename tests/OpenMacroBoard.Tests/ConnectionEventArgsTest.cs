using AwesomeAssertions;
using OpenMacroBoard.SDK;
using Xunit;

namespace OpenMacroBoard.Tests;

public class ConnectionEventArgsTest
{
    [Fact]
    public void NewConnectionPropertyShouldBeSet()
    {
        var con = new ConnectionEventArgs(true);
        con.NewConnectionState.Should().BeTrue();

        con = new ConnectionEventArgs(false);
        con.NewConnectionState.Should().BeFalse();
    }
}
