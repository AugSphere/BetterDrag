namespace BetterDrag
{
    internal static class DefaultShipConfigurations
    {
        internal static readonly ShipDragPerformanceData baseShipConfiguration = new(
            lengthMultiplier: 1.0f,
            formFactor: 0.15f,
            buoyancyMultiplier: 0.12f,
            massMultiplier: 1f,
            viscousDragMultiplier: 1.0f,
            waveMakingDragMultiplier: 1.0f,
            calculateViscousDragForce: DragModel.CalculateViscousDragForce,
            calculateWaveMakingDragForce: DragModel.CalculateWaveMakingDragForce
        );

        internal static ShipDragPerformanceData GetDefaultPerformanceByName(string shipName)
        {
            return (shipName) switch
            {
                "BOAT dhow small (10)" => new(formFactor: 0.25f, buoyancyMultiplier: 0.08f),
                "BOAT dhow medium (20)" => new(formFactor: 0.21f, buoyancyMultiplier: 0.10f),
                "BOAT medi small (40)" => new(formFactor: 0.24f, buoyancyMultiplier: 0.07f),
                "BOAT medi medium (50)" => new(formFactor: 0.19f, buoyancyMultiplier: 0.17f),
                "BOAT junk large (70)" => new(formFactor: 0.23f, buoyancyMultiplier: 0.15f),
                "BOAT junk medium (80)" => new(formFactor: 0.22f, buoyancyMultiplier: 0.09f),
                "BOAT junk small singleroof(90)" => new(
                    formFactor: 0.23f,
                    buoyancyMultiplier: 0.09f
                ),
                "BOAT Shroud Small" => new(
                    formFactor: 0.8f,
                    buoyancyMultiplier: 0.16f,
                    viscousDragMultiplier: 0.8f,
                    waveMakingDragMultiplier: 0.95f
                ),
                "BOAT Shroud Large" => new(
                    formFactor: 0.6f,
                    buoyancyMultiplier: 0.16f,
                    viscousDragMultiplier: 0.8f,
                    waveMakingDragMultiplier: 0.9f
                ),
                "BOAT Shroud Small (160)" => new(formFactor: 0.12f, buoyancyMultiplier: 0.08f),
                "BOAT GLORIANA (182)" => new(formFactor: 0.18f, buoyancyMultiplier: 0.09f),
                "BOAT CHRONIAN (187)" => new(formFactor: 0.20f, buoyancyMultiplier: 0.09f),
                "BOAT CAELANOR (192)" => new(formFactor: 0.22f, buoyancyMultiplier: 0.13f),
                "BOAT GALLUS (197)" => new(formFactor: 0.15f, buoyancyMultiplier: 0.10f),
                "BOAT Le Requin (131)" => new(formFactor: 0.10f, buoyancyMultiplier: 0.10f),
                "BOAT LEOPARD (207)" => new(formFactor: 0.20f, buoyancyMultiplier: 0.065f),
                _ => new(),
            };
        }
    }
}
