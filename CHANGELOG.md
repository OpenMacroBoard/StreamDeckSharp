# Changelog
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

## [Unreleased]

## [0.2.0] - 2018-08-25 - *OpenMacroBoard* :tada:
Elgato Systems released the *"Steam Deck Mini"* a few weeks ago so I decided to refactor
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
  
