# Changelog OpenMacroBoard.SDK
All notable changes to this project will be documented in this file.
I'm trying to keep it up to date, but I'm a lazy bastard - when in doubt - check out the commit log ;-)

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
