using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Static, declarative tweak applied to an <see cref="AudioSource"/> right before it plays.
    /// Options are resolved once per playback (no per-frame logic).
    /// Implementations must be safe to apply multiple times on different sources.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "ISoundOption")]
    public interface ISoundOption
    {
        void Apply(AudioSource source, AudioPlaybackContext context);
    }
}
