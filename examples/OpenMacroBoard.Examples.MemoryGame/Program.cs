using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using System;
using System.Threading;

namespace OpenMacroBoard.Examples.MemoryGame;

internal static class Program
{
    private static readonly Random Rnd = new();

    /// <summary>
    /// Positon of restart button
    /// </summary>
    private static readonly int RestartKey = 7;
    private static readonly int[] OpenCard = new int[2];

    private static readonly KeyBitmap[] IconsActive = new KeyBitmap[7];
    private static readonly KeyBitmap[] IconsInactive = new KeyBitmap[7];

    /// <summary>
    /// 14 slots for memory (7x2 cards)
    /// </summary>
    private static readonly int[] GameState = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6];
    private static readonly bool[] CardVisible = new bool[14];

    private static readonly AutoResetEvent ThreadSleeper = new(false);
    private static readonly Lock CloseCardLock = new();

    private static int mode = 0;
    private static KeyBitmap restartIcon;

    private static void Main()
    {
        InitializeIconBitmaps();

        using var s = ExampleHelper.OpenBoard(d => d.Keys.Count == 15);

        s.KeyStateChanged += StreamDeckKeyHandler;
        StartGame(s);

        ExampleHelper.WaitForKeyToExit();
    }

    private static void StartGame(IMacroBoard deck)
    {
        // shuffle memory cards
        OpenCard[0] = -1;
        OpenCard[1] = -1;
        mode = 0;
        SuffleArray(GameState, Rnd);

        for (var i = 0; i < CardVisible.Length; i++)
        {
            CardVisible[i] = false;
        }

        // Clear all tiles (except restart key)
        for (var i = 0; i < deck.Keys.Count; i++)
        {
            if (i != RestartKey)
            {
                deck.ClearKey(i);
            }
        }

        // (Re-)Draw restart key image
        deck.SetKeyBitmap(RestartKey, restartIcon);
    }

    private static void RefreshKeyIcon(IMacroBoard deck, int cardId)
    {
        var keyId = cardId >= RestartKey ? cardId + 1 : cardId;

        if (CardVisible[cardId])
        {
            if (OpenCard[0] == cardId || OpenCard[1] == cardId)
            {
                deck.SetKeyBitmap(keyId, IconsInactive[GameState[cardId]]);
            }
            else
            {
                deck.SetKeyBitmap(keyId, IconsActive[GameState[cardId]]);
            }
        }
        else
        {
            deck.SetKeyBitmap(keyId, KeyBitmap.Black);
        }
    }

    private static void InitializeIconBitmaps()
    {
        restartIcon = IconLoader.LoadIconByName("restart.png", true);
        for (var i = 0; i < IconsActive.Length; i++)
        {
            var name = $"card{i}.png";
            IconsActive[i] = IconLoader.LoadIconByName(name, true);
            IconsInactive[i] = IconLoader.LoadIconByName(name, false);
        }
    }

    private static void SuffleArray<T>(T[] array, Random rnd)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var pick = rnd.Next(array.Length - i) + i;

            // Swap elements
            (array[pick], array[i]) = (array[i], array[pick]);
        }
    }

    private static void CloseCards(IMacroBoard deck)
    {
        lock (CloseCardLock)
        {
            if (mode != 2)
            {
                return;
            }

            CardVisible[OpenCard[0]] = false;
            CardVisible[OpenCard[1]] = false;
            var c1 = OpenCard[0];
            var c2 = OpenCard[1];
            OpenCard[0] = -1;
            OpenCard[1] = -1;
            RefreshKeyIcon(deck, c1);
            RefreshKeyIcon(deck, c2);
            mode = 0;
        }
    }

    private static void StreamDeckKeyHandler(object sender, KeyEventArgs e)
    {
        if (sender is not IMacroBoard deck)
        {
            return;
        }

        if (e.Key == RestartKey && e.IsDown)
        {
            StartGame(deck);
            return;
        }

        if (e.IsDown)
        {
            if (mode == 2)
            {
                ThreadSleeper.Set();
                CloseCards(deck);
            }

            var cardId = e.Key < RestartKey ? e.Key : e.Key - 1;
            if (mode == 0)
            {
                if (!CardVisible[cardId])
                {
                    mode = 1;
                    OpenCard[0] = cardId;
                    CardVisible[cardId] = true;
                    RefreshKeyIcon(deck, cardId);
                }
            }
            else if (mode == 1 && !CardVisible[cardId])
            {
                OpenCard[1] = cardId;
                CardVisible[cardId] = true;
                RefreshKeyIcon(deck, cardId);

                if (GameState[OpenCard[0]] == GameState[OpenCard[1]])
                {
                    mode = 0;
                    var c1 = OpenCard[0];
                    var c2 = OpenCard[1];
                    OpenCard[0] = -1;
                    OpenCard[1] = -1;
                    RefreshKeyIcon(deck, c1);
                    RefreshKeyIcon(deck, c2);
                }
                else
                {
                    mode = 2;

                    new Thread(() =>
                    {
                        ThreadSleeper.WaitOne(2000);
                        CloseCards(deck);
                    })
                    .Start();
                }
            }
        }
    }
}
