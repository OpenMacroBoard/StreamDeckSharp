# Device Details

## Stream Deck Rev1
### Teardown

- https://www.youtube.com/watch?v=_ha2LdSAvXU (German, with Hardware Details)
- https://www.youtube.com/watch?v=9apO--Qpz58 (English, 30min uncut)
- https://www.youtube.com/watch?v=rOQu9_t2zOY (No commentary, just music)
- https://imgur.com/gallery/XyKiL

### Hardware
Accoring to description of the German tear down video:

|                           |                                     |
| ------------------------- | ----------------------------------- |
| Display                   | LD043H10-40NC-K3 4,3" 480x272 Pixel |
| Processor                 | 400MHZ ATSAM9G45                    |
| SDRAM                     | 512MBit winbond W9751G6KB5I         |
| Flash                     | 1GBit winbond W29N01GVSIAA          |
| 3,3V LDO                  | TLV1117-33                          |
| Adjustable LDO            | TJ4203GSF5-ADJ (2x)                 |
| Stepper for LED Backlight | AME5142                             |

## Stream Deck Mini
No details atm.

## Stream Deck XL

### Button Auto-Press Issue
My (and according to a quick google search also some other customers) stream deck xl started to automatically
trigger random buttons and sometimes event kept some buttons pressed for quite a while.

It looks like the stream deck xl has a design issue where some buttons are randomly pressed.
For my device (and accoring to some other customer videos) the following buttons seem to be affected:

🔲🔲🔲❌🔲🔲🔲🔲
🔲🔲🔲❌🔲🔲❌❌
🔲🔲🔲❌🔲🔲🔲🔲
🔲🔲🔲❌🔲🔲🔲🔲

- More details about the issue:
    - Reddit
        - https://www.reddit.com/r/ElgatoGaming/comments/cb6lf2/streamdeck_xl_a_button_keeps_pressing_itself/
        - https://www.reddit.com/r/ElgatoGaming/comments/a3duq0/pressing_stream_deck_buttons_will_push_random/
        - https://www.reddit.com/r/ElgatoGaming/comments/a3ce30/bug_stream_deck_buttons_randomly_press_themselves/
    - Amazon
        - https://www.amazon.com/-/de/gp/customer-reviews/R3MUHRTR603RM2/ref=cm_cr_arp_d_viewpnt?ie=UTF8&ASIN=B07RL8H55Z#R3MUHRTR603RM2
    - YouTube
        - https://www.youtube.com/watch?v=e3pW7UE9Ngw
        - https://www.youtube.com/watch?v=oPw5ESehcUM

I wrote a quick diagnostics tool OpenMacroBoard.Examples.ButtonPressDiagnostics