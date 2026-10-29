using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Mutates an <see cref="AudioSource"/>'s playback parameters (pitch, volume, etc.)
    /// at the moment of triggering. Modulators MAY hold internal state across calls
    /// (e.g. an incremental pitch modulator), so they should be reused — not recreated
    /// per playback — when stateful behaviour is desired.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "ISoundModulator")]
    public interface ISoundModulator
    {
        void Modulate(AudioSource source, AudioPlaybackContext context);
    }
}
