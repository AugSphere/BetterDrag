using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using static BetterDrag.ShipDragPerformanceData;
#if DEBUG
using System.Reflection;
#endif

namespace BetterDrag
{
    /// <summary>
    /// A struct holding drag performance setting overrides for a single ship.
    ///
    /// <para>
    /// All entries are optional, leave <c>null</c> for the ones you do not want to override.
    /// </para>
    ///
    /// <para>
    /// All units are metric. Unit reference: cog's LWL is approximately 12.39m.
    /// </para>
    /// </summary>
    [DataContract]
    public readonly struct ShipDragPerformanceData(
        float? lengthAtWaterline = null,
        float? formFactor = null,
        float? buoyancyMultiplier = null,
        float? massMultiplier = null,
        float? viscousDragMultiplier = null,
        float? waveMakingDragMultiplier = null,
        DragForceFunction? calculateViscousDragForce = null,
        DragForceFunction? calculateWaveMakingDragForce = null
    ) : IEquatable<ShipDragPerformanceData>
    {
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? lengthAtWaterline = lengthAtWaterline;

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? formFactor = formFactor;

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? buoyancyMultiplier = buoyancyMultiplier;

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? massMultiplier = massMultiplier;

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? viscousDragMultiplier = viscousDragMultiplier;

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        private readonly float? waveMakingDragMultiplier = waveMakingDragMultiplier;

        [NonSerialized]
        private readonly DragForceFunction? calculateViscousDragForce = calculateViscousDragForce;

        [NonSerialized]
        private readonly DragForceFunction? calculateWaveMakingDragForce =
            calculateWaveMakingDragForce;

        /// <summary>
        /// Length of the hull at waterline in meters.
        /// </summary>
        public readonly float LengthAtWaterline =>
            this.lengthAtWaterline ?? placeholderData.LengthAtWaterline;

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
        public readonly float FormFactor => this.formFactor ?? placeholderData.FormFactor;

        /// <summary>
        /// Ship-specific buoyancy multiplier.
        /// </summary>
        public readonly float BuoyancyMultiplier =>
            this.buoyancyMultiplier ?? placeholderData.BuoyancyMultiplier;

        /// <summary>
        /// Ship-specific mass multiplier.
        /// </summary>
        public readonly float MassMultiplier =>
            this.massMultiplier ?? placeholderData.MassMultiplier;

        /// <summary>
        /// Ship-specific drag multiplier for viscous resistance.
        /// </summary>
        public readonly float ViscousDragMultiplier =>
            this.viscousDragMultiplier ?? placeholderData.ViscousDragMultiplier;

        /// <summary>
        /// Ship-specific drag multiplier for wave-making resistance.
        /// </summary>
        public readonly float WaveMakingDragMultiplier =>
            this.waveMakingDragMultiplier ?? placeholderData.WaveMakingDragMultiplier;

        /// <summary>
        /// Custom force function type.
        /// </summary>
        /// <param name="forwardVelocity">Absolute forward component of ship velocity in default unity meters/second.</param>
        /// <param name="lengthAtWaterline">Length at waterline in meters. Specified in ship's configuration.</param>
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
            this.calculateViscousDragForce ?? placeholderData.CalculateViscousDragForce;

        /// <summary>
        /// Same as <see cref="CalculateViscousDragForce"/>, but for wave-making drag.
        /// </summary>
        public readonly DragForceFunction CalculateWaveMakingDragForce =>
            this.calculateWaveMakingDragForce ?? placeholderData.CalculateWaveMakingDragForce;

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            if (obj is not ShipDragPerformanceData)
                return false;

            return Equals((ShipDragPerformanceData)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ShipDragPerformanceData other)
        {
            if (lengthAtWaterline != other.lengthAtWaterline)
                return false;
            if (formFactor != other.formFactor)
                return false;
            if (buoyancyMultiplier != other.buoyancyMultiplier)
                return false;
            if (massMultiplier != other.massMultiplier)
                return false;
            if (viscousDragMultiplier != other.viscousDragMultiplier)
                return false;
            if (waveMakingDragMultiplier != other.waveMakingDragMultiplier)
                return false;
            if (!ReferenceEquals(calculateViscousDragForce, other.calculateViscousDragForce))
                return false;
            if (!ReferenceEquals(calculateWaveMakingDragForce, other.calculateWaveMakingDragForce))
                return false;

            return true;
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
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(
                lengthAtWaterline,
                formFactor,
                buoyancyMultiplier,
                massMultiplier,
                viscousDragMultiplier,
                waveMakingDragMultiplier,
                calculateViscousDragForce,
                calculateWaveMakingDragForce
            );
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return nameof(ShipDragPerformanceData) + "(" + this.FieldRepr() + ")";
        }

        internal readonly string FieldRepr()
        {
#if !DEBUG
            return "";
#else
            static string FuncRepr(DragForceFunction? func)
            {
                if (func is null)
                    return "";

                var info = func.GetMethodInfo();
                return info.DeclaringType.Name + "." + info.Name;
            }
            return String.Join(
                ", ",
                $"LWL={this.lengthAtWaterline}",
                $"FormFactor={this.formFactor}",
                $"BuoyancyMultiplier={this.buoyancyMultiplier}",
                $"MassMultiplier={this.massMultiplier}",
                $"ViscousDragMultiplier={this.viscousDragMultiplier}",
                $"WaveMakingDragMultiplier={this.waveMakingDragMultiplier}",
                $"CalculateViscousDragForce={FuncRepr(this.calculateViscousDragForce)}",
                $"CalculateWaveMakingDragForce={FuncRepr(this.calculateWaveMakingDragForce)}"
            );
#endif
        }

        internal static ShipDragPerformanceData Merge(
            ShipDragPerformanceData highPriority,
            ShipDragPerformanceData lowPriority
        )
        {
            return new ShipDragPerformanceData(
                lengthAtWaterline: highPriority.lengthAtWaterline ?? lowPriority.lengthAtWaterline,
                formFactor: highPriority.formFactor ?? lowPriority.formFactor,
                buoyancyMultiplier: highPriority.buoyancyMultiplier
                    ?? lowPriority.buoyancyMultiplier,
                massMultiplier: highPriority.massMultiplier ?? lowPriority.massMultiplier,
                viscousDragMultiplier: highPriority.viscousDragMultiplier
                    ?? lowPriority.viscousDragMultiplier,
                waveMakingDragMultiplier: highPriority.waveMakingDragMultiplier
                    ?? lowPriority.waveMakingDragMultiplier,
                calculateViscousDragForce: highPriority.calculateViscousDragForce
                    ?? lowPriority.calculateViscousDragForce,
                calculateWaveMakingDragForce: highPriority.calculateWaveMakingDragForce
                    ?? lowPriority.calculateWaveMakingDragForce
            );
        }

        internal static readonly ShipDragPerformanceData placeholderData = new(
            lengthAtWaterline: 15f,
            formFactor: 0.15f,
            buoyancyMultiplier: 0.12f,
            massMultiplier: 1f,
            viscousDragMultiplier: 1.0f,
            waveMakingDragMultiplier: 1.0f,
            calculateViscousDragForce: DragModel.CalculateViscousDragForce,
            calculateWaveMakingDragForce: DragModel.CalculateWaveMakingDragForce
        );
    };

