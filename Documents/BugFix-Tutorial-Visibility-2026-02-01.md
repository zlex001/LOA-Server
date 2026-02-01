# Tutorial System Bug Fix - 2026-02-01

## Problem Summary

User reported three issues with the tutorial system:

1. **Initial Visibility Issue**: Player cannot see gold mine and lizard in the initial map, but can see them after walking one step
2. **Inconsistent Visibility**: Walking back to the initial map makes them visible again
3. **Empty Option Panel**: Clicking around causes an empty option panel to appear, causing the game to freeze with no client errors

## Root Cause Analysis

### Issue 1 & 2: Initial Visibility Problem

**Root Cause**: Gold mine and lizard were being placed on a **random** sand map in the tutorial copy, which could be at distance >1 from the shore (starting position).

**Analysis**:
- Player starts at "遗迹-岸边" (Ruins Shore) with ViewScale = 1
- Tutorial copy creation uses `scope=5`, copying all maps within range
- There are multiple "遗迹-沙地" (Ruins Sand) map tiles (see map coordinates rows 96-100)
- `Copy.Init` (line 54) randomly selects one sand map from all available candidates
- If the selected sand map is at distance >1 from shore, player cannot see the characters initially
- After player moves, visibility system updates, but the inconsistency suggests timing issues

### Issue 3: Empty Option Panel

**Root Cause**: Clicking on invalid or destroyed characters could cause `Left.Operation` to return empty item lists, causing client UI to freeze.

**Analysis**:
- `Click.Character.On` did not validate if target character is still valid
- `Left.Operation` could return empty lists if character info was unavailable
- Client UI may not handle empty option lists gracefully

## Solutions Implemented

### Fix 1: Place Characters on Closest Map

**File**: `Domain/Authentication/Register.cs`  
**Method**: `CreateTutorialCopy`

Changed strategy:
1. Create empty copy first (without characters in config)
2. Find the **closest** sand map to the start position in the copy
3. Manually create gold mine and lizard on that specific map
4. This ensures they are always within player's initial vision range

```csharp
// Before: Random selection via Copy config
copyConfig.characters[sandMap.Config.Id] = sandCharacters; // Uses random map

// After: Explicit placement on closest map
var closestSandMap = FindClosestSandMap(copy.Start, copy.Content.Gets<Copy.Map>());
var goldMine = closestSandMap.Load<Item>(goldMineConfig);
var lizard = closestSandMap.Create<Life>(lizardConfig, 1);
```

### Fix 2: Delay Initial Visibility Check

**File**: `Domain/Tutorial.cs`  
**Method**: `Start`

Added 100ms delay before checking initial visibility to ensure Perception system has indexed newly created characters.

```csharp
Domain.Time.Agent.Instance.Scheduler.Once(100, (_) =>
{
    if (player != null && player.Map != null)
    {
        CheckInitialVisibility(player, state);
    }
});
```

### Fix 3: Validate Click Targets

**File**: `Domain/Click/Character.cs`  
**Method**: `Do`

Added validation to prevent creating options for invalid characters:
- Check if target is null
- Check if character has no map and is not equipped (may be destroyed)

### Fix 4: Prevent Empty Option Lists

**File**: `Domain/Display/Left.cs`  
**Method**: `Operation`

Added safety checks:
- Return error message if target is null
- Return placeholder text if no items were generated
- Prevents empty lists from being sent to client

## Expected Behavior After Fix

1. Player spawns at shore and can **immediately see** gold mine and lizard (distance = 1)
2. Tutorial guidance triggers correctly from the start
3. Clicking on characters always shows valid option panels
4. No more game freezes from empty option panels

## Testing Recommendations

1. Create new character and verify gold mine/lizard are visible at spawn
2. Walk around and verify visibility remains consistent
3. Test clicking on various characters in different states
4. Monitor server logs for distance and placement info (added debug logs)

## Related Files Modified

- `Domain/Authentication/Register.cs` - Tutorial copy creation logic
- `Domain/Tutorial.cs` - Initial visibility check timing
- `Domain/Click/Character.cs` - Click validation
- `Domain/Display/Left.cs` - Option safety checks

## Technical Notes

### Map Coordinate System

From `Library/Design/地图坐标.csv`:
- Row 100, Col 0: 遗迹-岸边 (Shore)
- Row 100, Cols 1-4: 遗迹-沙地 (Sand, distance 1)
- Rows 96-99: More sand maps (distance 2-4)

### Copy Behavior

`Logic/Copy.cs` line 54 uses `random.Next()` to select map for character placement:
```csharp
var targetMap = candidateMaps[random.Next(candidateMaps.Count)];
```

This was causing non-deterministic character placement in tutorial copies.
