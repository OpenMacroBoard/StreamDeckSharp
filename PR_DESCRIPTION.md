## Pull Request: Add Stream Deck Module 6 Support

### Summary
Adds support for the **Stream Deck Module 6** (USB PID `0x00B8`) to StreamDeckSharp.

### Device Specifications
- **Model**: Stream Deck Module 6
- **USB**: VID `0x0FD9`, PID `0x00B8` (Elgato)
- **Layout**: 3 columns  2 rows (6 buttons total)
- **Key Size**: 9696 pixels (vs Mini's 8080)
- **Protocol**: Same HID protocol as Stream Deck Mini

### Changes Made
1. Added `StreamDeckModule6` hardware definition to `Hardware.cs`
2. Registered USB PID `0x00B8` 
3. Configured `GridKeyLayout(3, 2, 96, 14)` (3 cols, 2 rows, 96px keys, 14px spacing)
4. Uses `HidComDriverStreamDeckMini(96)` driver
5. Added public property `StreamDeckModule6` for device access

### Testing
 **I own this device** and can test once a build is available.

Device detected by Windows but not recognized by StreamDeckSharp 6.1.0:
```
Status       : OK
FriendlyName : USB Input Device
InstanceId   : USB\VID_0FD9&PID_00B8\AB3LA5161J5FUJ
```

### Code Pattern
Follows the existing pattern used for Stream Deck Mini:
```csharp
StreamDeckMini =
    RegisterNewHardwareInternal(
        "Stream Deck Mini",
        new GridKeyLayout(3, 2, 80, 32),
        new HidComDriverStreamDeckMini(80),
        ElgatoUsbId(0x0063),
        ElgatoUsbId(0x0090)
    );

// Module 6 follows same pattern with 96px keys:
StreamDeckModule6 =
    RegisterNewHardwareInternal(
        "Stream Deck Module 6",
        new GridKeyLayout(3, 2, 96, 14),
        new HidComDriverStreamDeckMini(96),
        ElgatoUsbId(0x00B8)
    );
```

### Request
Could you create a pre-release NuGet package so I can test with my physical device? Happy to provide feedback and screenshots.

### References
- Device is relatively new (released 2024/2025)
- Uses same button layout as Mini but larger LCD keys
- Part of Elgato's modular Stream Deck lineup

---
**Ready to merge once testing confirms functionality!**
