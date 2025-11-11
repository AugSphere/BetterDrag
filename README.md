Modifies boat physics to introduce more realistic drag, with the hull's shape
and size now affecting longitudinal drag.

# Key features
* Drag is modelled as a force: fully loaded big ships are harder to slow down
  than empty ones.
* A simple simulation of wave-making resistance: hull and hump speeds are
  modelled for each hull.
* Heavily laden ships that sit lower in the water experience more drag.
* Configurable water drag settings for each ship and globally.

# Installation
The mod depends on [BepInEx 5](https://github.com/BepInEx/BepInEx).

After BepInEx is installed, extract the contents of the release archive into
the `Sailwind\BepInEx\plugins` folder.

# Configuration
After launching the game once with the mod installed two configuration files
will be created:
* `Sailwind\BepInEx\config\com.AugSphere.BetterDrag.cfg` holds global mod
settings.
* `com.AugSphere.BetterDrag.shipdata.json` in the same directory as
`BetterDrag.dll` allows to override specific ship settings.

Refer to [ShipDragPerformanceData documentation](doc/ShipDragPerformanceData.md)
for what the settings mean. Setting custom water drag functions through
config files is not currently supported.

# Mod compatibility
There are no known conflicts.

If you are an author of a ship mod and would like to include custom drag
settings for your ship, add this mod's assembly reference and set up
`BetterDrag` as a soft dependency:
```c#
[BepInDependency("com.AugSphere.BetterDrag", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
   private void Awake()
    {
//...
        BetterDragCompatibility.AddBetterDragData();
```

Call [SetCustomPerformance](doc/ShipDragConfigManager.SetCustomPerformance(string,Nullable_ShipDragPerformanceData_).md)
with the name of your ship. Names can be found in
[this community spreadsheet](https://docs.google.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing).

Make sure the calling code is annotated with
`[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]`
and guarded by a check for `BetterDrag` being present, so the custom drag
settings can be skipped if a user does not have the mod installed.

For example:
```c#
internal static class BetterDragCompatibility
{
    private static bool? _enabled;

    public static bool IsEnabled
    {
        get
        {
            _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                "com.AugSphere.BetterDrag"
            );
            return (bool)_enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool AddBetterDragData()
    {
        if (!IsEnabled)
            return false;
        BetterDrag.ShipDragPerformanceData.DragForceFunction customViscousDragFunc = 
            (forwardVelocity, _, _, _, _) => {
            return forwardVelocity * 100f;
        };
        var customData = new BetterDrag.ShipDragPerformanceData()
        {
            LengthAtWaterline = 50,
            CalculateViscousDragForce = customViscousDragFunc,
        };

        var shipName = "BOAT GLORIANA (182)";
        return BetterDrag.ShipDragConfigManager.SetCustomPerformance(shipName, customData);
    }
}
```

# Development
Copy or link `Assembly-CSharp.dll` and `Crest.dll` from `Sailwind\Sailwind_Data\Managed`
into the `lib` folder.

A [Csharpier](https://csharpier.com/) formatting husky pre-commit hook is included.