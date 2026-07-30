using Crest;

namespace BetterDrag.Utilities;

internal sealed class UnstickUpdateVelocity
{
    private int _counter;
    private const int UnstickOn = 20;

    internal void Update(BoatProbes boatProbes)
    {
        if (_counter > UnstickOn)
        {
            return;
        }

        if (_counter == UnstickOn)
        {
            boatProbes.dontUpdateVelocity = false;
        }

        ++_counter;
        return;
    }
}
