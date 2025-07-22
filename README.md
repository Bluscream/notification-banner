# notification-banner

**Authors:** Bluscream, Belphemur, Cursor AI

C# app to send custom Windows notifications, useful for alternate shells like [CairoDesktop](https://github.com/cairoshell/cairoshell) or automation scripts.

> [!NOTE]
> This project is a complete rework of [Bluscream/notify-toast](https://github.com/Bluscream/notify-toast).

<center>
  
![banner.exe --message "This is a basic notification for screenshot purposes." --title "Screenshot Test" --time 15](https://files.catbox.moe/6rp9tq.png)

</center>

## Usage

You can run the app with either positional arguments or named arguments:

### Positional Arguments
```
banner.exe "Your message here" "Optional title here"
```
- If you provide one argument, it is used as the message.
- If you provide two arguments, they are used as message and title.

### Named Arguments
```
banner.exe --message "Your message" --title "Optional title" --image "image_path_or_base64" --position "topleft" --time 10 --sound "sound.wav" --color "#FF0000" --size 100 --primary --exit
```
You can use any of the following prefixes for each argument: `--`, `-`, or `/` (e.g., `--message`, `-message`, `/message`).

#### Arguments
- `--message` (required if not using positional): The notification message
- `--title`: The notification title
- `--image`: Image path, url or base64 string
- `--position`: Banner position as a string (e.g., `topleft`, `topright`, `bottomleft`, `bottomright`, `topcenter`, `bottomcenter`, `center`)
- `--time`: Time to display notification (seconds)
- `--sound`: Path to WAV file or URL to play when notification is shown (defaults to Windows Notify System Generic.wav)
- `--color`: Background color in hex format (e.g., `#FF0000` for red, `#80FF0000` for semi-transparent red)
- `--size`: Size scaling factor (100 = default size, 150 = 150% larger, 50 = 50% smaller)
- `--primary`: Force banner to always appear on the primary screen (default: uses screen with cursor)
- `--exit`: Exit the application after showing the notification

### Example
```
banner.exe "Hello world!" "My Title"
banner.exe --message "Hello world!" --position "bottomright" --time 5
banner.exe --message "Alert!" --title "System Alert" --sound "C:\sounds\alert.wav"
banner.exe --message "Download complete" --sound "https://example.com/notification.wav"
banner.exe --message "Silent notification" --sound ""  # Disable sound
banner.exe --message "Red notification" --color "#FF0000"
banner.exe --message "Semi-transparent blue" --color "#80FF0000"
banner.exe --message "Large notification" --size 150
banner.exe --message "Small notification" --size 75
banner.exe --message "Primary screen notification" --primary
banner.exe --message "Exit after notification" --exit
```

---
