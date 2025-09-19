### [BetterDrag](BetterDrag.md 'BetterDrag').[ShipDragDataStore](BetterDrag.ShipDragDataStore.md 'BetterDrag\.ShipDragDataStore')

## ShipDragDataStore\.SetCustomPerformance\(string, ShipDragPerformanceData\) Method

Store custom performance data for a ship\.

Existing data is overwritten.

```csharp
public static bool SetCustomPerformance(string? shipName, BetterDrag.ShipDragPerformanceData? data);
```
#### Parameters

<a name='BetterDrag.ShipDragDataStore.SetCustomPerformance(string,BetterDrag.ShipDragPerformanceData).shipName'></a>

`shipName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The name of the ship object\. Can be found in a [community spreadsheet](https://docs.google.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing 'https://docs\.google\.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing')\.

<a name='BetterDrag.ShipDragDataStore.SetCustomPerformance(string,BetterDrag.ShipDragPerformanceData).data'></a>

`data` [ShipDragPerformanceData](BetterDrag.ShipDragPerformanceData.md 'BetterDrag\.ShipDragPerformanceData')

Ship's peformance overrides\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
\`true\` if custom performace was successfully set\.