# Draft: Auto Power Mode Switcher

## Core Objective
Create a lightweight Windows program that automatically switches power modes based on user presence/activity (High Performance when active, Power Saver when idle).

## Core Requirements (Confirmed)
1. **Detection Strategy**: Hybrid (User selectable: Keyboard/Mouse idle time OR Monitor display state).
2. **Tech Stack**: C# / .NET (Native AOT recommended for low memory footprint).
3. **App Type**: Windows System Tray application (GUI for settings/manual override).
4. **Power Switching**: Use `PowerSetActiveScheme` Win32 API for zero-overhead switching (fallback to `powercfg`).

## Confirmed Decisions (Complete)
- **Restore behavior**: Remember and restore the original plan when user returns (not a hard toggle).
- **Power plan source**: Read ALL power plans from the system (including custom user-created ones).
- **Default idle timeout**: 5 minutes (configurable in settings UI).
- **Detection method**: 3 modes (user selectable in settings):
  1. 键鼠空闲计时 (GetLastInputInfo polling)
  2. 显示器熄屏检测 (RegisterPowerSettingNotification event)
  3. 同时启用两种 — OR logic: either idle or monitor off triggers switch to idle plan
- **UI Framework**: MewUI (aprillz/MewUI) for settings window; Win32 Shell_NotifyIcon P/Invoke for system tray icon
- **Tray integration**: Right-click menu opens MewUI settings window, shows current status

## All Critical Decisions (Final — from Metis review)
- **Tie-breaker**: Same-priority rules → earlier creation time wins (deterministic, stable sort).
- **Cross-midnight rules**: Not supported in v1; rules within the same calendar day only. Start must be < End.
- **Manual override expiry**: Supports optional TTL (e.g., 2 hours); if no TTL set, stays until user manually clears.
- **Sleep/resume behavior**: Immediately re-evaluate strategy + idle state on system wake/resume.
- **Missing GUID fallback**: Show tray balloon notification, skip the switch, log the error.
- **Explorer restart**: Handle TaskbarCreated message and re-register tray icon automatically.
- **External power plan changes**: AutoPower re-applies on next trigger cycle — does NOT forcefully override in real-time.
- **Session lock/unlock**: Treat lock as "idle", unlock as "active" (handled naturally by monitor-off event path).
- **Restore snapshot**: Captured at startup and each time user returns from idle. Invalidated when user manually changes plan while active.
- **Log**: Rolling daily log file in %AppData%\AutoPower\logs\ for diagnostics.

## Strategy Feature (Confirmed)
- **Time-slot rules**: Define rules by time range (e.g., 09:00–18:00 → High Performance).
- **Weekday / Weekend**: Different rules for weekdays vs weekends.
- **Priority-based stacking**: Multiple rules, higher priority overrides lower priority when overlapping.
- **Manual override mode**: User can manually lock a power plan temporarily; rules are paused until manually unlocked.
- **Auto-switch logic**: Active rule overrides idle-detection switching. If no rule is active, idle-detection handles switching.

## Auto-Startup (Confirmed)
- **Method**: Windows Task Scheduler (`schtasks`) — creates task at login with optional delay.
- **Implementation**: `Process.Start("schtasks", ...)` to register/unregister the task.
- **Toggle**: Checkbox in settings window; program checks task existence to reflect current state.

## Scope
- IN: Tray icon (Win32), MewUI settings window, 3-mode detection, all power plans, configurable timeout, restore-on-return, time-slot strategies, weekday/weekend strategies, priority stacking, manual override, auto-startup via Task Scheduler.
- OUT: Camera/presence sensors, remote management, CLI-only mode, installer package.

