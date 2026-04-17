# AutoPower

AutoPower is a lightweight Windows tray app that automatically switches Windows power plans based on user activity and time-based rules.

## Features

- Detect idle state with keyboard/mouse, monitor state, or both
- Switch between active and idle power plans automatically
- Apply scheduled rules with priority support
- Temporarily override the current plan with an optional expiration time
- Start with Windows
- Manage everything from the tray icon and settings window
- Write rolling logs for troubleshooting

## Requirements

- Windows 10 or later (full tray app experience)
- Linux (GNOME) for basic power profile switching support
- Administrator rights to change power plans (Windows)
- .NET 10 SDK to build from source

## Build

```powershell
dotnet restore
dotnet build
dotnet publish src/AutoPower -c Release -r win-x64 -p:PublishAot=true
```

## Run

```powershell
.\src\AutoPower\bin\Release\net10.0\win-x64\publish\AutoPower.exe
```

After launch, use the tray icon to open settings and choose detection mode, active plan, idle plan, and optional schedule rules.

## Data Locations

- Config: `./data/config.json`
- Logs: `./logs/`
