using System;

namespace Helmsman.Core;

public enum BoardingState { GracePeriod, WaitingForPlayer, FinalCountdown, Ready }

/// <summary>Uses a monotonic game clock. An absent passenger never authorizes departure.</summary>
public sealed class BoardingGate
{
    private readonly double graceEnd;
    private readonly double finalSeconds;
    private double? finalEnd;
    private bool waitedForPlayer;
    public BoardingState State { get; private set; }
    public double Remaining { get; private set; }

    public BoardingGate(double now, double graceSeconds = 10, double finalSeconds = 3)
    {
        graceEnd = now + Math.Max(0, graceSeconds);
        this.finalSeconds = Math.Max(0, finalSeconds);
    }

    public bool Update(double now, bool aboard)
    {
        Remaining = Math.Max(0, graceEnd - now);
        if (now < graceEnd) { State = BoardingState.GracePeriod; return false; }
        if (!aboard)
        {
            waitedForPlayer = true;
            finalEnd = null;
            State = BoardingState.WaitingForPlayer;
            return false;
        }
        if (waitedForPlayer)
        {
            if (!finalEnd.HasValue) finalEnd = now + finalSeconds;
            Remaining = Math.Max(0, finalEnd.Value - now);
            if (Remaining > 0) { State = BoardingState.FinalCountdown; return false; }
        }
        State = BoardingState.Ready;
        return true;
    }
}
