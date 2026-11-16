using Overby.Extensions.AsyncBinaryReaderWriter;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OpenMacroBoard.SocketIO.Internals;

internal sealed class TcpClientBinaryIO : IDisposable
{
    private bool disposed = false;

    public TcpClientBinaryIO(TcpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;

        var stream = client.GetStream();

        Reader = new(stream, Encoding.UTF8, true);
        Writer = new(stream, Encoding.UTF8, true);
    }

    public AsyncBinaryReader Reader { get; }
    public AsyncBinaryWriter Writer { get; }

    public TcpClient Client { get; }

    public static async Task<TcpClientBinaryIO> ConnectAsync(IPEndPoint endPoint)
    {
        var client = new TcpClient();
        await client.ConnectAsync(endPoint.Address, endPoint.Port);
        return new TcpClientBinaryIO(client);
    }

    public void Dispose()
    {
        lock (Client)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            try
            {
                Client.Close();
                Client.Dispose();
            }
            catch
            {
                // we don't want to throw in Dispose().
            }
        }
    }
}
