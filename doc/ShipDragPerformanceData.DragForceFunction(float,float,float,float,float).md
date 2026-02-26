### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')

## ShipDragPerformanceData\.DragForceFunction\(float, float, float, float, float\) Delegate

Custom force function type\.

```csharp
public delegate float ShipDragPerformanceData.DragForceFunction(float forwardVelocity, float lengthAtWaterline, float formFactor, float displacement, float wettedArea);
```
#### Parameters

<a name='BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).forwardVelocity'></a>

`forwardVelocity` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

Absolute forward component of ship velocity in default unity meters/second\.

<a name='BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).lengthAtWaterline'></a>

`lengthAtWaterline` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

Length at waterline in meters\. Specified in ship's configuration\.

<a name='BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).formFactor'></a>

`formFactor` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

Form factor of the ship\. Specified in ship's configuration\.

<a name='BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).displacement'></a>

`displacement` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

Ship's displacement in m^3\. Calculated by the mod\.

<a name='BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).wettedArea'></a>

`wettedArea` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

Ship's wetted surface area in m^2\. Calculated by the mod\.

#### Returns
[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')  
Absolute water drag force magnitude in N\.