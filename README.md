# LiveSession

**LiveSession** is a lightweight Windows system tray application that prevents web applications from timing out in browsers — even when you're working in other programs like Excel or a PMS system.

Designed for hotel environments where **Island Browser** (and other Chromium-based browsers) log out of Guest Chat and other web apps after 10 minutes of no interaction.

---

## How It Works

LiveSession runs silently in the background and periodically sends invisible keyboard events directly to browser windows using the Windows `PostMessage` API — **without stealing focus or interrupting your work**.

- Targets browser renderer windows at the OS level (no browser extension needed)
- Works even when the browser is minimized or behind other windows
- Each monitored app has its own independent keep-alive timer
- Supports Island Browser, Microsoft Edge, Google Chrome, and any other windowed app

---

## Download

**[⬇ Download LiveSession.exe](https://github.com/SedatTelli/livesession/releases/latest/download/LiveSession.exe)**

Single file · No installation required · ~60 MB (self-contained .NET 10)

---

## Installation (Step by Step)

### Step 1 — Choose a permanent folder

Before running the app for the first time, place `LiveSession.exe` in a **permanent location**. The app registers its own path for Windows auto-start, so moving the file later will break auto-start.

**Recommended path:**
```
C:\Users\YourName\AppData\Local\Programs\LiveSession\LiveSession.exe
```

Or a simpler path that works without admin rights:
```
C:\LiveSession\LiveSession.exe
```

> ⚠️ Do **not** run from your Downloads folder — auto-start will point to Downloads and the file may later be moved or deleted.

### Step 2 — Run the application

Double-click `LiveSession.exe`. No installer, no admin rights required.

The app will:
- Appear in the **system tray** (bottom-right clock area) — not in the taskbar
- Register itself to **start automatically with Windows** on first launch
- Begin monitoring Island Browser, Microsoft Edge, and Google Chrome immediately

### Step 3 — Verify it's running

Look for the LiveSession icon in the system tray (bottom-right corner of the screen, near the clock).

- **Single click** → Open Dashboard
- **Right-click** → Tray menu

---

## Usage

### System Tray Menu

| Menu Item | Action |
|---|---|
| Open Dashboard | View status of monitored apps |
| Settings | Configure which apps to monitor and intervals |
| Pause Protection | Temporarily stop keep-alive events |
| View Logs | Open the log folder |
| Exit | Close the application |

### Dashboard

Shows real-time status of each monitored application:
- Green dot = app is running and being kept alive
- Grey dot = app is not running (no action needed)
- Last keep-alive time per app
- Today's total action count

### Settings

1. Click **"Refresh Apps"** to scan currently open windows
2. Check/uncheck apps to include or exclude them
3. Use the **slider** (1–120 min) to set the keep-alive interval per app
4. Click **Save** — takes effect immediately, no restart needed

---

## Default Protected Applications

| Application | Process | Interval |
|---|---|---|
| Island Browser | `Island` | Every 4 minutes |
| Microsoft Edge | `msedge` | Every 4 minutes |
| Google Chrome | `chrome` | Every 4 minutes |

---

## Auto-Start on Windows Boot

LiveSession automatically registers itself in the Windows Registry under:

```
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
```

This means it starts silently with Windows — no taskbar entry, only the system tray icon.

If you ever move the EXE to a different folder, simply re-run it once and it will update the registry entry automatically.

To **disable** auto-start: open Task Manager → Startup Apps → disable LiveSession.

---

## Settings File

Settings are stored at:
```
C:\Users\YourName\AppData\Roaming\LiveSession\settings.json
```

You can edit this file manually or use the Settings window in the app.

---

## Logs

Log files are stored at:
```
C:\Users\YourName\AppData\Roaming\LiveSession\Logs\
```

Rolling daily logs, kept for 7 days. Access via tray menu → **View Logs**.

---

## System Requirements

- Windows 10 / 11 (64-bit)
- No .NET installation required (self-contained)
- ~60 MB disk space

---

## Contributors

<!-- intentionally left blank -->

---

*by [Sedat Telli](https://www.linkedin.com/in/sedattelli/)*
