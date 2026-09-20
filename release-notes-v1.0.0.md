# 🛰️ Marauder Studio v1.0.0

First public release of **Marauder Studio** — a beautiful Windows 10/11 desktop application for managing and flashing **ESP32 Marauder** devices.

Built with .NET 9 and ModernWpfUI (Fluent Design).

---

## 📥 Download

**`MarauderStudio.exe`** — single self-contained file (~140 MB), no .NET installation required.

- SHA-256: `d77d955b264c35f896129848460009b14efc5bdbea2da7dee137b8a31a4494bc`
- Architecture: x64
- Minimum OS: Windows 10 (1809) / Windows 11

> **v1.0.0 hotfix** — fixed crash on startup caused by invalid Symbol enum values (`Connect`, `Disconnect`, `Chip`, `Important`). The app now uses confirmed-valid Segoe MDL2 Asset names. Startup log is written to `%APPDATA%\MarauderStudio\startup.log` for diagnostics.

---

## ✨ What's included

### Dashboard
- Auto-detect COM port every 2 seconds
- USB connect at 115200 baud
- Live device info from `info` command: firmware version, chip family (ESP32/S2/S3/C5/C6), hardware, Station/AP MAC, SD card status

### Flasher
- **Drag-and-drop** `.bin` files or browse via file dialog
- **GitHub Releases** integration — pick a release, choose a board, download with progress
- **26 built-in board profiles**: v4, v6, v6.1, v7, v8, Mini, Mini v3, Pancake, Cardputer, Cardputer ADV, Multiboard S3, Dev Board Pro, LDDB, M5StickC Plus/2, all CYD variants, Flipper Zero Dev Board, ESP32-S2 Reverse Feather, M5 Nano C6, LilyGo T-Dongle C5, ESP32-C5 DevKit, Dual Mini C5
- Three write methods: **esptool USB**, **Web OTA** (`http://192.168.4.1/update`), **SD-card** (`update.bin`)
- Modes: **Application-only** (OTA-safe) or **Full flash** (bootloader + partitions + OTA data + app)
- Erase options: none / app / full
- Live progress bar with stage detection (detect / erase / write / verify)
- Color-coded log console

### Serial Monitor
- Terminal-style console with mono font (Cascadia Code)
- Live send/receive over COM port
- Command history with ↑/↓ keys
- `Ctrl+L` clears the log
- Quick-action buttons: `info`, `scanap`, `scansta`, `stopscan`, `settings`, `reboot`

### Device Control
- Preset tiles for all Marauder command categories:
  - **WiFi**: scan AP, scan stations, select AP, clear/list
  - **Attacks**: beacon spam, deauth, probe, evil portal
  - **Sniffers**: beacon, deauth, PMKID, SAE, BT
  - **Bluetooth**: Sour Apple, Samsung spam, Swift Pair, AirTag spoof
  - **GPS / Wardrive**: GPS data, wardrive start/stop

### Settings
- Language: 🇷🇺 Russian (default) / 🇬🇧 English, hot-swap without restart
- Theme: Dark / Light / Follow system
- esptool path (auto-detects bundled, user-supplied, or system Python)
- WiFi credentials for OTA AP (default: `MarauderOTA` / `justcallmekoko`)
- Download folder
- All settings persisted to `%APPDATA%\MarauderStudio\settings.json`

---

## 🔌 esptool

Three resolution modes:
1. **Bundled**: drop `esptool.exe` into `Resources/esptool/` next to the app
2. **User path**: configured in Settings
3. **System Python**: `python -m esptool` if installed

Build instructions:
```bash
pip install esptool pyinstaller
pyinstaller --onefile $(which esptool)
```

---

## 🐛 Known Limitations

- Esptool auto-resolution requires Python 3 in PATH for fallback mode
- OTA via Web requires manual WiFi connection to `MarauderOTA` AP (UI hints)
- No screen-reader accessibility audit yet — fluent controls inherit ModernWpfUI's defaults
- Tests for `WifiConnector` and `WebOtaUploader` are not included (require Windows WLAN API mocking and network mocking)

---

## 🛠 Build from source

```bash
git clone https://github.com/USAchevIP/MarauderStudio.git
cd MarauderStudio
dotnet test
dotnet run --project src/MarauderStudio
```

Requirements: .NET 9 SDK.

---

## 📄 License

[MIT](../LICENSE) © 2026 Ukiterus

ESP32 Marauder © justcallmekoko — see https://github.com/justcallmekoko/ESP32Marauder

---

## ⚖ Legal Notice

ESP32 Marauder is an offensive security research tool. Features such as deauthentication, evil portals, BLE spam, and PMKID capture may be restricted or illegal in your jurisdiction. **Use only on devices and networks you own or are explicitly authorized to test.**
