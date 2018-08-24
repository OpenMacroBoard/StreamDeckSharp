using System;
using System.Threading;
using System.Windows.Threading;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.VirtualBoard
{
    /// <summary>
    /// A factory that allows you to spawn new virtual boards
    /// </summary>
    public static class BoardFactory
    {
        /// <summary>
        /// Spawns a virtual board that is simular to the classic Stream Deck
        /// </summary>
        /// <returns></returns>
        public static IMacroBoard SpawnVirtualBoard()
            => SpawnVirtualBoard(new KeyPositionCollection(5, 3, 72, 25));

        /// <summary>
        /// Spawns a new virtual macro board with a given <paramref name="keyLayout"/>.
        /// </summary>
        /// <param name="keyLayout"></param>
        /// <returns></returns>
        public static IMacroBoard SpawnVirtualBoard(IKeyPositionCollection keyLayout)
        {
            VirtualBoardViewModel boardViewModel = null;
            VirtualBoardWindow boardWindow = null;
            Exception exceptionWasThrown = null;

            using (var waitForBoardWindow = new ManualResetEvent(false))
            {
                var uiThread = new Thread(() =>
                {
                    try
                    {
                        boardViewModel = new VirtualBoardViewModel(keyLayout);
                        boardWindow = new VirtualBoardWindow(boardViewModel);
                    }
                    catch (Exception ex)
                    {
                        exceptionWasThrown = ex;
                        return;
                    }
                    finally
                    {
                        waitForBoardWindow.Set();
                    }

                    boardWindow.Show();
                    Dispatcher.Run();
                })
                {
                    IsBackground = true
                };

                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();

                waitForBoardWindow.WaitOne();

                if (exceptionWasThrown != null)
                    throw exceptionWasThrown;

                return boardViewModel;
            }
        }
    }
}
