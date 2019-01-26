<div align="center">
  <img src="https://raw.githubusercontent.com/OpenStreamDeck/StreamDeckSharp/master/doc/images/banner/StreamDeckSharpBanner_150px.png">
</div>

-----------------

**StreamDeckSharp is a simple (unofficial) .NET interface for the [Elgato Stream Deck](https://www.elgato.com/en/gaming/stream-deck)**

[![GitHub release](https://img.shields.io/github/release/OpenStreamDeck/StreamDeckSharp.svg)](https://github.com/OpenStreamDeck/StreamDeckSharp/releases) [![license](https://img.shields.io/github/license/OpenStreamDeck/StreamDeckSharp.svg)](https://github.com/OpenStreamDeck/StreamDeckSharp/blob/master/LICENSE.md)

#### [Recent Changes](CHANGELOG.md)

## Quickstart _(TL;DR)_
***At the moment only Windows is supported (tested with 10, should also work with 8, 7, Vista and XP)***
1. Add StreamDeckSharp reference (via nuget or download latest release)
2. Add a using directive for StreamDeckSharp: `using StreamDeckSharp;`

I want to...              | Code (C#)
------------------------- | ---------------------------------------------------------
create a device reference | `var deck = StreamDeck.OpenDevice();`  
set the brightness        | `deck.SetBrightness(50);`
create bitmap for key     | `var bitmap = KeyBitmap.Create.FromFile("icon.png")`
set key image             | `deck.SetKeyBitmap(keyId, bitmap)`
clear key image           | `deck.ClearKey(keyId)`
process key events        | `deck.KeyStateChanged += KeyHandler;`

**Make sure to dispose the device reference correctly** _(use `using` whenever possible)_

## Examples
If you want to see some examples take a look at the [example projects](https://github.com/OpenMacroBoard/StreamDeckSharp.ExampleCollection).  
Here is a short example called "Austria". Copy the code and start hacking :wink:

```C#
using System;
using OpenMacroBoard.SDK;
using StreamDeckSharp;

namespace StreamDeckSharp.Examples.Austria
{
    class Program
    {
        static void Main(string[] args)
        {
            //This example is designed for the 5x3 (original) Stream Deck.

            //Create some color we use later to draw the flag of austria
            var red = KeyBitmap.Create.FromRgb(237, 41, 57);
            var white = KeyBitmap.Create.FromRgb(255, 255, 255);
            var rowColors = new KeyBitmap[] { red, white, red };

            //Open the Stream Deck device
            using (var deck = StreamDeck.OpenDevice())
            {
                deck.SetBrightness(100);

                //Send the bitmap informaton to the device
                for (int i = 0; i < deck.Keys.Count; i++)
                    deck.SetKeyBitmap(i, rowColors[i / 5]);

                Console.ReadKey();
            }
        }
    }
}
```

Here is what the "Rainbow" example looks like after pressing some keys

![Rainbow example photo](doc/images/rainbow_example.png?raw=true "Rainbow demo after pressing some keys")

### Play video on StreamDeck
Here is a short demo of playing a video on the stream deck device.

[![Demo video of the example](https://i.imgur.com/8tlkaIg.png)](http://www.youtube.com/watch?v=tNwUG0sPmKw)  
_*The glitches you can see are already fixed._

More about that in the Wiki: [Play video on StreamDeck](https://github.com/OpenStreamDeck/StreamDeckSharp/wiki/Play-video-on-StreamDeck)

---
 
###### This project is not related to *Elgato Systems GmbH* in any way

---
