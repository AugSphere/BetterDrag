using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using static BetterDrag.ShipConfiguration.ShipDragPerformanceData;
#if DEBUG
using System.Reflection;
#endif

namespace BetterDrag.ShipConfiguration;

/// <summary>
/// A structure holding drag performance setting overrides for a single ship.
///
/// <para>
/// All arguments are optional, pass only the ones you wish to override.
/// </para>
/// </summary>
[DataContract]
public readonly struct ShipDragPerformanceData(
    float? formFactor = null,
    float? buoyancyMultiplier = null,
    float? viscousDragMultiplier = null,
    float? waveMakingDragMultiplier = null,
    float? lengthMultiplier = null,
    float? massMultiplier = null,
    DragForceFunction? calculateViscousDragForce = null,
    DragForceFunction? calculateWaveMakingDragForce = null
) : IEquatable<ShipDragPerformanceData>
{
    [DataMember(EmitDefaultValue = false, IsRequired = false, Name = nameof(LengthMultiplier))]
    private readonly float? _lengthMultiplier = lengthMultiplier;

    [DataMember(EmitDefaultValue = false, IsRequired = false, Name = nameof(FormFactor))]
    private readonly float? _formFactor = formFactor;

    [DataMember(EmitDefaultValue = false, IsRequired = false, Name = nameof(BuoyancyMultiplier))]
    private readonly float? _buoyancyMultiplier = buoyancyMultiplier;

    [DataMember(EmitDefaultValue = false, IsRequired = false, Name = nameof(MassMultiplier))]
    private readonly float? _massMultiplier = massMultiplier;

    [DataMember(EmitDefaultValue = false, IsRequired = false, Name = nameof(ViscousDragMultiplier))]
    private readonly float? _viscousDragMultiplier = viscousDragMultiplier;

    [DataMember(
        EmitDefaultValue = false,
        IsRequired = false,
        Name = nameof(WaveMakingDragMultiplier)
    )]
    private readonly float? _waveMakingDragMultiplier = waveMakingDragMultiplier;

    [NonSerialized]
    private readonly DragForceFunction? _calculateViscousDragForce = calculateViscousDragForce;

    [NonSerialized]
    private readonly DragForceFunction? _calculateWaveMakingDragForce =
        calculateWaveMakingDragForce;

    /// <inheritdoc/>
    [Obsolete("lengthAtWaterline is no longer used. Remove it.")]
    public ShipDragPerformanceData(
        float lengthAtWaterline,
        float? formFactor = null,
        float? buoyancyMultiplier = null,
        float? viscousDragMultiplier = null,
        float? waveMakingDragMultiplier = null,
        float? lengthMultiplier = null,
        float? massMultiplier = null,
        DragForceFunction? calculateViscousDragForce = null,
        DragForceFunction? calculateWaveMakingDragForce = null
    )
        : this(
            formFactor: formFactor,
            buoyancyMultiplier: buoyancyMultiplier,
            viscousDragMultiplier: viscousDragMultiplier,
            waveMakingDragMultiplier: waveMakingDragMultiplier,
            lengthMultiplier: lengthMultiplier,
            massMultiplier: massMultiplier,
            calculateViscousDragForce: calculateViscousDragForce,
            calculateWaveMakingDragForce: calculateWaveMakingDragForce
        ) { }

    /// <summary>
    /// Length multiplier for the hull.
    /// <para>
    /// A value of 2.0 would make the mod treat the hull as though it is twice the length, increasing the top speed.
    /// </para>
    /// </summary>
    public readonly float LengthMultiplier =>
        _lengthMultiplier ?? DefaultShipConfigurations.BaseShipConfiguration.LengthMultiplier;

    /// <summary>
    /// Form factor of the hull for ITTC 57 friction line.
    ///
    /// <para>
    /// Represents additional drag caused by a ship's hull form compared to a flat plate of the same wetted surface area.
    /// </para>
    /// <para>
    /// Typical values range from 0.05 to 0.30, higher means more resistance.
    /// </para>
    /// </summary>
    public readonly float FormFactor =>
        _formFactor ?? DefaultShipConfigurations.BaseShipConfiguration.FormFactor;

    /// <summary>
    /// Ship-specific buoyancy multiplier.
    /// <para>
    /// A value of 2.0 would make the hull float as though it displaces twice the volume of water at the same draft.
    /// </para>
    /// </summary>
    public readonly float BuoyancyMultiplier =>
        _buoyancyMultiplier ?? DefaultShipConfigurations.BaseShipConfiguration.BuoyancyMultiplier;

    /// <summary>
    /// Ship-specific mass multiplier.
    /// <para>
    /// A value of 2.0 would make the hull (but not the cargo) twice as heavy.
    /// </para>
    /// </summary>
    public readonly float MassMultiplier =>
        _massMultiplier ?? DefaultShipConfigurations.BaseShipConfiguration.MassMultiplier;

    /// <summary>
    /// Ship-specific drag multiplier for viscous resistance.
    /// <para>
    /// Viscous drag smoothly increases with velocity and dominates at low speeds.
    /// </para>
    /// </summary>
    public readonly float ViscousDragMultiplier =>
        _viscousDragMultiplier
        ?? DefaultShipConfigurations.BaseShipConfiguration.ViscousDragMultiplier;

    /// <summary>
    /// Ship-specific drag multiplier for wave-making resistance.
    /// <para>
    /// Wave-making drag oscillates with velocity and rises sharply close to the highest speed the hull is capable of.
    /// </para>
    /// </summary>
    public readonly float WaveMakingDragMultiplier =>
        _waveMakingDragMultiplier
        ?? DefaultShipConfigurations.BaseShipConfiguration.WaveMakingDragMultiplier;

    /// <summary>
    /// Custom force function type.
    /// </summary>
    /// <param name="forwardVelocity">Absolute forward component of ship velocity in default unity meters/second.</param>
    /// <param name="lengthAtWaterline">Length at waterline in meters. Calculated by the mod.</param>
    /// <param name="formFactor">Form factor of the ship. Specified in ship's configuration.</param>
    /// <param name="displacement">Ship's displacement in m^3. Calculated by the mod.</param>
    /// <param name="wettedArea">Ship's wetted surface area in m^2. Calculated by the mod.</param>
    /// <returns>Absolute water drag force magnitude in N.</returns>
    public delegate float DragForceFunction(
        float forwardVelocity,
        float lengthAtWaterline,
        float formFactor,
        float displacement,
        float wettedArea
    );

    /// <summary>
    /// An optional custom viscous drag force curve as a function of velocity and ship characteristics.
    /// <para>
    /// Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s.
    /// </para>
    /// </summary>
    public readonly DragForceFunction CalculateViscousDragForce =>
        _calculateViscousDragForce
        ?? DefaultShipConfigurations.BaseShipConfiguration.CalculateViscousDragForce;

    /// <summary>
    /// Same as <see cref="CalculateViscousDragForce"/>, but for wave-making drag.
    /// </summary>
    public readonly DragForceFunction CalculateWaveMakingDragForce =>
        _calculateWaveMakingDragForce
        ?? DefaultShipConfigurations.BaseShipConfiguration.CalculateWaveMakingDragForce;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        return obj is ShipDragPerformanceData data && Equals(data);
    }

    /// <inheritdoc/>
    public readonly bool Equals(ShipDragPerformanceData other)
    {
        return _lengthMultiplier == other._lengthMultiplier
            && _formFactor == other._formFactor
            && _buoyancyMultiplier == other._buoyancyMultiplier
            && _massMultiplier == other._massMultiplier
            && _viscousDragMultiplier == other._viscousDragMultiplier
            && _waveMakingDragMultiplier == other._waveMakingDragMultiplier
            && ReferenceEquals(_calculateViscousDragForce, other._calculateViscousDragForce)
            && ReferenceEquals(_calculateWaveMakingDragForce, other._calculateWaveMakingDragForce);
    }

    /// <inheritdoc/>
    public static bool operator ==(ShipDragPerformanceData data1, ShipDragPerformanceData data2)
    {
        return data1.Equals(data2);
    }

    /// <inheritdoc/>
    public static bool operator !=(ShipDragPerformanceData data1, ShipDragPerformanceData data2)
    {
        return !data1.Equals(data2);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(
            _lengthMultiplier,
            _formFactor,
            _buoyancyMultiplier,
            _massMultiplier,
            _viscousDragMultiplier,
            _waveMakingDragMultiplier,
            _calculateViscousDragForce,
            _calculateWaveMakingDragForce
        );
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return nameof(ShipDragPerformanceData) + "(" + FieldRepr() + ")";
    }

    internal readonly string FieldRepr()
    {
#if !DEBUG
        return "";
#else
        static string FuncRepr(DragForceFunction? func)
        {
            if (func is null)
            {
                return "";
            }

            var info = func.GetMethodInfo();
            return info.DeclaringType.Name + "." + info.Name;
        }
        return string.Join(
            ", ",
            $"FormFactor={_formFactor}",
            $"BuoyancyMultiplier={_buoyancyMultiplier}",
            $"ViscousDragMultiplier={_viscousDragMultiplier}",
            $"WaveMakingDragMultiplier={_waveMakingDragMultiplier}",
            $"MassMultiplier={_massMultiplier}",
            $"LengthMultiplier={_lengthMultiplier}",
            $"CalculateViscousDragForce={FuncRepr(_calculateViscousDragForce)}",
            $"CalculateWaveMakingDragForce={FuncRepr(_calculateWaveMakingDragForce)}"
        );
#endif
    }

    internal static ShipDragPerformanceData Merge(
        ShipDragPerformanceData highPriority,
        ShipDragPerformanceData lowPriority
    )
    {
        return new ShipDragPerformanceData(
            lengthMultiplier: highPriority._lengthMultiplier ?? lowPriority._lengthMultiplier,
            formFactor: highPriority._formFactor ?? lowPriority._formFactor,
            buoyancyMultiplier: highPriority._buoyancyMultiplier ?? lowPriority._buoyancyMultiplier,
            massMultiplier: highPriority._massMultiplier ?? lowPriority._massMultiplier,
            viscousDragMultiplier: highPriority._viscousDragMultiplier
                ?? lowPriority._viscousDragMultiplier,
            waveMakingDragMultiplier: highPriority._waveMakingDragMultiplier
                ?? lowPriority._waveMakingDragMultiplier,
            calculateViscousDragForce: highPriority._calculateViscousDragForce
                ?? lowPriority._calculateViscousDragForce,
            calculateWaveMakingDragForce: highPriority._calculateWaveMakingDragForce
                ?? lowPriority._calculateWaveMakingDragForce
        );
    }
};

