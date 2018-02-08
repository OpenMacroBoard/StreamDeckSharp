@echo off
ILRepack.exe /out:"..\src\StreamDeckSharp\bin\Release\Merged\StreamDeckSharp.dll" /xmldocs /internalize "..\src\StreamDeckSharp\bin\Release\StreamDeckSharp.dll" "..\src\StreamDeckSharp\bin\Release\HidLibrary.dll"
