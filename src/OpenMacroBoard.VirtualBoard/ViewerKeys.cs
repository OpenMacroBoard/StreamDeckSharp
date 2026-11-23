using OpenMacroBoard.SDK;
using System.Collections.Generic;
using System.Threading;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// Combines the keys several viewers (browser pages) hold into the state of the board:
/// a key is down while at least one viewer holds it.
/// </summary>
internal sealed class ViewerKeys
{
    private readonly Lock gate = new();
    private readonly InMemoryMacroBoard board;
    private readonly HashSet<long>[] keyHolders;

    public ViewerKeys(InMemoryMacroBoard board)
    {
        this.board = board;
        keyHolders = new HashSet<long>[board.Keys.Count];

        for (var i = 0; i < keyHolders.Length; i++)
        {
            keyHolders[i] = [];
        }
    }

    /// <summary>
    /// Records that a viewer holds or releases a key. Keys the board does not have are ignored.
    /// </summary>
    public void SetViewerKeyState(long viewerId, int keyId, bool isDown)
    {
        if (keyId < 0 || keyId >= keyHolders.Length)
        {
            return;
        }

        lock (gate)
        {
            var holders = keyHolders[keyId];

            if (isDown)
            {
                holders.Add(viewerId);
            }
            else
            {
                holders.Remove(viewerId);
            }

            // The board raises its event only if the state of the key really changes.
            board.SetKeyState(keyId, holders.Count > 0);
        }
    }

    /// <summary>
    /// Releases every key a viewer holds, for example when its connection ends.
    /// </summary>
    public void ReleaseViewer(long viewerId)
    {
        for (var keyId = 0; keyId < keyHolders.Length; keyId++)
        {
            SetViewerKeyState(viewerId, keyId, isDown: false);
        }
    }
}
