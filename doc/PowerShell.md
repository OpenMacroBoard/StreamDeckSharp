You can use this library in Windows PowerShell.

Example...

add-type -path 'WindowsPowerShell\StreamDeckSharp.dll'

$deckInterface = [StreamDeckSharp.StreamDeck]::FromHID()

$POSH = [StreamDeckSharp.StreamDeckKeyBitmap]::FromFile(".\Pictures\PowerShell72x72.png")

$deckInterface.SetKeyBitmap(0, $POSH.CloneBitmapData())

$KeyEvent = Register-ObjectEvent -InputObject $deckInterface -EventName KeyPressed -SourceIdentifier buttonPress -Action {write-host $eventArgs.key)}