    /// <summary>
    /// A class that manages custom and default ship configurations.
    /// </summary>
    public static class ShipDragConfigManager
    {
        private static Dictionary<String, ShipDragPerformanceData> userPerformance = [];
        private static readonly Dictionary<String, ShipDragPerformanceData> customPerformance = [];

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
                return false;
            customPerformance[shipName] = data.Value;
            return true;
        }

        /// <summary>
        /// Return drag performance for a ship.
        /// <para>Priority: user config > custom performance > default performance.</para>
        /// </summary>
        internal static ShipDragPerformanceData GetPerformanceData(GameObject ship)
        {
            ShipDragPerformanceData? userData = GetPerformance(ship, userPerformance);
            ShipDragPerformanceData? customData = GetPerformance(ship, customPerformance);
            ShipDragPerformanceData shipDefaultData = GetDefaultPerformance(ship);

            ShipDragPerformanceData mergedData = shipDefaultData;

            if (customData is not null)
                mergedData = ShipDragPerformanceData.Merge(customData.Value, mergedData);

            if (userData is not null)
                mergedData = ShipDragPerformanceData.Merge(userData.Value, mergedData);

#if DEBUG
            BetterDragDebug.LogLinesBuffered(
                [
                    $"\nMerging data for: {ship.name}",
                    $"User data: {userData}",
                    $"Custom data: {customData}",
                    $"Default data: {shipDefaultData}",
                    $"Merged data: {mergedData}\n",
                ]
            );
#endif
            return mergedData;
        }

        internal static void FillUserPerformance(
            Dictionary<string, ShipDragPerformanceData> userConfig
        )
        {
            userPerformance = userConfig;
        }

        internal static ShipDragPerformanceData GetDefaultPerformance(GameObject ship)
        {
            var shipName = GetNormalizedShipName(ship);
            return GetDefaultPerformanceByName(shipName);
        }

        internal static ShipDragPerformanceData GetDefaultPerformanceByName(string shipName)
        {
            return (shipName) switch
            {
                "BOAT dhow small (10)" => new(12.76f, 0.25f, 0.08f),
                "BOAT dhow medium (20)" => new(21.03f, 0.21f, 0.10f),
                "BOAT medi small (40)" => new(12.39f, 0.24f, 0.07f),
                "BOAT medi medium (50)" => new(24.83f, 0.19f, 0.17f),
                "BOAT junk large (70)" => new(29.9f, 0.23f, 0.15f),
                "BOAT junk medium (80)" => new(21.8f, 0.22f, 0.09f),
                "BOAT junk small singleroof(90)" => new(11.07f, 0.23f, 0.09f),
                "BOAT Shroud Small" => new(14.77f, 0.8f, 0.16f, 0.9f, 0.95f),
                "BOAT Shroud Large" => new(34.56f, 0.6f, 0.16f, 0.9f, 0.9f),
                "BOAT GLORIANA (182)" => new(24.6f, 0.18f, 0.09f),
                "BOAT CHRONIAN (187)" => new(36f, 0.20f, 0.09f),
                "BOAT CAELANOR (192)" => new(18f, 0.22f, 0.13f),
                "BOAT GALLUS (197)" => new(9.2f, 0.15f, 0.10f),
                "BOAT Le Requin (131)" => new(36.8f, 0.10f, 0.10f),
                _ => new(),
            };
        }

        private static string GetNormalizedShipName(GameObject ship)
        {
            var shipName = ship.name;
            var suffix = "(Clone)";

            while (shipName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                shipName = shipName.Substring(0, shipName.Length - suffix.Length);
            }
            return shipName;
        }

        private static ShipDragPerformanceData? GetPerformance(
            GameObject ship,
            Dictionary<String, ShipDragPerformanceData> store
        )
        {
            var isPresent = store.TryGetValue(
                GetNormalizedShipName(ship),
                out ShipDragPerformanceData data
            );
            return isPresent ? data : null;
        }
    }
}
