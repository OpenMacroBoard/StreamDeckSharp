@echo off

t4 main-website.tt -o ../website/README.md
t4 main-organisation.tt -o ../github/profile/README.md
t4 main-omb-sdk.tt -o ../src/OpenMacroBoard.SDK/README.md
t4 main-sds-rm.tt -o ../src/StreamDeckSharp/README.md
