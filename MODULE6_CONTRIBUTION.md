# Stream Deck Module 6 Support - Contribution Guide

## What We Added:
 Stream Deck Module 6 hardware definition to Hardware.cs
 USB Product ID: 0x00B8 (Elgato VID: 0x0FD9)
 Layout: 3 columns  2 rows (6 buttons)
 Key Size: 9696 pixels (vs Mini's 8080)
 Driver: HidComDriverStreamDeckMini(96)

## Files Changed:
- src/StreamDeckSharp/Hardware.cs

## Next Steps to Contribute:

### 1. Fork the Repository
   - Go to: https://github.com/OpenMacroBoard/StreamDeckSharp
   - Click 'Fork' button
   - Clone YOUR fork: git clone https://github.com/YOUR_USERNAME/StreamDeckSharp.git

### 2. Apply Our Changes
   Our commit: 4fb0c12 on branch 'add-streamdeck-module6-support'
   
   To transfer to your fork:
   cd /path/to/your/fork
   git remote add upstream https://github.com/OpenMacroBoard/StreamDeckSharp.git
   git remote add contribution C:/Users/djtam/OneDrive/Documents/Coding/Project/Millionaire/StreamDeckSharp
   git fetch contribution
   git cherry-pick 4fb0c12

### 3. Test (if you have the device)
   - Build the library
   - Run with your Module 6
   - Verify all 6 buttons work
   - Test image display (9696)

### 4. Create Pull Request
   - Push to your fork: git push origin add-streamdeck-module6-support
   - Go to: https://github.com/YOUR_USERNAME/StreamDeckSharp
   - Click 'Compare & pull request'
   - Title: 'Add Stream Deck Module 6 support (USB PID 0x00B8)'
   - Description:
     * Adds support for Stream Deck Module 6
     * 6 buttons, 32 layout, 9696px keys
     * USB PID: 0x00B8
     * Uses same HID protocol as Mini
     * Tested with physical device 

### 5. Alternative: Create Issue First
   If you're unsure, create an issue first:
   - Go to: https://github.com/OpenMacroBoard/StreamDeckSharp/issues
   - Title: 'Feature Request: Stream Deck Module 6 support'
   - Mention you have the device and can test
   - Attach our patch/changes

## For Testing in Your Millionaire Game:

Once you have the modified library built, you can:

1. Build StreamDeckSharp with Module 6 support
2. Copy the DLL to your game's bin folder
3. Or reference the local project instead of NuGet

Let me know if you want help with any of these steps!