## Tech Stack Decision
- **Language**: C# / .NET 8+ (NativeAOT for small EXE ~3.5MB)
- **UI**: MewUI (settings window) + Win32 Shell_NotifyIcon P/Invoke (tray icon)
- **Key APIs**:
  - `GetLastInputInfo` (User32.dll) — keyboard/mouse idle detection
  - `RegisterPowerSettingNotification` + `GUID_CONSOLE_DISPLAY_STATE` (User32.dll) — monitor state events
  - `PowerSetActiveScheme` / `PowerEnumerate` (PowrProf.dll) — power plan switching & listing
  - `Shell_NotifyIcon` (Shell32.dll) — system tray icon with right-click menu
- **Config storage**: JSON file in `%AppData%\AutoPower\config.json`
- **Distribution**: Single self-contained `.exe`

## Technical Approaches (Pending Research)

### 1. Detecting User Presence / Idle State

**Approach A: System Idle Time (Mouse & Keyboard)**
- **API**: `GetLastInputInfo` (Win32 User32.dll)
- **How it works**: Queries the OS for the time of the last user input event (mouse movement, click, keyboard press).
- **Pros**: Native, extremely lightweight, zero polling overhead if used with a timer, accurate for basic usage.
- **Cons**: Only detects physical input. Won't detect if the user is watching a long video or reading a document without moving the mouse.
- **Languages**: Easily P/Invoked in C#, available in C++, Rust (via `windows-rs`), Python (via `ctypes`).

**Approach B: Display State / Monitor Power Event**
- **API**: `RegisterPowerSettingNotification` with `GUID_CONSOLE_DISPLAY_STATE` (Win32 User32.dll)
- **How it works**: The app receives a message when the monitor turns off (due to Windows' own idle timer) or turns on.
- **Pros**: Event-driven (zero polling), perfectly syncs with Windows' existing logic for "user is away" (which already handles video playback correctly if the media player blocks sleep).
- **Cons**: Requires a message loop (hidden window). Only works if the user allows their monitor to sleep.

**Approach C: Camera / Presence Sensor (Advanced)**
- **API**: Windows Hello / Human Presence Sensor APIs (Windows 11)
- **Pros**: True presence detection (even if not typing).
- **Cons**: Requires specific hardware, higher CPU/battery usage to run the camera/sensor, privacy concerns.

### 2. Changing the Power Plan

**Approach A: Command Line (`powercfg`)**
- **Command**: `powercfg -setactive <GUID>`
- **Common GUIDs**:
  - High Performance: `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`
  - Power Saver: `a1841308-3541-4fab-bc81-f71556f20b4a`
  - Balanced: `381b4222-f694-41f0-9685-ff5bb260df2e`
- **Pros**: Simple to implement in any language (just launch a process).
- **Cons**: Launching a new process (`cmd.exe` or `powercfg.exe`) is slightly heavier than a direct API call, causes a brief console flash if not hidden properly.

**Approach B: Direct Win32 API**
- **API**: `PowerSetActiveScheme` (Win32 PowrProf.dll)
- **How it works**: Calls the native power management library directly with the scheme GUID.
- **Pros**: Instant, zero overhead, clean.
- **Cons**: Requires P/Invoke definitions in managed languages or unsafe code in Rust.

---

## Recommended Stacks

1. **C# / .NET (Worker Service or WinForms Tray App)**
   - **Why**: Excellent Windows integration. `GetLastInputInfo` and `PowerSetActiveScheme` are trivial to P/Invoke. Easy to make a system tray icon. Low memory footprint if compiled ahead-of-time (Native AOT in .NET 8).
2. **Rust**
   - **Why**: Maximum performance, minimum memory footprint (< 10MB RAM). The `windows-rs` crate provides safe bindings to all required APIs. Perfect for a silent background service.
3. **Python**
   - **Why**: Fastest to write. Can use `ctypes` for APIs or just `os.system('powercfg ...')`.
   - **Cons**: Heavy memory footprint, requires packaging (PyInstaller) to distribute as an `.exe`, making it a bulky ~30MB executable for a tiny task.
4. **Go**
   - **Why**: Single static binary, very low resource usage, easy cross-compilation. Good `syscall` support for Windows APIs.
