# Changelog OpenMacroBoard.SDK
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

## [Unreleased]
The SDK no longer depends on ImageSharp. See [Migrating from the ImageSharp based API](../../doc/migrating-from-imagesharp.md).

### Added
  - New package `OpenMacroBoard.SDK.KeyBitmap.SkiaSharp`: `KeyBitmap.Create.FromSkiaCanvas` (draw text and shapes on a SkiaSharp canvas) and `KeyBitmap.Create.FromSkiaBitmap`. It is the counterpart of the GDI package for Windows, Linux and macOS. The examples draw with it now and no longer use ImageSharp.
  - `IJpegEncoder`: interface for JPEG encoders, so device providers can let users bring their own encoder.
  - `OmbJpegEncoder`: built-in baseline JPEG encoder without unsafe code or native libraries.
  - `IImageDecoder`: interface for image decoders, and overloads of `KeyBitmap.Create.FromFile` and `FromStream` (and their async variants) that take a decoder, to load formats the built-in decoder does not support.
  - `OmbImageDecoder`: built-in decoder for BMP, PNG and JPEG files without unsafe code.
  - `ImageDecodingException`: the one exception the built-in decoder throws for unsupported, damaged or too large images.
  - `KeyBitmap` extension methods `Resize`, `Rotate90`, `Rotate180`, `Rotate270`, `FlipHorizontal` and `FlipVertical`.
  - `OmbResizeMode` (`BoxPad`, `Pad`, `Crop`, `Stretch`) and a `Resize` overload that takes it.

### Changed
  - **Breaking:** `KeyBitmap.Create.FromFile` and `FromStream` (and their async variants) decode images with the built-in decoder instead of ImageSharp. Supported are BMP (uncompressed), PNG (all color types and bit depths, interlaced or not) and JPEG (baseline and progressive, grayscale and color). Other formats that ImageSharp could read (GIF, WebP, TIFF, ...) and CMYK JPEG files are rejected with `ImageDecodingException`. JPEG files are turned the way their EXIF orientation says (photos from phones and cameras load upright, like in image viewers); ImageSharp did not do that. Animation, color profiles and gamma are ignored. Images with more than 50 million pixels are rejected. To load other formats, pass your own `IImageDecoder`.
  - **Breaking:** `DrawFullScreenBitmap` takes a `KeyBitmap` and an `OmbResizeMode` instead of an ImageSharp image and ImageSharp's `ResizeMode`. The image is scaled with the SDK's own code, so it looks slightly softer than before.
  - `ButtonPressEffectAdapter` scales the pressed key with the SDK's own code instead of ImageSharp. The result looks the same, but the pixels at edges can differ slightly.
  - **Breaking:** a `KeyBitmap` always contains pixels now. Before, a bitmap could be "empty" (no pixel data, shown as black), and code that read the pixels had to check for that first. `KeyBitmap.Black` is a real black bitmap of one pixel now. See "Removed" for the two members that went away with this.
  - **Breaking** (silent, the code still compiles): `KeyBitmap` no longer compares by content. `==`, `!=`, `Equals` and `GetHashCode` now tell whether two variables refer to the same object; before they compared the size and every pixel. `KeyBitmap` also no longer implements `IEquatable<KeyBitmap>`. Code that skips an update "if the image did not change" still works, but no longer skips for two separately created, identical images. To compare the content, compare the pixels: `((IKeyBitmapDataAccess)a).GetData().Span.SequenceEqual(((IKeyBitmapDataAccess)b).GetData().Span)` (and the width and height).
  - **Breaking** (only for device providers): `IKeyBitmapDataAccess.GetData()` returns `ReadOnlyMemory<byte>` instead of `ReadOnlySpan<byte>`, so the pixels can be stored and used in asynchronous code. To migrate, use `GetData().Span`.

  - VirtualBoard is a web application now and runs on Windows, Linux and macOS. It shows the board on a page in the browser (default `http://127.0.0.1:28621/`) instead of a window. Applications reach it through SocketIO as before, and the CLI parameters are the same, plus `--web-port` and `--open-browser`. There is no Windows executable to download any more; start it with `dotnet run --project src/OpenMacroBoard.VirtualBoard`.

### Fixed
  - `KeyBitmap.Create.FromBgra32Array` and `FromRgba32Array` created bitmaps with a pixel buffer that was a third too large (four bytes per pixel instead of three).
  - SocketIO: on Linux and macOS, applications did not find boards that are hosted on a loopback address (the default, `127.0.0.1`), because these systems do not deliver the discovery broadcast to a socket bound to that address. The host now answers discovery requests there too, but only requests that come from the same machine. The board itself is still reachable only on the address it is bound to.
  - SocketIO: disposing a `SocketIOMacroBoardHost` or a `SocketIOBoardListener` could end the whole process with an unhandled `SocketException` ("Operation canceled") from the discovery socket. Socket errors there are handled now.

### Removed
  - **Breaking:** `KeyBitmap.Create.Empty(width, height)`. Use `KeyBitmap.Black`, it is scaled to the size of the key.
  - **Breaking** (only for device providers): `IKeyBitmapDataAccess.IsEmpty`. Bitmaps are never empty any more, so the check can be deleted.
  - **Breaking:** `KeyBitmap.Create.FromImageSharpImage`, `ToImage`, `ImageSharpExtensions` and `ConditionalDisposable<T>`. The migration page shows the few lines that replace them.

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
