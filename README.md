# notification-banner

**Authors:** Bluscream, Belphemur, Cursor AI

C# app to send custom Windows notifications, useful for alternate shells like [CairoDesktop](https://github.com/cairoshell/cairoshell) or automation scripts. Features Do Not Disturb mode detection and important notification override.

> [!NOTE]
> This project is a complete rework of [Bluscream/notify-toast](https://github.com/Bluscream/notify-toast).

<center>
  
![banner.exe --message "This is a basic notification for screenshot purposes." --title "Screenshot Test" --time 15](https://files.catbox.moe/6rp9tq.png)

</center>

## Features

- **Custom Windows Notifications**: Display notifications with custom messages, titles, and styling
- **Multiple Positions**: 7 different screen positions (top-left, top-center, top-right, bottom-left, bottom-center, bottom-right, center)
- **Image Support**: Local files, URLs, or base64 encoded images
- **Sound Support**: Local WAV files or URLs
- **Color Customization**: Background colors with transparency support
- **Size Scaling**: Adjustable notification size (50% to 200%)
- **Do Not Disturb Detection**: Automatically respects Windows Focus Assist (Do Not Disturb) mode
- **Important Notifications**: Override Do Not Disturb mode with `--important` flag
- **Queue System**: Handles multiple notifications with intelligent queuing
- **Configuration Files**: JSON-based configuration for defaults and automatic image selection
- **Single Instance**: Prevents multiple instances, pipes notifications to existing instance

## Quick Start

### Basic Usage
```bash
# Simple notification
banner.exe "Hello world!" "My Title"

# With named arguments
banner.exe --message "Hello world!" --position "bottomright" --time 5
```

### Positional Arguments
```
banner.exe "Your message here" "Optional title here"
```
- If you provide one argument, it is used as the message.
- If you provide two arguments, they are used as message and title.

### Named Arguments
```
banner.exe --message "Your message" --title "Optional title" --image "image_path_or_base64" --position "topleft" --time 10 --sound "sound.wav" --color "#FF0000" --size 100 --primary --important --exit
```
You can use any of the following prefixes for each argument: `--`, `-`, or `/` (e.g., `--message`, `-message`, `/message`).

## Examples

### Basic Usage
```bash
banner.exe "Hello world!" "My Title"
banner.exe --message "Hello world!" --position "bottomright" --time 5
```

### Sound Examples
```bash
banner.exe --message "Alert!" --title "System Alert" --sound "C:\sounds\alert.wav"
banner.exe --message "Download complete" --sound "https://example.com/notification.wav"
banner.exe --message "Silent notification" --sound ""  # Disable sound
```

### Visual Customization
```bash
banner.exe --message "Red notification" --color "#FF0000"
banner.exe --message "Semi-transparent blue" --color "#80FF0000"
banner.exe --message "Large notification" --size 150
banner.exe --message "Small notification" --size 75
```

### Screen and Behavior
```bash
banner.exe --message "Primary screen notification" --primary
banner.exe --message "Exit after notification" --exit
```

### Do Not Disturb Examples
```bash
# Regular notification (respects Do Not Disturb)
banner.exe --message "Regular update" --title "System Update"

# Important notification (bypasses Do Not Disturb)
banner.exe --message "Critical security alert!" --title "Security Alert" --important
```

## Configuration Reference

| Parameter | Description | Default | Command Line | JSON Config | HTTP Query | IPC JSON |
|-----------|-------------|---------|--------------|-------------|------------|----------|
| `message` | The notification message | `""` | `--message "text"` | `"Message": "text"` | `?message=text` | `"Message": "text"` |
| `title` | The notification title | `"Notification"` | `--title "text"` | `"Title": "text"` | `?title=text` | `"Title": "text"` |
| `image` | Image path, URL or base64 string | `""` | `--image "path/url"` | `"Image": "path/url"` | `?image=path/url` | `"Image": "path/url"` |
| `position` | Banner position on screen | `"topleft"` | `--position "pos"` | `"Position": "pos"` | `?position=pos` | `"Position": "pos"` |
| `time` | Display time in seconds | `"10"` | `--time "15"` | `"Time": "15"` | `?time=15` | `"Time": "15"` |
| `sound` | Path to WAV file or URL | `"C:\Windows\Media\Windows Notify System Generic.wav"` | `--sound "path"` | `"Sound": "path"` | `?sound=path` | `"Sound": "path"` |
| `color` | Background color (hex) | `""` | `--color "#FF0000"` | `"Color": "#FF0000"` | `?color=%23FF0000` | `"Color": "#FF0000"` |
| `size` | Size scaling factor (50-200) | `"100"` | `--size "150"` | `"Size": "150"` | `?size=150` | `"Size": "150"` |
| `primary` | Force primary screen | `false` | `--primary` | `"Primary": true` | `?primary=true` | `"Primary": true` |
| `important` | Bypass Do Not Disturb | `false` | `--important` | `"Important": true` | `?important=true` | `"Important": true` |
| `exit` | Exit after notification | `false` | `--exit` | `"Exit": true` | `?exit=true` | `"Exit": true` |
| `api-listen-port` | HTTP server port | `14969` | `--api-listen-port 8080` | `"ApiListenPort": 8080` | ❌ | ❌ |
| `log-file` | Path to log file | `""` | `--log-file "path"` | `"LogFile": "path"` | ❌ | ❌ |
| `console` | Show console window | `false` | `--console` | `"Console": true` | ❌ | ❌ |
| `create-default-config` | Create default config files | `false` | `--create-default-config` | `"CreateDefaultConfig": true` | ❌ | ❌ |
| `default-images` | Regex patterns for auto-images | `{"HASS\\.Agent": "..."}` | ❌ | `"DefaultImages": {...}` | ❌ | ❌ |

**Position Values:** `topleft`, `topright`, `bottomleft`, `bottomright`, `topcenter`, `bottomcenter`, `center`

## Advanced Features

### Do Not Disturb Mode

The application automatically detects and respects Windows Focus Assist (Do Not Disturb) mode:

- **Automatic Detection**: Uses Windows API to detect Focus Assist state
- **Registry Fallback**: Falls back to registry check if API fails
- **Important Override**: Use `--important` flag to show notifications even when Do Not Disturb is active
- **Silent Skipping**: Regular notifications are silently skipped when Do Not Disturb is active

### Default Images

The application supports automatic image selection based on the notification title using regex patterns. If no `--image` argument is provided via command line, the system will check if the title matches any patterns defined in the `DefaultImages` configuration.

#### How it works
1. When a notification is created without an explicit `--image` parameter
2. The system checks the notification title against regex patterns in the `DefaultImages` dictionary
3. If a match is found, the corresponding image URL/path is automatically set
4. The first matching pattern takes precedence

#### Configuration
The application supports JSON configuration files for persistent settings. Configuration files can be placed at:

- **Global**: `%ProgramData%\notification-banner.json` (requires admin privileges)
- **Program**: `notification-banner.json` (in the same directory as the executable)
- **User**: `%USERPROFILE%\notification-banner.json`

User-level configuration overrides program-level settings. Everything else overrides both.

The configuration uses JSON format with the following structure:

```json
{
  "Message": "",
  "Title": "Notification",
  "Time": "10",
  "Position": "topleft",
  "Exit": false,
  "Color": "",
  "Sound": "C:\\Windows\\Media\\Windows Notify System Generic.wav",
  "Size": "100",
  "Primary": false,
  "Important": false,
  "ApiListenPort": 14969,
  "LogFile": "C:\\Users\\username\\AppData\\Local\\Temp\\banner.log",
  "Console": false,
  "CreateDefaultConfig": false,
  "DefaultImages": {
    "HASS\\.Agent": "https://www.hass-agent.io/2.1/assets/images/logo/logo-256.png",
    "error|fail|critical": "C:/Images/error.png",
    "success|ok|done": "C:/Images/success.png",
    "info|notice": "C:/Images/info.png"
  }
}
```

**Configuration Priority:**
1. Command line arguments (highest priority)
2. Global configuration file
3. Program configuration file  
4. User configuration file
5. Default values (lowest priority)

**Creating Default Configuration Files:**
```bash
# Create default config files in all locations
banner.exe --create-default-config
```

#### Default Images Example
```
banner.exe "Agent is running" "HASS.Agent"  # Will automatically use the HASS.Agent logo
banner.exe --message "Operation failed" --title "Error occurred"  # Will use error.png if configured
banner.exe --message "Task completed" --title "Success"  # Will use success.png if configured
```

**Note:** The `--image` command line argument always takes precedence over automatic image selection.

## Inter-Process Communication (IPC)

The application supports two types of IPC mechanisms for sending notifications to an already running instance.

### Named Pipe IPC

**C# Example:**
```csharp
using System.IO.Pipes;
using System.Text.Json;

// Create notification config
var config = new {
    Message = "Hello from C#!",
    Title = "C# IPC Test",
    Time = "10",
    Position = "bottomright"
};

// Send via named pipe
using var client = new NamedPipeClientStream(".", "notification-banner-pipe", PipeDirection.Out);
client.Connect(2000);
using var writer = new StreamWriter(client) { AutoFlush = true };
writer.WriteLine(System.Diagnostics.Process.GetCurrentProcess().Id);
writer.Write(JsonSerializer.Serialize(config));
```

### HTTP API

The application can also run as an HTTP server, allowing remote applications to send notifications via HTTP requests.

**Start the HTTP server:**
```bash
# Start the application with HTTP server enabled
banner.exe --api-listen-addresses "localhost:31415,0.0.0.0:8080"
```

**Send notifications via HTTP requests:**
```bash
# Notification with all parameters
curl "http://localhost:31415/?message=Custom%20notification&title=My%20Title&time=15&position=bottomright&color=%23FF0000&size=120&important=true"

# Using PowerShell
Invoke-WebRequest -Uri "http://localhost:31415/?message=PowerShell%20test&title=PS%20Test" -Method GET

# Using Python
import requests
response = requests.get("http://localhost:31415/", params={
    "message": "Python notification",
    "title": "Python Test",
    "time": "10",
    "position": "topleft"
})
```

**Supported HTTP Methods:**
- `GET` - Send notification via query parameters
- `POST` - Send notification via query parameters

**Response Format:**
```json
{
  "success": true,
  "message": "Notification queued successfully",
  "timestamp": "2025-07-30T04:37:08.0883393Z"
}
```

## Logging

The application includes a comprehensive logging system with configurable output destinations.

### Logging Configuration

**Console output enabled:**
```bash
banner.exe --console
# Console visible with logging output
```

**File logging:**
```bash
banner.exe --api-listen-addresses "localhost:31415" --log-file "app.log"
# Logs to file only (console hidden)
```

## Development

### Building

#### Requirements
- .NET 8.0 SDK
- Windows 10.0.19041.0 or later
- Windows Forms support

#### Build Commands
```bash
# Build for development
dotnet build

# Build and run tests
tools\test.cmd /build

# Publish single-file executable
dotnet publish -c Release
```

#### Project Structure
```
notification-banner/
├── Framework/Banner/          # Banner display framework
├── Model/                     # Application models and queue
├── Util/                      # Utility functions
├── Config.cs                  # Configuration handling
├── DefaultIcon.cs             # Default icon definitions
├── Program.cs                 # Application entry point
└── tools/                     # Build and test scripts
```

### Technical Details

- **Framework**: .NET 8.0 Windows Forms
- **Target**: Windows 10.0.19041.0+
- **Architecture**: Single-file executable with self-contained runtime
- **Dependencies**: Minimal - only Windows Forms and .NET runtime
- **API Usage**: Windows user32.dll for Do Not Disturb detection
- **Registry Access**: Fallback method for notification settings

## License

This project is based on the original work by Bluscream and has been significantly enhanced by the community.

---

For issues, feature requests, or contributions, please visit the project repository.