/// <summary>
/// A class that manages custom and default ship configurations.
/// </summary>
public static class ShipDragConfigManager
{
    private static readonly Dictionary<string, ShipDragPerformanceData> UserPerformance = [];
    private static readonly Dictionary<string, ShipDragPerformanceData> CustomPerformance = [];

    /// <summary>
    /// Store custom performance data for a ship.
    /// <para>Existing data is overwritten.</para>
    /// </summary>
    /// <param name="shipName">The name of the ship object. Can be found in a <see  href="https://docs.google.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing">community spreadsheet</see>.</param>
    /// <param name="data">Ship's performance overrides.</param>
    /// <returns><c>true</c> if custom performance was successfully set.</returns>
    public static bool SetCustomPerformance(string? shipName, ShipDragPerformanceData? data)
    {
        if (shipName is null || data is null)
        {
            return false;
        }

        CustomPerformance[shipName] = data.Value;
        return true;
    }

    /// <summary>
    /// Return drag performance for a ship.
    /// <para>Priority: user config > custom performance > default performance.</para>
    /// </summary>
    internal static ShipDragPerformanceData GetPerformanceData(GameObject ship)
    {
        ShipDragPerformanceData? userData = GetPerformance(ship, UserPerformance);
        ShipDragPerformanceData? customData = GetPerformance(ship, CustomPerformance);
        ShipDragPerformanceData shipDefaultData = GetDefaultPerformance(ship);

        ShipDragPerformanceData mergedData = shipDefaultData;

        if (customData is not null)
        {
            mergedData = Merge(customData.Value, mergedData);
        }

        if (userData is not null)
        {
            mergedData = Merge(userData.Value, mergedData);
        }

#if DEBUG
        Utilities.BetterDragDebug.LogLinesBuffered([
            $"\nMerging data for: {ship.name}",
            $"User data: {userData}",
            $"Custom data: {customData}",
            $"Default data: {shipDefaultData}",
            $"Merged data: {mergedData}\n",
        ]);
#endif
        return mergedData;
    }

    internal static void FillUserPerformance(Dictionary<string, ShipDragPerformanceData> userConfig)
    {
        foreach (var item in userConfig)
        {
            UserPerformance.Add(Utilities.Utilities.StripCloneSuffix(item.Key), item.Value);
        }
    }

    internal static ShipDragPerformanceData GetDefaultPerformance(GameObject ship)
    {
        var shipName = Utilities.Utilities.GetNormalizedShipName(ship);
        return DefaultShipConfigurations.GetDefaultPerformanceByName(shipName);
    }

    private static ShipDragPerformanceData? GetPerformance(
        GameObject ship,
        Dictionary<string, ShipDragPerformanceData> store
    )
    {
        var isPresent = store.TryGetValue(
            Utilities.Utilities.GetNormalizedShipName(ship),
            out ShipDragPerformanceData data
        );
        return isPresent ? data : null;
    }
}
