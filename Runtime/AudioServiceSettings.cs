using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Everything <see cref="AudioService.Initialize"/> needs to build the audio root:
    /// database, default volumes, SFX pool sizing and optional mixer routing.
    /// Plain data, framework-free: adapters convert their own serialized configuration into it.
    /// </summary>
    public sealed class AudioServiceSettings
    {
        public string RootName = "AudioRoot";
        public AudioDatabase Database;
        public AudioVolumesDefaults Volumes = new AudioVolumesDefaults(1f, 0.8f, 1f, 0.7f);
        public int SfxPoolInitialSize = 8;
        public int SfxPoolMaxSize = 32;
        public AudioMixerGroup MusicMixerGroup;
        public AudioMixerGroup SfxMixerGroup;
        public AudioMixerGroup AmbientMixerGroup;
    }
}
