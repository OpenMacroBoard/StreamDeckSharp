@echo off
pushd
cd /D "%~dp0"

REM Cleanup target directory
del "..\src\StreamDeckSharp\bin\Release\Merged\*.dll"
del "..\src\StreamDeckSharp\bin\Release\Merged\*.xml"
del "..\src\StreamDeckSharp\bin\Release\Merged\*.pdb"
del "..\src\StreamDeckSharp\bin\Release\Merged\*.nupkg"

REM Merge hidlib and streamdecksharp
ILRepack.exe /out:"..\src\StreamDeckSharp\bin\Release\Merged\StreamDeckSharp.dll" /xmldocs /internalize "..\src\StreamDeckSharp\bin\Release\StreamDeckSharp.dll" "..\src\StreamDeckSharp\bin\Release\HidLibrary.dll"

REM Create nuget package
nuget.exe pack "..\src\StreamDeckSharp\StreamDeckSharp.nuspec" -OutputDirectory "..\src\StreamDeckSharp\bin\Release\Merged"

popd

exit /b 0
