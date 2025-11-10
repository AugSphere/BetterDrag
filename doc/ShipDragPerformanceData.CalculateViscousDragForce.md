### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')

## ShipDragPerformanceData\.CalculateViscousDragForce Property

An optional custom viscous drag force curve as a function of velocity and ship characteristics\.

Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s.

```csharp
public BetterDrag.ShipDragPerformanceData.DragForceFunction CalculateViscousDragForce { get; }
```

#### Property Value
[DragForceFunction\(float, float, float, float, float\)](ShipDragPerformanceData.DragForceFunction(float,float,float,float,float).md 'BetterDrag\.ShipDragPerformanceData\.DragForceFunction\(float, float, float, float, float\)')