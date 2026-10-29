using UnityEngine;

namespace Astra.Echo
{
    /// <summary>Optional capability for playing clips without requiring a registered audio ID.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IRawClipAudioService")]
    public interface IRawClipAudioService
    {
        AudioPlaybackResult PlaySfx(AudioClip clip, float volumeScale = 1f);
    }
}
