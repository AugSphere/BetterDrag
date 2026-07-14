### [BetterDrag](BetterDrag.md 'BetterDrag')

## ShipDragPerformanceData Struct

A structure holding drag performance setting overrides for a single ship\.


All arguments are optional, pass only the ones you wish to override.

```csharp
public readonly struct ShipDragPerformanceData : System.IEquatable<BetterDrag.ShipDragPerformanceData>
```

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')

| Constructors | |
| :--- | :--- |
| [ShipDragPerformanceData\(Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, Nullable&lt;float&gt;, DragForceFunction, DragForceFunction\)](ShipDragPerformanceData.ShipDragPerformanceData(Nullable_float_,Nullable_float_,Nullable_float_,Nullable_float_,Nullable_float_,Nullable_float_,DragForceFunction,DragForceFunction).md 'BetterDrag\.ShipDragPerformanceData\.ShipDragPerformanceData\(System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, System\.Nullable\<float\>, BetterDrag\.ShipDragPerformanceData\.DragForceFunction, BetterDrag\.ShipDragPerformanceData\.DragForceFunction\)') | A structure holding drag performance setting overrides for a single ship\.    All arguments are optional, pass only the ones you wish to override. |

| Properties | |
| :--- | :--- |
| [BuoyancyMultiplier](ShipDragPerformanceData.BuoyancyMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.BuoyancyMultiplier') | Ship\-specific buoyancy multiplier\.   A value of 2.0 would make the hull float as though it displaces twice the volume of water at the same draft. |
| [CalculateViscousDragForce](ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce') | An optional custom viscous drag force curve as a function of velocity and ship characteristics\.   Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s. |
| [CalculateWaveMakingDragForce](ShipDragPerformanceData.CalculateWaveMakingDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateWaveMakingDragForce') | Same as [CalculateViscousDragForce](ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce'), but for wave\-making drag\. |
| [FormFactor](ShipDragPerformanceData.FormFactor.md 'BetterDrag\.ShipDragPerformanceData\.FormFactor') | Form factor of the hull for ITTC 57 friction line\.    Represents additional drag caused by a ship's hull form compared to a flat plate of the same wetted surface area.  Typical values range from 0.05 to 0.30, higher means more resistance. |
| [LengthMultiplier](ShipDragPerformanceData.LengthMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.LengthMultiplier') | Length multiplier for the hull\.   A value of 2.0 would make the mod treat the hull as though it is twice the length, increasing the top speed. |
| [MassMultiplier](ShipDragPerformanceData.MassMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.MassMultiplier') | Ship\-specific mass multiplier\.   A value of 2.0 would make the hull (but not the cargo) twice as heavy. |
| [ViscousDragMultiplier](ShipDragPerformanceData.ViscousDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.ViscousDragMultiplier') | Ship\-specific drag multiplier for viscous resistance\.   Viscous drag smoothly increases with velocity and dominates at low speeds. |
| [WaveMakingDragMultiplier](ShipDragPerformanceData.WaveMakingDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.WaveMakingDragMultiplier') | Ship\-specific drag multiplier for wave\-making resistance\.   Wave-making drag oscillates with velocity and rises sharply close to the highest speed the hull is capable of. |

| Methods | |
| :--- | :--- |
| [Equals\(object\)](ShipDragPerformanceData.Equals(object).md 'BetterDrag\.ShipDragPerformanceData\.Equals\(object\)') | Indicates whether this instance and a specified object are equal\. |
| [GetHashCode\(\)](ShipDragPerformanceData.GetHashCode().md 'BetterDrag\.ShipDragPerformanceData\.GetHashCode\(\)') | Returns the hash code for this instance\. |
| [ToString\(\)](ShipDragPerformanceData.ToString().md 'BetterDrag\.ShipDragPerformanceData\.ToString\(\)') | Returns the fully qualified type name of this instance\. |
