using System;

namespace Helmsman.Core;

public static class HelmMath
{
    // Separate entry/exit angles keep small heading changes from repeatedly dropping sail.
    public static bool NeedsSlowTurn(double headingError, bool alreadySlowing) =>
        Math.Abs(headingError) > (alreadySlowing ? 55 : 80);

    public static double StoppingDistance(double speed, double deceleration, double reactionSeconds, double margin)
    {
        if (deceleration <= 0) throw new ArgumentOutOfRangeException(nameof(deceleration));
        speed = Math.Abs(speed);
        return speed*reactionSeconds + speed*speed/(2*deceleration) + margin;
    }

    // Positive heading error is starboard; reverse movement changes steering response.
    public static double Rudder(double headingError, bool reverse) =>
        Math.Max(-0.65,Math.Min(0.65,headingError / 55.0)) * (reverse ? -1 : 1);
}
