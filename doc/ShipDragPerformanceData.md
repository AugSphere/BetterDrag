### [BetterDrag](BetterDrag.md 'BetterDrag')

## ShipDragPerformanceData Struct

A struct holding drag performance setting overrides for a single ship\.


All entries are optional, leave `null` for the ones you do not want to override.

All units are metric. Unit reference: cog's LWL is approximately 12.39m.

```csharp
public readonly struct ShipDragPerformanceData : System.IEquatable<BetterDrag.ShipDragPerformanceData>
```

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')

| Constructors | |
| :--- | :--- |
| [ShipDragPerformanceData\(Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, DragForceFunction, DragForceFunction\)](ShipDragPerformanceData.ShipDragPerformanceData(Nullable_float_,Nullable_float_,Nullable_float_,Nullable_float_,Nullable_float_,DragForceFunction,DragForceFunction).md 'BetterDrag\.ShipDragPerformanceData\.ShipDragPerformanceData\(System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, BetterDrag\.ShipDragPerformanceData\.DragForceFunction, BetterDrag\.ShipDragPerformanceData\.DragForceFunction\)') | A struct holding drag performance setting overrides for a single ship\.    All entries are optional, leave `null` for the ones you do not want to override.  All units are metric. Unit reference: cog's LWL is approximately 12.39m. |

| Properties | |
| :--- | :--- |
| [BuoyancyMultiplier](ShipDragPerformanceData.BuoyancyMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.BuoyancyMultiplier') | Ship\-specific buoyancy multippier\. |
| [CalculateViscousDragForce](ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce') | An optional custom viscous drag force curve as a function of velocity and ship characteristics\.   Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s. |
| [CalculateWaveMakingDragForce](ShipDragPerformanceData.CalculateWaveMakingDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateWaveMakingDragForce') | Same as [CalculateViscousDragForce](ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce'), but for wave\-making drag\. |
| [FormFactor](ShipDragPerformanceData.FormFactor.md 'BetterDrag\.ShipDragPerformanceData\.FormFactor') | Form factor of the hull for ITTC 57 friction line\.    Represents additional drag caused by a ship's hull form compared to a flat plate of the same wetted surface area.  Typical values range from 0.05 to 0.30, higher means more resistance. |
| [LengthAtWaterline](ShipDragPerformanceData.LengthAtWaterline.md 'BetterDrag\.ShipDragPerformanceData\.LengthAtWaterline') | Length of the hull at waterline in metres\. |
| [ViscousDragMultiplier](ShipDragPerformanceData.ViscousDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.ViscousDragMultiplier') | Ship\-specific drag multiplier for viscous resistance\. |
| [WaveMakingDragMultiplier](ShipDragPerformanceData.WaveMakingDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.WaveMakingDragMultiplier') | Ship\-specific drag multippier for wave\-making resistance\. |

| Methods | |
| :--- | :--- |
| [Equals\(object\)](ShipDragPerformanceData.Equals(object).md 'BetterDrag\.ShipDragPerformanceData\.Equals\(object\)') | Indicates whether this instance and a specified object are equal\. |
| [GetHashCode\(\)](ShipDragPerformanceData.GetHashCode().md 'BetterDrag\.ShipDragPerformanceData\.GetHashCode\(\)') | Returns the hash code for this instance\. |
| [ToString\(\)](ShipDragPerformanceData.ToString().md 'BetterDrag\.ShipDragPerformanceData\.ToString\(\)') | Returns the fully qualified type name of this instance\. |
