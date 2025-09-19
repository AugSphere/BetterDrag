### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragPerformanceData](BetterDrag.ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')

## ShipDragPerformanceData\.CalculateViscousDragForce Field

An optional custom viscous drag force curve as a function of velocity and ship characteristics\.

Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s.

```csharp
public DragForceFunction? CalculateViscousDragForce;
```

#### Field Value
[DragForceFunction\(float, float, float, float, float\)](BetterDrag.ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).md 'BetterDrag\.ShipDragPerformanceData\.DragForceFunction\(float, float, float, float, float\)')