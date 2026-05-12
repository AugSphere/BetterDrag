using Crest;

namespace BetterDrag
{
    internal class UnstickUpdateVelocity
    {
        private int counter;
        const int unstickOn = 20;

        internal void Update(BoatProbes boatProbes)
        {
            if (counter > unstickOn)
                return;
            if (counter == unstickOn)
                boatProbes.dontUpdateVelocity = false;
            ++counter;
            return;
        }
    }
}
