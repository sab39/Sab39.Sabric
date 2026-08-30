namespace Sab39.Sabric.Engine;

/// <summary>
/// One playthrough: the tick loop, and the space currently being played.
/// </summary>
/// <remarks>
/// Ticks are driven from outside and carry the caller's timestamp, so the session measures no time
/// of its own - whatever is scheduling frames decides what a tick is worth.
///
/// A session outlives the spaces it plays through, which is the whole reason it is a separate
/// thing: a level transition tears down a space and everything in it while the session goes on.
/// </remarks>
public abstract class GameSessionBase
{
    public int Ticks { get; private set; }
    public long FirstTickStamp { get; private set; }
    public long LastTickStamp { get; private set; }
    public long Delta { get; private set; }
    public long TotalMillis => LastTickStamp - FirstTickStamp;

    public abstract GameSpaceBase CurrentSpace { get; }

    protected virtual void OnInit() { }
    public void Init()
    {
        Ticks = 0;
        OnInit();
    }

    /// <summary>
    /// Raised once per tick, after the tick has been fully applied.
    /// </summary>
    /// <remarks>
    /// Deliberately not raised by an OnTicked method. The OnXyz convention means "the protected
    /// virtual that raises event Xyz", and OnTick is a plain lifecycle hook with no event behind
    /// it; leaving the raiser out keeps the two from being confused for each other.
    /// </remarks>
    public event EventHandler? Ticked;

    protected virtual void OnTick(long tickStamp) => CurrentSpace.Advance(Delta);
    public void Tick(long tickStamp)
    {
        if (Ticks == 0)
        {
            FirstTickStamp = tickStamp;
            LastTickStamp = tickStamp;
        }
        Delta = tickStamp - LastTickStamp;

        OnTick(tickStamp);

        Ticks++;
        LastTickStamp = tickStamp;

        Ticked?.Invoke(this, EventArgs.Empty);
    }
}
