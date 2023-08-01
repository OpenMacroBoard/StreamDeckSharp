# Changelog
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

## [v5.0.0-preview] - 2023-08-01
Again another preview because I'm pretty busy right now.
- Restructure how the HID communication is abstracted internally. This is useful for consumers when Elgato releases new hardware that can be controlled with the same software. The user of the library can now register new devices with existing "HID drivers" (for example if a Stream Deck MK.3 is released)
- `KeyBitmap.FromBgr24Array` was moved to `KeyBitmap.Create.FromBgr24Array`
- More HID Verification Tests
- Tested with real hardware
  - Stream Deck XL (FW v1.01.000)
  - Stream Deck Mini (FW v2.03.001)
  - Stream Deck (FW v1.0.191203)
  - Stream Deck MK.2 (FW v1.01.001)
    - Saw visual glitches in ImageGlitchTest.Rainbow without throttling
    - Added throttling to about 1,5 MB/s
    - Example video frame rate looked still ok with throttling

## [v4.0.0-preview] - 2022-11-18
A preview version (v4.0.0-preview) is currently available on nuget. At the moment there isn't a migration guide, but Jon Skeet has a project that show a the changes he had to make: https://github.com/OpenMacroBoard/StreamDeckSharp/issues/45#issuecomment-1079933751
Alternatively you can also take a look at the changes of the example projects: [Changes example projects](https://github.com/OpenMacroBoard/OpenMacroBoard.ExampleCollection/compare/1104c4f...3203bff)

## [3.2.0] - 2021-09-04
- Support for Stream Deck MK.2 (by jdahlblom - https://github.com/OpenMacroBoard/StreamDeckSharp/pull/36)
- Remove old cooldown mechanism and replace with a new throttle mechanism that limits the write speed
- Fix a few reported gaps between buttons (again)

## [3.0.0] - 2021-03-05
- Added HidSharp Source to SteamDeckSharp, because the original HidSharp does not have a strong name we sign it here and ship it with StreamDeckSharp. This is required so we can ship new versions of StreamDeckSharp that reference an already existing version of OpenStreamDeck.SDK

## [2.2.0] - 2020-08-24
- Improve accuracy of the reported gaps between buttons (is used to draw "fullscreen" for example)

## [2.1.0] - 2020-08-22
- Added `StreamDeck.CreateDeviceListener()` that allows to keep track of stream decks between reconnects and provides events to react to connection changes.

## [2.0.0] - 2020-06-05
- Drop .NET 4 support. In theory one could still port back the code to .NET 4 but it's not worth it to still maintain that. netstandard20 is the only target from now on.

## [1.1.0] - 2020-03-14
- A lot of code cleanup (+ static analyzers to enforce rules)
- Added locks around hidstream access to fix some glitches
- Switch to semantic(-ish) versioning
- Fix alignment issue with keys that weren't 72px but 96px

## [0.3.5] - 2019-11-28
### Changed
  - Prepare for netstandard2 release
  - Switch to HidSharp (because of netstandard support)
  - Change the stream deck Xl jpg encoder (the old one was from WPF, which is not supported on netstandard)
  - Switch to new csproj format and embedded nuspec
  - Remove old build stuff (switched to nuke.build)
  - New icon for the nuget package

### Added
  - Support for Stream Deck Rev2 :tada: (Thanks to patrick-dmxc for reporting that)

## [0.3.4] - 2019-06-22
### Added
  - Stream Deck XL now correctly
      - reports firmware version
      - reports serial number
      - shows the logo
      - sets the brightness

### Changed
  - It's now possible to open devices without caching (not recommended)
  - Lower key cooldown for Mini and XL variant, because they don't suffer
    from images glitches like the classic stream deck. This improves the
    framerate for cached handles (try the video example ^^)
  - `IHardware` now has a `DeviceName` property
  - Cleaned up a lot of things

## [0.3.2] - 2019-06-19
### Added
  - Support for Stream Deck XL :tada:

## [0.3.1] - 2019-01-26
### Added
  - Implement `GridKeyPositionCollection` to make dealing with keyboard layouts simpler
  - New `IStreamDeckBoard` for easier access to `GridKeyPositionCollection` Keys
    (Use pattern matching on `IMacroBoard` to get `GridKeyPositionCollection`)
  - Methods to get serialnumber and firmware version from `IStreamDeckBoard`

### Changed
  - Change nuget package to license expression

## [0.2.0] - 2018-08-25 - *OpenMacroBoard* :tada:
Elgato Systems released the *"Stream Deck Mini"* a few weeks ago so I decided to refactor
`StreamDeckSharp` and use a more generic approach that makes it possible to implement
many different macro boards (with LCD buttons) - even ones with buttons that are not placed in a grid layout (like keyboards).

Because of this changes I decided to split `StreamDeckSharp` into a generic part called `OpenMacroBoard.SDK`
and the StreamDeck specific part still called `StreamDeckSharp` and to change the organization name from `OpenStreamDeck` to `OpenMacroBoard` to reflect the generic nature.

### Added
  - `OpenMacroBoard.VirtualBoard` to allow developing software for macro boards (eg. Stream Deck) without real hardware.
  - A bunch of unit tests
  - `IKeyBitmapDataAccess` to retrieve raw RGB data from KeyBitmaps
  - `IKeyPositionCollection` to allow for complex key layouts.
  - `DrawFullScreenExtension` is now part of the library (instead of just an example project)
  - More unit tests
  - Support for different LCD key resolutions
### Changed
  - Interface name `IStreamDeck` to `IMacroBoard`
  - KeyId order for the stream deck changed to be more intuitive
    _(left-to-right and top-to-bottom)_

## [0.1.10] - 2018-04-30
### Added
  - StreamDeck.EnumerateDevices to discover all connected StreamDeck devices.

## [0.1.9] - 2018-02-09
### Added
  - KeyBitmaps now overrides `Equals`, `GetHashCode`, `==` and `!=` and implements `IEquatable<KeyBitmap>`
### Changed
  - Renamed many classes  
    _(remove "StreamDeck"-prefix, thats what namespaces are for)_
  - Moved examples to separate repository
### Fixed
  - Image glitches  
    _Use the same buffer for hid reports and added a cooldown of 75ms for each key,
	this should prevent reuse of memory before async (overlapped) write is finished_
	
## [0.1.8] - 2018-01-29
### Added
  - Example _"DrawFullScreen"_  
    _(Shows how to draw a single image that spans over all 15 keys)_  
    You should check out the video demo in the wiki ;-)
### Fixed
  - Button Images created with `FromRawBitmap` are now correctly flipped
  
  
## [0.1.7] - 2018-01-29
### Changed
  - HidLibrary optimizations (thanks to Sam Messina)
    - Don't copy write buffer.
    - Remove EventMonitor
    - Add DeviceChangeNotifier _(no more polling)_
  - Number of write threads changed to one.
  
## [0.1.6] - 2018-01-27
### Changed
  - Target Framework now .Net 4.0  
    _(XP should in theory be supported now ^^)_
  - Remove async/await methods in HidLibrary to support 4.0
  
## [0.1.4] - 2018-01-27
### Changed
  - `IStreamDeck` no longer supports raw bitmap as argument.  
    _(Use `StreamDeckKeyBitmap.FromRawBitmap`) instead_
  - Factory methods for GDI and WPF stuff moved to `StreamDeckKeyBitmap`
  - Update _"Drawing"_ example to reflect that.
  
## [0.1.3] - 2018-01-27
### Added
  - New extension method for `IStreamDeck` to apply raw bitmap to buttons
  - HidLibrary forked and included as a submodule
### Changed
  - Changed IStreamDeck to support dis- and reconnects.  
    _Backend doesn't support it now, but the interface is ready :-)_
  - Refactoring HID communication to make `StreamDeckHID` simpler.
### Fixed
  - Exception in Dispose  
    _ShowLogo tested if object was disposed after it was marked as such_
  - Timeout for HID write operations, because they keep blocking forever after disconnecting (and reconnecting) the device.
  
## [0.1.2] - 2018-01-25
### Added
  - `IStreamDeck.ShowLogo()` to show the default stream deck logo
  - Extention methods for `IStreamDeck` to create button images from `System.Drawing` and `System.Windows` (WPF) elements.  
    _This should make it easier to create button images programmatically (e.g. adding text to a button)_
  - Example _"Drawing"_

## [0.1.1] - 2017-06-29
  - First alpha release
  - HID communication based on [HidLibrary](https://github.com/mikeobrien/HidLibrary)
  - Examples _"Austria"_ and _"Rainbow"_
  
