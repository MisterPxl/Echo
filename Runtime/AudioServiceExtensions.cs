using UnityEngine;

namespace Astra.Echo
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioServiceExtensions")]
    public static class AudioServiceExtensions
    {
        /// <summary>Preserves the interface call syntax while keeping raw playback optional.</summary>
        public static AudioPlaybackResult PlaySfx(this IAudioService service, AudioClip clip, float volumeScale = 1f)
        {
            if (service == null) return AudioPlaybackResult.Fail("IAudioService not available.");
            if (service is IRawClipAudioService raw) return raw.PlaySfx(clip, volumeScale);
            return AudioPlaybackResult.Fail("This audio service does not support raw clips. Implement IRawClipAudioService or install a RawClipAudioServiceAdapter.");
        }
    }
}
