# Focus First Restored Tab After Restore

## Problem

When restoring previously opened tabs (after Explorer restart or crash), the last restored tab ends up in focus because each new tab steals focus when created. The user expects the first restored tab to be focused instead.

## Solution

Track the first restored tab's handle during `RestorePreviousWindows()` and switch focus back to it after all tabs finish restoring.

### Changes

Only `ExplorerWatcher.RestorePreviousWindows()` is modified:

1. Add `nint firstTabHandle = 0` before the restore loop.
2. In the `isFirstTab` branch, capture the active tab handle into `firstTabHandle` after navigating the existing default tab.
3. After the `foreach` loop completes, if `firstTabHandle != 0` and more than one tab was restored, call `SelectTabByHandle(_mainWindowHandle, firstTabHandle)` to switch focus back to the first tab.

### Edge Cases

- **Single tab restored**: Already focused, no extra switch needed.
- **User declines restore**: `firstTabHandle` stays 0, no action taken.
- **No main window**: Restore code is unreachable in this case.

## Approach Considered and Rejected

**Switch to index 0 after loop**: Simpler (one-liner) but assumes the first tab is always at index 0. Fragile if the first-tab navigation logic changes.
