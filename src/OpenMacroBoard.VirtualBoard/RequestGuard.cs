using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// Keeps other websites away from the virtual board.
/// </summary>
/// <remarks>
/// <para>
/// The board listens on the local machine, so any website opened in a browser on that machine
/// could try to reach it. Two checks prevent that: the request must be addressed to an IP
/// address, to "localhost" or to the name of this machine (a foreign name that resolves to
/// this machine is refused), and a request that names its origin must come from the page of
/// the board itself.
/// </para>
/// </remarks>
internal static class RequestGuard
{
    public static Task RejectForeignRequestsAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;

        if (!IsOwnHost(request.Host.Host) || !IsOwnOrigin(request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return next(context);
    }

    internal static bool IsOwnHost(string host)
    {
        var name = host.Trim('[', ']');

        return IPAddress.TryParse(name, out _)
            || string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, Dns.GetHostName(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOwnOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();

        if (origin.Length == 0)
        {
            // Not a browser, or a plain navigation to the page.
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            && string.Equals(originUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
