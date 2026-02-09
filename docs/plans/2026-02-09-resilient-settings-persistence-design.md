# Resilient Settings Persistence

## Problem

The `settings.json` file stores both user preferences and volatile window records. `File.WriteAllText` is not atomic — it truncates first, then writes. If the process is killed mid-write (power outage, Windows shutdown), the file is left empty or with partial JSON. On next launch, deserialization fails silently and all settings reset to defaults, including `RestorePreviousWindows`.

## Solution

Two changes:

1. **Separate files**: User preferences (`settings.json`) and window records (`windows.json`) are stored independently. Corrupted window records can never reset user preferences.
2. **Atomic writes with backup**: All file writes use a tmp-write + `File.Replace` pattern. A `.bak` backup of the previous version is kept automatically.

## File Layout

```
%AppData%/ExplorerTabUtility/
  settings.json      # User preferences (small, stable)
  settings.bak       # Previous settings (auto-maintained)
  windows.json       # Window records (volatile, frequent writes)
  windows.bak        # Previous window records (auto-maintained)
```

## Write Pattern

```
1. Serialize JSON in memory
2. Write to .tmp file
3. File.Replace(tmp, primary, bak)  — NTFS atomic operation
```

## Read Pattern (fallback chain)

```
1. Try primary file
2. If missing/corrupted → try .bak file
3. If both fail → use defaults
```

## Files Changed

- `Constants.cs` — Add `WindowsFileName` constant
- `SettingsManager.cs` — Split persistence, atomic writes, thread-safe locks
- `AppSettings` — Remove `ClosedWindows` property

## Public API

No changes. `SettingsManager.ClosedWindows` getter/setter still works identically from the caller's perspective.
