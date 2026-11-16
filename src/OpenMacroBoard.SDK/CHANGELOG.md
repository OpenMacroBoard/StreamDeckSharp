# Changelog OpenMacroBoard.SDK
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

## [Unreleased]
### Added
  - `IJpegEncoder`: interface for JPEG encoders, so device providers can let users bring their own encoder.
  - `OmbJpegEncoder`: built-in baseline JPEG encoder without unsafe code or native libraries.
  - `KeyBitmap` extension methods `Resize`, `Rotate90`, `Rotate180`, `Rotate270`, `FlipHorizontal` and `FlipVertical`.
  - `ImageDecodingException`: the one exception the built-in decoders throw for unsupported, damaged or too large images.
### Changed
  - `KeyBitmap.Create.FromFile` and `FromStream` (and their async variants) decode BMP and PNG files with built-in decoders instead of ImageSharp. All PNG color types, bit depths and interlacing are supported; animation, color profiles and gamma are ignored. Compressed BMP files are not supported. Other formats still go through ImageSharp for now.
  - `ButtonPressEffectAdapter` scales the pressed key with the SDK's own code instead of ImageSharp. The result looks the same, but the pixels at edges can differ slightly.

## [0.4.0] - 2019-06-28
### Changed
  - Prepare for netstandard/netcore release
  - Removed `FromWpfElement` (because it's not available in netstandard2)
  - Switched to new csproj format (Microsoft.Net.Sdk)
  - Removed nuspec (covered by new project format)

## [0.3.0] - 2019-01-23
### Added
  - Add `GridKeyPositionCollection` (to simplify KeyCollections for rectangular layouts)
### Fixed
  - Update nuget [depricated licenceUrl](https://docs.microsoft.com/en-us/nuget/consume-packages/finding-and-choosing-packages#license-url-deprecation) tag

## [0.2.1] - 2018-08-25
### Fixed
  - `KeyBitmap.Create.FromWpfElement` corrupted the image (and sometimes crashed) during convertion from `Pbgra32` to `Bgr24`

## [0.2.0] - 2018-08-25 - *OpenMacroBoard* :tada:
Check the Readme.md from the `StreamDeckSharp` Repo.
