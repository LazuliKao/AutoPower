# AutoPower

AutoPower is a lightweight Windows tray app that automatically switches Windows power plans based on rule-driven conditions.

## Features

- Detect `Keyboard/Mouse idle`, `Monitor off`, or both as runtime condition sources
- Evaluate prioritized action rules with `all` / `any` / `none` condition groups
- Combine day-of-week, time range, keyboard/mouse idle, and monitor state conditions
- Use an explicit `defaultPlanGuid` before falling back to active/idle plans
- Temporarily override the current plan with an optional expiration time
- Preview the next 24 hours using the current detector snapshot
- Start with Windows and manage everything from the tray icon and settings window
- Write rolling logs for troubleshooting

## Requirements

- Windows 10 or later
- Administrator rights to change power plans
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

After launch, use the tray icon to open settings and configure detector availability, rule conditions, default plan, and fallback plans.

## Config Overview

Top-level fields:

- `mode`: which detector sources are available to rules and fallback
- `idleTimeoutMinutes`: idle threshold for `Keyboard/Mouse idle`
- `defaultPlanGuid`: optional plan used when no rule matches
- `activePlanGuid` / `idlePlanGuid`: final safety fallback plans
- `rules`: prioritized action rules
- `override`: temporary manual override

Each rule targets exactly one power plan and contains a **tree-structured** condition group:

```json
{
  "name": "Work Hours",
  "targetPlanGuid": "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
  "priority": 10,
  "isEnabled": true,
  "condition": {
    "operator": 0,
    "conditions": [
      { "type": 0, "dayType": 1 },
      { "type": 1, "start": "09:00:00", "end": "17:00:00" }
    ],
    "groups": []
  }
}
```

### Condition Group Structure (Tree-Based)

**Each `StrategyConditionGroup` is a node in the evaluation tree:**

```
Root Group (All / Any / None)
├── Leaf Conditions
│   ├── DayType (weekday/weekend/all)
│   ├── TimeRange (start - end)
│   ├── KeyboardMouseIdle
│   └── MonitorOff
└── Nested Groups (recursive)
    ├── SubGroup 1 (All / Any / None)
    │   ├── Leaf Conditions...
    │   └── Nested Groups...
    └── SubGroup 2 (All / Any / None)
        └── ...
```

**Operators:**
- `All (0)`: ALL conditions AND nested groups must be true (short-circuit: first false fails)
- `Any (1)`: ANY condition OR nested group can be true (short-circuit: first true succeeds)
- `None (2)`: NONE of the conditions or nested groups can be true (short-circuit: first true fails)

### Nested Group Examples

**Example 1: Work Hours OR After-Hours Meeting**
```json
{
  "operator": 1,
  "conditions": [],
  "groups": [
    {
      "operator": 0,
      "conditions": [
        { "type": 0, "dayType": 1 },
        { "type": 1, "start": "09:00:00", "end": "17:00:00" }
      ],
      "groups": []
    },
    {
      "operator": 0,
      "conditions": [
        { "type": 1, "start": "18:00:00", "end": "20:00:00" }
      ],
      "groups": []
    }
  ]
}
```
Evaluation: `(Weekday AND 9AM-5PM) OR (6PM-8PM)`

**Example 2: Weekday AND (Idle OR Monitor Off)**
```json
{
  "operator": 0,
  "conditions": [
    { "type": 0, "dayType": 1 }
  ],
  "groups": [
    {
      "operator": 1,
      "conditions": [
        { "type": 2 },
        { "type": 3 }
      ],
      "groups": []
    }
  ]
}
```
Evaluation: `Weekday AND (KeyboardMouseIdle OR MonitorOff)`

**Example 3: Business Hours AND NOT (Presentation Time)**
```json
{
  "operator": 0,
  "conditions": [
    { "type": 0, "dayType": 1 },
    { "type": 1, "start": "08:00:00", "end": "18:00:00" }
  ],
  "groups": [
    {
      "operator": 2,
      "conditions": [
        { "type": 1, "start": "10:00:00", "end": "11:30:00" },
        { "type": 1, "start": "14:00:00", "end": "15:30:00" }
      ],
      "groups": []
    }
  ]
}
```
Evaluation: `Weekday AND 8AM-6PM AND NOT (10AM-11:30AM OR 2PM-3:30PM)`

**Example 4: Complex Nested (All within Any within All)**
```json
{
  "operator": 0,
  "conditions": [
    { "type": 0, "dayType": 1 }
  ],
  "groups": [
    {
      "operator": 1,
      "conditions": [],
      "groups": [
        {
          "operator": 0,
          "conditions": [
            { "type": 1, "start": "09:00:00", "end": "17:00:00" }
          ],
          "groups": []
        },
        {
          "operator": 0,
          "conditions": [
            { "type": 1, "start": "20:00:00", "end": "22:00:00" },
            { "type": 2 }
          ],
          "groups": []
        }
      ]
    }
  ]
}
```
Evaluation: `Weekday AND ((9AM-5PM) OR (8PM-10PM AND Keyboard/Mouse Idle))`

Enum values:

- `StrategyConditionGroupOperator`: `All = 0`, `Any = 1`, `None = 2`
- `StrategyConditionType`: `DayType = 0`, `TimeRange = 1`, `KeyboardMouseIdle = 2`, `MonitorOff = 3`
- `DayType`: `All = 0`, `Weekday = 1`, `Weekend = 2`

## Evaluation Order

Runtime decisions follow this order:

1. Active manual override
2. First matching rule by `priority desc`, then `createdAt asc`, then `id asc`
3. `defaultPlanGuid`
4. Final fallback: `idlePlanGuid` when the enabled detectors say idle, otherwise `activePlanGuid`

## Data Locations

- Config: `./data/config.json`
- Logs: `./logs/`
