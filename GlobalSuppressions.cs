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
    Target = "~T:BetterDrag.Physics.DragModel"
)]
[assembly: SuppressMessage(
    "Refactoring",
    "NotOneShotInitialization:Declare the local variable with one-shot initialization.",
    Justification = "No cleaner way to merge",
    Scope = "member",
    Target = "~M:BetterDrag.ShipConfiguration.ShipDragConfigManager.GetPerformanceData(UnityEngine.GameObject)~BetterDrag.ShipConfiguration.ShipDragPerformanceData"
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
    Target = "~T:BetterDrag.Utilities.Profiler"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0051:Remove unused private members",
    Justification = "Conditional compilation",
    Scope = "type",
    Target = "~T:BetterDrag.Utilities.Profiler"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1814:Prefer jagged arrays over multidimensional",
    Justification = "Wastes no space",
    Scope = "type",
    Target = "~T:BetterDrag.Hydrostatics.Hydrostatics"
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
    Target = "~F:BetterDrag.Utilities.Cache`1._name"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0052:Remove unread private members",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~F:BetterDrag.Utilities.Cache`1._name"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~M:BetterDrag.ShipConfiguration.ShipDragPerformanceData.FieldRepr~System.String"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0052:Remove unread private members",
    Justification = "Conditional compilation",
    Scope = "member",
    Target = "~F:BetterDrag.Hydrostatics.Hydrostatics._shipName"
)]
[assembly: SuppressMessage(
    "Cleaning",
    "UnusedVariable:Unused variable is declared.",
    Justification = "Debug print support",
    Scope = "member",
    Target = "~M:BetterDrag.Physics.PhysicsCalculation.CalculateDragForce(System.Single,System.Single,System.Single,System.Single,BetterDrag.ShipConfiguration.ShipDragPerformanceData,System.Boolean,System.Int32)~System.Single"
)]
[assembly: SuppressMessage(
    "Cleaning",
    "UnusedVariable:Unused variable is declared.",
    Justification = "API Deprecation",
    Scope = "member",
    Target = "~M:BetterDrag.ShipConfiguration.ShipDragPerformanceData.#ctor(System.Single,System.Nullable{System.Single},System.Nullable{System.Single},System.Nullable{System.Single},System.Nullable{System.Single},System.Nullable{System.Single},System.Nullable{System.Single},BetterDrag.ShipConfiguration.ShipDragPerformanceData.DragForceFunction,BetterDrag.ShipConfiguration.ShipDragPerformanceData.DragForceFunction)"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1814:Prefer jagged arrays over multidimensional",
    Justification = "Wastes no space",
    Scope = "type",
    Target = "~T:BetterDrag.Utilities.OutputFilter"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0008:Use explicit type",
    Justification = "Strict enough without it",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Refactoring",
    "IsNull:Do not use 'is' pattern matching with 'null'.",
    Justification = "Reference check for null is correct",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE1006:Naming Styles",
    Justification = "Harmony syntax",
    Scope = "type",
    Target = "~T:BetterDrag.BoatProbesFixedUpdateDragPatch"
)]