# Changelog
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

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
  
