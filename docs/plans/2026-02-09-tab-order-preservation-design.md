# Tab Order Preservation & Restore Reliability

## Problem

When restoring tabs after a Windows restart or explorer crash:
1. Tabs may open at Home instead of their saved location (null location edge case)
2. Tab order is not preserved (dictionary iteration order != visual tab order)
3. Deduplication silently drops tabs at the same path

## Solution

### Tab Order
- Add `Order` field to `WindowInfo` (creation order via static counter) and `WindowRecord` (serialized)
- In `PersistWindows()`, determine actual tab order from live windows by enumerating `GetAllExplorerTabs` per parent window and matching tab handles
- Fall back to `WindowInfo.Order` (creation order) when windows are dead (crash path)
- Restore tabs sequentially with `await` instead of fire-and-forget, sorted by `Order`

### Null Location Guard
- Skip records with null/empty location in `PersistWindows()` — a missing tab is better than a wrong tab
- Add try/catch to `NavigateComplete2` handler for defensive safety

### Deduplication Removal
- Remove `GroupBy(w => w.Location)` deduplication — if two tabs share a path, both should be restored
- The existing "TakeLast 100" limit already caps total count

## Files Changed

- `WindowInfo.cs` — Add `Order` property with static counter
- `WindowRecord.cs` — Add `Order` property
- `ExplorerWatcher.cs` — Fix `PersistWindows`, `OnExplorerProcessTerminated`, `RestorePreviousWindows`, and navigate handler
