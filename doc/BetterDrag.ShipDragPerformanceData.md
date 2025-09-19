### [BetterDrag](BetterDrag.md 'BetterDrag')

## ShipDragPerformanceData Class

A class holding drag performance setting overrides for a single ship\.


All entries are optional, leave `null` for the ones you do not want to override.

All units are metric. Unit reference: cog's LWL is approximately 12.39m.

```csharp
public class ShipDragPerformanceData
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ShipDragPerformanceData

| Fields | |
| :--- | :--- |
| [CalculateViscousDragForce](BetterDrag.ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce') | An optional custom viscous drag force curve as a function of velocity and ship characteristics\.   Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s. |
| [CalculateWaveMakingDragForce](BetterDrag.ShipDragPerformanceData.CalculateWaveMakingDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateWaveMakingDragForce') | Same as [CalculateViscousDragForce](BetterDrag.ShipDragPerformanceData.CalculateViscousDragForce.md 'BetterDrag\.ShipDragPerformanceData\.CalculateViscousDragForce'), but for wave\-making drag\. |
| [FormFactor](BetterDrag.ShipDragPerformanceData.FormFactor.md 'BetterDrag\.ShipDragPerformanceData\.FormFactor') | Form factor of the hull for ITTC 57 friction line\.    Represents additional drag caused by a ship's hull form compared to a flat plate of the same wetted surface area.  Typical values range from 0.05 to 0.30, higher means more resistance. |
| [LengthAtWaterline](BetterDrag.ShipDragPerformanceData.LengthAtWaterline.md 'BetterDrag\.ShipDragPerformanceData\.LengthAtWaterline') | Length of the hull at waterline in metres\. |
| [ViscousDragMultiplier](BetterDrag.ShipDragPerformanceData.ViscousDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.ViscousDragMultiplier') | Ship\-specific drag multiplier for viscous resistance\. |
| [WaveMakingDragMultiplier](BetterDrag.ShipDragPerformanceData.WaveMakingDragMultiplier.md 'BetterDrag\.ShipDragPerformanceData\.WaveMakingDragMultiplier') | Ship\-specific drag multippier for wave\-making resistance\. |
