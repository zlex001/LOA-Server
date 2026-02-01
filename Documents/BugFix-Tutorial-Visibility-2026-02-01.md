# Tutorial System Bug Fix - 2026-02-01

## Problem Summary

User reported three issues with the tutorial system:

1. **Initial Visibility Issue**: Player cannot see gold mine and lizard in the initial map, but can see them after walking one step
2. **Inconsistent Visibility**: Walking back to the initial map makes them visible again
3. **Empty Option Panel**: Clicking around causes an empty option panel to appear, causing the game to freeze with no client errors

## Root Cause Analysis

### Issue 1 & 2: Initial Visibility Problem

**Root Cause**: **Timing issue** - Characters were created in the copy but Perception system hadn't indexed them yet when initial visibility check happened.

**Analysis**:
- Player spawns in tutorial copy at "遗迹-岸边" (Ruins Shore)
- Copy.Init creates gold mine and lizard on random sand map
- Tutorial.Start immediately calls CheckInitialVisibility
- BUT: Perception system needs time to index newly created characters
- Characters are actually there, but not yet visible in GetVisibleCharacters()
- After player moves, Perception updates and characters become visible

**Key Insight**: This is NOT a distance/placement problem - it's a timing/indexing problem. The random placement by Copy is correct and should be preserved.

### Issue 3: Empty Option Panel

**Root Cause**: Clicking on invalid or destroyed characters could cause `Left.Operation` to return empty item lists, causing client UI to freeze.

**Analysis**:
- `Click.Character.On` did not validate if target character is still valid
- `Left.Operation` could return empty lists if character info was unavailable
- Client UI may not handle empty option lists gracefully

## Solutions Implemented

### Fix 1: Delay Initial Visibility Check

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

### Fix 2: Validate Click Targets

**File**: `Domain/Click/Character.cs`  
**Method**: `Do`

Added validation to prevent creating options for invalid characters:
- Check if target is null
- Check if character has no map and is not equipped (may be destroyed)

### Fix 3: Prevent Empty Option Lists

**File**: `Domain/Display/Left.cs`  
**Method**: `Operation`

Added safety checks:
- Return error message if target is null
- Return placeholder text if no items were generated
- Prevents empty lists from being sent to client

## Expected Behavior After Fix

1. Characters are randomly placed by Copy system (preserves original design)
2. After 100ms delay, initial visibility check runs correctly
3. Tutorial guidance triggers based on actual visibility state
4. Clicking on characters always shows valid option panels
5. No more game freezes from empty option panels

## Testing Recommendations

1. Create new character and verify gold mine/lizard are visible at spawn
2. Walk around and verify visibility remains consistent
3. Test clicking on various characters in different states
4. Monitor server logs for distance and placement info (added debug logs)

## Related Files Modified

- `Domain/Tutorial.cs` - Initial visibility check timing (100ms delay)
- `Domain/Click/Character.cs` - Click validation
- `Domain/Display/Left.cs` - Option safety checks

## Technical Notes

### Why Not Change Character Placement?

Initially considered changing character placement logic to guarantee distance/visibility, but this would:
1. Add unnecessary complexity to tutorial-specific code
2. Duplicate logic that Copy already handles well
3. Violate the principle that Copy should randomly place characters

The random placement is **intentional** and **correct** - tutorial should work regardless of where characters spawn.

### Copy Behavior (Preserved)

`Logic/Copy.cs` line 54 uses `random.Next()` to select map for character placement:
```csharp
var targetMap = candidateMaps[random.Next(candidateMaps.Count)];
```

This random selection is preserved for all copies including tutorial.
