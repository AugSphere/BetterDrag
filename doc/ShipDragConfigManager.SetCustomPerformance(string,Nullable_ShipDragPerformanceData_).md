### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragConfigManager](ShipDragConfigManager.md 'BetterDrag\.ShipDragConfigManager')

## ShipDragConfigManager\.SetCustomPerformance\(string, Nullable\<ShipDragPerformanceData\>\) Method

Store custom performance data for a ship\.

Existing data is overwritten.

```csharp
public static bool SetCustomPerformance(string? shipName, System.Nullable<BetterDrag.ShipDragPerformanceData> data);
```
#### Parameters

<a name='BetterDrag.ShipDragConfigManager.SetCustomPerformance(string,System.Nullable_BetterDrag.ShipDragPerformanceData_).shipName'></a>

`shipName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The name of the ship object\. Can be found in a [community spreadsheet](https://docs.google.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing 'https://docs\.google\.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing')\.

<a name='BetterDrag.ShipDragConfigManager.SetCustomPerformance(string,System.Nullable_BetterDrag.ShipDragPerformanceData_).data'></a>

`data` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[ShipDragPerformanceData](ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Ship's performance overrides\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
`true` if custom performance was successfully set\.