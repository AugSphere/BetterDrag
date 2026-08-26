// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

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
    "Style",
    "IDE1006:Naming Styles",
    Justification = "Harmony syntax",
    Scope = "type",
    Target = "~T:BetterDrag.BoatProbesFixedUpdateDragPatch"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Unity components and bepin plugin",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0060:Remove unused parameter",
    Justification = "Delegate API",
    Scope = "member",
    Target = "~M:BetterDrag.Physics.DragModel.CalculateViscousDragForce(System.Single,System.Single,System.Single,System.Single,System.Single)~System.Single"
)]
