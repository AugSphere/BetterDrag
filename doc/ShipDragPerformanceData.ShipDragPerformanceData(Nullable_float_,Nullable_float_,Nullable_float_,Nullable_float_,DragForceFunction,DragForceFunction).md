### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')

## ShipDragPerformanceData\(Nullable\<float\>, Nullable\<float\>, Nullable\<float\>, Nullable\<float\>, DragForceFunction, DragForceFunction\) Constructor

A struct holding drag performance setting overrides for a single ship\.


All entries are optional, leave `null` for the ones you do not want to override.

All units are metric. Unit reference: cog's LWL is approximately 12.39m.

```csharp
public ShipDragPerformanceData(System.Nullable<float> lengthAtWaterline=null, System.Nullable<float> formFactor=null, System.Nullable<float> viscousDragMultiplier=null, System.Nullable<float> waveMakingDragMultiplier=null, BetterDrag.ShipDragPerformanceData.DragForceFunction? calculateViscousDragForce=null, BetterDrag.ShipDragPerformanceData.DragForceFunction? calculateWaveMakingDragForce=null);
```
#### Parameters

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).lengthAtWaterline'></a>

`lengthAtWaterline` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).formFactor'></a>

`formFactor` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).viscousDragMultiplier'></a>

`viscousDragMultiplier` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).waveMakingDragMultiplier'></a>

`waveMakingDragMultiplier` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).calculateViscousDragForce'></a>

`calculateViscousDragForce` [DragForceFunction\(float, float, float, float, float\)](ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).md 'BetterDrag\.ShipDragPerformanceData\.DragForceFunction\(float, float, float, float, float\)')

<a name='BetterDrag.ShipDragPerformanceData.ShipDragPerformanceData(System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,System.Nullable_float_,BetterDrag.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipDragPerformanceData.DragForceFunction).calculateWaveMakingDragForce'></a>

`calculateWaveMakingDragForce` [DragForceFunction\(float, float, float, float, float\)](ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).md 'BetterDrag\.ShipDragPerformanceData\.DragForceFunction\(float, float, float, float, float\)')