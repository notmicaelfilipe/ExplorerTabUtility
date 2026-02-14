# Focus First Restored Tab — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** After restoring previously opened tabs, focus the first restored tab instead of the last one.

**Architecture:** Track the first tab's handle during the restore loop in `RestorePreviousWindows()`, then switch back to it after all tabs are restored using the existing `SelectTabByHandle` method.

**Tech Stack:** C# / .NET, COM interop (SHDocVw), Win32 API

---

### Task 1: Add first-tab tracking and focus-back logic

**Files:**
- Modify: `ExplorerTabUtility/Hooks/ExplorerWatcher.cs:528-583` (`RestorePreviousWindows` method)

**Step 1: Add tracking variables before the loop**

At line 536, add `firstTabHandle` and `tabCount` alongside the existing `isFirstTab`:

```csharp
var isFirstTab = true;
nint firstTabHandle = 0;
var tabCount = 0;
```

**Step 2: Capture the first tab handle in the `isFirstTab` branch**

After `isFirstTab = false;` (line 546), store `activeTabHandle` which is already computed at line 548:

```csharp
if (isFirstTab)
{
    isFirstTab = false;

    var activeTabHandle = GetActiveTabHandle(_mainWindowHandle);
    firstTabHandle = activeTabHandle;
```

Also increment `tabCount` inside the loop (after the `if (result != MessageBoxResult.Yes) continue;` line):

```csharp
if (result != MessageBoxResult.Yes) continue;

tabCount++;
```

**Step 3: Add focus-back after the loop**

After the `foreach` loop closes (line 582), before the method's closing brace, add:

```csharp
// Focus the first restored tab
if (firstTabHandle != 0 && tabCount > 1)
    await SelectTabByHandle(_mainWindowHandle, firstTabHandle);
```

**Step 4: Verify the full method reads correctly**

The complete modified method should be:

```csharp
private async Task RestorePreviousWindows()
{
    var result = await RunInStaThread(() => CustomMessageBox.Show(
        "Do you want to restore previously opened windows?",
        "Explorer Tab Utility",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question));

    var isFirstTab = true;
    nint firstTabHandle = 0;
    var tabCount = 0;
    foreach (var record in _closedWindows.Where(record => record.Restore).OrderBy(r => r.Order))
    {
        record.Restore = false;

        if (result != MessageBoxResult.Yes) continue;

        tabCount++;

        // Navigate the existing default tab instead of creating a new one
        if (isFirstTab)
        {
            isFirstTab = false;

            var activeTabHandle = GetActiveTabHandle(_mainWindowHandle);
            firstTabHandle = activeTabHandle;
            var window = activeTabHandle != 0
                ? await Helper.DoUntilNotDefaultAsync(() => GetWindowByTabHandle(activeTabHandle), 2_000, 50)
                : null;

            if (window != null)
            {
                var tcs = new TaskCompletionSource<bool>();
                DWebBrowserEvents2_NavigateComplete2EventHandler navigateHandler = null!;
                navigateHandler = (object _, ref object _) =>
                {
                    window.NavigateComplete2 -= navigateHandler;
                    tcs.TrySetResult(true);
                    SelectItems(window, record.SelectedItems);
                };

                window.NavigateComplete2 += navigateHandler;
                try
                {
                    await Navigate(window, record.Location);
                }
                catch
                {
                    window.NavigateComplete2 -= navigateHandler;
                    tcs.TrySetResult(false);
                }

                WinApi.RestoreWindowToForeground(_mainWindowHandle);
                await Task.WhenAny(tcs.Task, Task.Delay(5000));
                continue;
            }
        }

        await OpenTabNavigateWithSelection(record);
    }

    // Focus the first restored tab
    if (firstTabHandle != 0 && tabCount > 1)
        await SelectTabByHandle(_mainWindowHandle, firstTabHandle);
}
```

**Step 5: Build and verify**

Run: `dotnet build ExplorerTabUtility/ExplorerTabUtility.csproj`
Expected: Build succeeds with no errors.

**Step 6: Commit**

```bash
git add ExplorerTabUtility/Hooks/ExplorerWatcher.cs
git commit -m "fix: Focus first restored tab after restore completes"
```

### Manual Testing

1. Open several Explorer tabs navigated to different folders.
2. Kill `explorer.exe` (via Task Manager or `taskkill /f /im explorer.exe`).
3. Explorer restarts, the app prompts to restore — click Yes.
4. Verify: all tabs restore, and the **first** tab (lowest order) is the active/focused one.
5. Edge case: restore with only one tab — verify it's focused (no change from current behavior).
