# notification-banner

**Authors:** Bluscream, Belphemur, Cursor AI

C# app to send custom Windows notifications, useful for alternate shells like [CairoDesktop](https://github.com/cairoshell/cairoshell) or automation scripts.

> [!NOTE]
> This project is a complete rework of [Bluscream/notify-toast](https://github.com/Bluscream/notify-toast).

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

### Default Images
The application supports automatic image selection based on the notification title using regex patterns. If no `--image` argument is provided via command line, the system will check if the title matches any patterns defined in the `DefaultImages` configuration.

#### How it works
1. When a notification is created without an explicit `--image` parameter
2. The system checks the notification title against regex patterns in the `DefaultImages` dictionary
3. If a match is found, the corresponding image URL/path is automatically set
4. The first matching pattern takes precedence

#### Configuration
DefaultImages can be configured in the JSON configuration files (both program-level and user-level). The configuration uses regex patterns as keys and image URLs/paths as values:

```json
{
  "DefaultImages": {
    "HASS\\.Agent": "https://www.hass-agent.io/2.1/assets/images/logo/logo-256.png",
    "error|fail|critical": "C:/Images/error.png",
    "success|ok|done": "C:/Images/success.png",
    "info|notice": "C:/Images/info.png"
  }
}
```

#### Default Images Example
```
banner.exe "Agent is running" "HASS.Agent"  # Will automatically use the HASS.Agent logo
banner.exe --message "Operation failed" --title "Error occurred"  # Will use error.png if configured
banner.exe --message "Task completed" --title "Success"  # Will use success.png if configured
```

**Note:** The `--image` command line argument always takes precedence over automatic image selection.

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
