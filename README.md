# 🛰️ Marauder Studio

> Beautiful, modern Windows 10/11 desktop application for managing and flashing **ESP32 Marauder** devices — built with .NET 9 and Fluent Design.

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/USAChevIP/MarauderStudio)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Latest release](https://img.shields.io/github/v/release/USAChevIP/MarauderStudio)](../../releases/latest)

---

## ✨ Features

- 🛰 **Dashboard** — auto-detect COM port, USB connect at 115200 baud, live device info (firmware version, chip family, MAC addresses, SD card)
- ⚡ **Flasher** with three `.bin` sources:
  - Drag-and-drop or browse local file
  - Direct download from GitHub Releases with progress
  - Built-in profiles for **26 boards** (v4/v6/v6.1/v7/v8/Mini/Mini v3/Pancake/Cardputer/CYD/C5/C6…)
- 📡 **Serial Monitor** — terminal-style console with mono font, command history (↑/↓), `Ctrl+L` to clear, quick buttons for `info` / `scanap` / `reboot`
- 🎮 **Device Control** — preset tiles organized by category: WiFi, Attacks, Sniffers, BT spam, GPS / Wardrive
- ⚙ **Settings** — theme (Dark/Light/System), language (RU/EN), esptool path, WiFi credentials for OTA, download folder
- ℹ **About** — version, license, links to Marauder repo and wiki

## 🖼 Screenshots

> _Screenshots will be added in upcoming releases. Build the app from source to see the Fluent Design UI in action._

## 🎯 Why Marauder Studio?

The existing toolchain for ESP32 Marauder is fragmented:

| Need | Existing options | Marauder Studio |
|---|---|---|
| Flash from .bin | Web flasher (`MarauderInstaller`), raw `esptool` | ✅ One-click with profile auto-detection |
| Web OTA | Browser at `http://192.168.4.1` | ✅ Built-in uploader with WiFi connect helper |
| Serial monitor | PuTTY / Arduino IDE / CoolTerm | ✅ Integrated with the same UI |
| Command presets | None | ✅ Tiles for all Marauder attack types |
| Firmware metadata | None | ✅ Reads `MRDRFWID` signature from `.bin` |
| Beautiful UI | Functional but dated (Tkinter) | ✅ Modern Fluent Design, dark by default |

## 🛠 Tech Stack

- **.NET 9 / WPF** with **[ModernWpfUI](https://github.com/Kinnara/ModernWpf)** (Fluent Design)
- **MVVM** via [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) source generators
- **esptool** integration through `System.Diagnostics.Process` (bundled, user path, or system Python)
- **GitHub API** — native `HttpClient` + `System.Text.Json` (no Octokit dependency)
- **WiFi** via [ManagedNativeWifi](https://github.com/metageek-llc/ManagedNativeWifi) (Windows WLAN API)
- **JSON-based localization** with custom `MarkupExtension` for hot RU/EN switching
- **xUnit** for unit testing (51 tests covering parsing, metadata, board database, progress)

## 📦 Project Structure

```
MarauderStudio/
├── MarauderStudio.sln
├── README.md
├── LICENSE
├── .gitignore
├── src/
│   ├── MarauderStudio/              # WPF UI app
│   │   ├── App.xaml(.cs)            # DI bootstrap, theme, localization
│   │   ├── MainWindow.xaml(.cs)     # NavigationView + Frame
│   │   ├── Views/                   # 6 pages (Dashboard, Flasher, Monitor, Device, Settings, About)
│   │   ├── ViewModels/              # MVVM view-models
│   │   ├── Converters/              # BoolToVis, InverseBool, EnumToInt, etc.
│   │   ├── Localization/            # JSON dictionaries + MarkupExtension
│   │   └── Resources/
│   │       ├── Localization/        # ru.json, en.json
│   │       └── Icons/               # SVG/PNG
│   └── MarauderStudio.Core/         # Pure business logic (no UI deps)
│       ├── Devices/                 # ChipFamily, DeviceInfo, PortEnumerator, ChipDetector
│       ├── Firmware/                # BoardDatabase (26 boards), FirmwareMetadata, GitHubReleasesClient
│       ├── Serial/                  # SerialPortService, MarauderProtocol (info parser)
│       ├── Flashing/                # EsptoolRunner, FlashOrchestrator, WebOtaUploader, WifiConnector
│       └── Common/                  # AppSettings, SettingsService
└── tests/
    └── MarauderStudio.Tests/        # xUnit — 51 tests
```

## 🚀 Quick Start

### Run from source

Requirements: **.NET 9 SDK**, Windows 10 1809+ or Windows 11.

```bash
git clone https://github.com/USAChevIP/MarauderStudio.git
cd MarauderStudio
dotnet test         # 51 tests, all passing
dotnet run --project src/MarauderStudio
```

### Download prebuilt binary

Grab the latest `MarauderStudio.exe` from the [Releases page](../../releases/latest).
The single-file build is fully self-contained (~140 MB) and runs on any Windows 10/11 x64 without installing .NET.

## 📥 Building the `.exe`

### Framework-dependent (small, ~157 KB)

Requires .NET 9 Desktop Runtime on the target machine:

```bash
dotnet publish src/MarauderStudio/MarauderStudio.csproj \
  -c Release -r win-x64 --self-contained false
```

### Self-contained single-file (~140 MB)

Works anywhere on Windows 10/11 x64:

```bash
dotnet publish src/MarauderStudio/MarauderStudio.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Result: `src/MarauderStudio/bin/Release/net9.0-windows/win-x64/publish/MarauderStudio.exe`.

## 🔌 esptool Setup

The app uses `esptool` through three sources (priority order):

1. **Bundled** — `Resources/esptool/esptool.exe` next to `MarauderStudio.exe`
2. **User path** — configured in Settings → "Путь к esptool"
3. **System Python** — `python -m esptool` if `esptool` is installed in the default Python

To bundle `esptool.exe`:

```bash
pip install esptool pyinstaller
pyinstaller --onefile $(which esptool)
# copy dist/esptool.exe → src/MarauderStudio/Resources/esptool/esptool.exe
```

## 🎮 Usage

1. **Plug in** your ESP32 Marauder via USB. The COM port appears in the Dashboard dropdown within 2 seconds.
2. Click **Connect** — the app reads `info` and displays firmware version, hardware, chip, MACs, and SD card status.
3. Open the **Flasher** tab:
   - Drag a `.bin` file OR pick a profile OR download from GitHub
   - Configure method (USB / Web OTA / SD), mode (app-only / full), erase (none / app / all)
   - Click **FLASH**
4. Switch to **Monitor** to send commands (`info`, `scanap`, etc.) and watch live output.
5. Open **Device** for one-click preset tiles for common Marauder attacks.

## 🔬 Marauder Protocol Reference

All protocol details were reverse-engineered from the official [justcallmekoko/ESP32Marauder](https://github.com/justcallmekoko/ESP32Marauder) repository (ESP-IDF source in `esp32_marauder/`, OTA loader in `MarauderOTA/`).

| Action | Protocol | Command / endpoint |
|---|---|---|
| Get device info | Serial 115200 | `\ninfo\n` (parses `Version:`, `Hardware:`, `Station MAC:`, `SD Card:` etc.) |
| Detect chip | esptool | `esptool.py --chip auto chip_id` |
| Detect chip from `.bin` | file | Search for `MRDRFWID` 80-byte metadata block |
| Web OTA | HTTP | `POST http://192.168.4.1/update` multipart (field `firmware`), response `OK`/`FAIL` |
| SD Update | SD-card | Copy `update.bin` to root, run `update -s` over Serial |
| esptool full flash | Serial | `write_flash -z 0x1000 bootloader 0x8000 partitions 0xE000 ota_data 0x10000 app` |

See the source for the full protocol parser:
- `src/MarauderStudio.Core/Serial/MarauderProtocol.cs` — `info` regex parser
- `src/MarauderStudio.Core/Firmware/FirmwareMetadata.cs` — `MRDRFWID` reader
- `src/MarauderStudio.Core/Flashing/EsptoolRunner.cs` — esptool wrapper with progress
- `src/MarauderStudio.Core/Flashing/WebOtaUploader.cs` — Web OTA multipart uploader

## 🤝 Contributing

PRs welcome. Before opening one, please:

1. Run `dotnet test` and confirm all 51 tests pass.
2. Add unit tests for any new business logic in `MarauderStudio.Core`.
3. Update both `ru.json` and `en.json` if you add user-facing strings.

## ⚖ Legal Notice

ESP32 Marauder is a research and educational tool. Some of its features (deauthentication, evil portals, BLE spam, etc.) may be restricted or illegal in your jurisdiction. **Use only on devices and networks you own or are explicitly authorized to test.** The authors of this application assume no responsibility for misuse.

## 📄 License

[MIT](LICENSE)
