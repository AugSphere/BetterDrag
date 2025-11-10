// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Size",
    "LongLine:A long line must be avoided.",
    Justification = "Using CSharpier for formatting",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Naming",
    "Underscore:The name of local variables must not include underscores.",
    Justification = "Harmony magic needs underscore",
    Scope = "type",
    Target = "~T:BetterDrag.BoatProbesFixedUpdateDragPatch"
)]
[assembly: SuppressMessage(
    "Cleaning",
    "UnusedVariable:Unused variable is declared.",
    Justification = "Compatibility with delegate",
    Scope = "type",
    Target = "~T:BetterDrag.DragModel"
)]
[assembly: SuppressMessage(
    "Refactoring",
    "NotOneShotInitialization:Declare the local variable with one-shot initialization.",
    Justification = "No cleaner way to merge",
    Scope = "member",
    Target = "~M:BetterDrag.ShipDragConfigManager.GetPerformanceData(UnityEngine.GameObject)~BetterDrag.ShipDragPerformanceData"
)]
[assembly: SuppressMessage(
    "Cleaning",
    "ByteOrderMark:The Byte Order Mark (BOM) must be removed.",
    Scope = "module",
    Justification = "VS compatibility"
)]
[assembly: SuppressMessage(
    "Cleaning",
    "UnusedVariable:Unused variable is declared.",
    Justification = "Conditional compilation",
    Scope = "type",
    Target = "~T:BetterDrag.Profiler"
)]
[assembly: SuppressMessage(
    "Refactoring",
    "UninitializedLocalVariable:The local variable is not initialized at declaration.",
    Justification = "No cleaner way to branch",
    Scope = "type",
    Target = "~T:BetterDrag.DragModel"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1814:Prefer jagged arrays over multidimensional",
    Justification = "Wastes no space",
    Scope = "type",
    Target = "~T:BetterDrag.Hydrostatics"
)]
[assembly: SuppressMessage(
    "Usage",
    "CA2243:Attribute string literals should parse correctly",
    Justification = "Bepin convention",
    Scope = "type",
    Target = "~T:BetterDrag.Plugin"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1823:Avoid unused private fields",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~F:BetterDrag.Cache`1.name"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~M:BetterDrag.ShipDragPerformanceData.FieldRepr~System.String"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1823:Avoid unused private fields",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~F:BetterDrag.Hydrostatics.shipName"
)]
