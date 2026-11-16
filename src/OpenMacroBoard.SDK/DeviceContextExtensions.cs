using OpenMacroBoard.SDK.Internals;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A collection of extensions for <see cref="IDeviceContext"/>.
/// </summary>
public static class DeviceContextExtensions
{
    /// <summary>
    /// Wait for and open the first detected <see cref="IMacroBoard"/> in this context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If there are multiple devices connected it's undefined which one you will get.
    /// If you want to specify a filter to get a specific device use
    /// <see cref="OpenAsync(IDeviceContext, Func{IDeviceReference, bool}, CancellationToken)"/>.
    /// </para>
    /// </remarks>
    public static Task<IMacroBoard> OpenAsync(
        this IDeviceContext context,
        CancellationToken cancellationToken = default
    )
    {
        return context.OpenAsync(_ => true, cancellationToken);
    }

    /// <summary>
    /// Wait for and open the first matching <see cref="IMacroBoard"/> in this context.
    /// </summary>
    public static async Task<IMacroBoard> OpenAsync(
        this IDeviceContext context,
        Func<IDeviceReference, bool> selector,
        CancellationToken cancellationToken = default
    )
    {
        return (await context.GetDeviceReferenceAsync(selector, cancellationToken)).Open();
    }

    /// <summary>
    /// Wait for and return the first matching <see cref="IDeviceReference"/>.
    /// </summary>
    public static async Task<IDeviceReference> GetDeviceReferenceAsync(
        this IDeviceContext context,
        Func<IDeviceReference, bool> selector,
        CancellationToken cancellationToken = default
    )
    {
        // check if there already is a matching device.
        var devRef = context.KnownDevices.FirstOrDefault(selector);

        if (devRef is not null)
        {
            return devRef;
        }

        // if not, register event handler
        // and wait for a matching device to show up

        var eventSync = new object();
        var waitForDeviceBlocker = new TaskCompletionSource<int>();

        IDeviceReference? foundDevice = null;

        void ProcessReport(DeviceStateReport report)
        {
            lock (eventSync!)
            {
                if (foundDevice != null)
                {
                    // ignore events if we've already found a device.
                    return;
                }

                if (report.Connected && selector(report.DeviceReference))
                {
                    foundDevice = report.DeviceReference;
                    waitForDeviceBlocker!.TrySetResult(0);
                }
            }
        }

        IDisposable subscription = null!;
        CancellationTokenRegistration registration = default;

        try
        {
            registration = cancellationToken.Register(() => waitForDeviceBlocker.TrySetCanceled());
            var deviceReportObserver = new DeviceStateObserver(ProcessReport);
            subscription = context.DeviceStateReports.Subscribe(deviceReportObserver);

            await waitForDeviceBlocker.Task;
        }
        finally
        {
            await registration.DisposeAsync();
            subscription.Dispose();
        }

#pragma warning disable S2583 // Conditionally executed code should be reachable
        if (foundDevice is null)
#pragma warning restore S2583
        {
            // We don't document that exception because it shouldn't happen,
            // and we don't expect the consumer to handle this exception type.

#pragma warning disable RCS1140 // Add exception to documentation comment.
            throw new InvalidOperationException("Couldn't locate device with matching criteria. This is a bug, please file an issue.");
#pragma warning restore RCS1140
        }

        return foundDevice;
    }
}
