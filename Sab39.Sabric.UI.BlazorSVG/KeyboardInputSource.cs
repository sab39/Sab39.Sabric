using Sab39.Sabric.Engine.Aether;

using nkast.Aether.Physics2D.Common;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Turns a live set of pressed browser key codes into a movement direction. The set is held by
/// whoever is capturing the key events; this only reads it.
/// </summary>
public sealed class KeyboardInputSource(IReadOnlySet<string> keyStates, params IEnumerable<(string Key, Vector2 Direction)> mappings)
    : IPlayerInputSource
{
    public KeyboardInputSource(IReadOnlySet<string> keyStates, string upKey, string downKey, string leftKey, string rightKey)
        : this(keyStates, (upKey, Vector2.North), (downKey, Vector2.South), (leftKey, Vector2.West), (rightKey, Vector2.East))
    {
    }

    public IReadOnlySet<string> KeyStates { get; } = keyStates;
    public IReadOnlyDictionary<string, Vector2> Mappings { get; } = mappings.ToDictionary(m => m.Key, m => m.Direction);

    public Vector2 MovementDirection => Mappings.Where(m => KeyStates.Contains(m.Key)).Sum(m => m.Value);
}
