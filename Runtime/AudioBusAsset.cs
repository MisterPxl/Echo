using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// A volume bus under Master: default volume, mixer routing and playback behaviour of every
    /// <see cref="AudioDefinition"/> that references it. Master applies on top of every bus.
    ///
    /// <para>Echo ships Music, Sfx and Ambient buses (<c>Runtime/Buses</c>); a project adds its own
    /// (Voice, UI…) by creating another asset. The service creates a bus the first time it is met,
    /// so a bus needs no registration; list it in <see cref="AudioServiceSettings.Buses"/> to have
    /// its sources ready at initialization.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "AudioBus", menuName = "Astra/Echo/Audio Bus")]
    public sealed class AudioBusAsset : ScriptableObject
    {
        [Tooltip("Name shown by settings screens and logs. Defaults to the asset name when empty.")]
        [SerializeField] private string _displayName;

        [Tooltip("Key under which a settings layer persists this bus volume. Defaults to the asset name when empty.")]
        [SerializeField] private string _volumeKey;

        [Range(0f, 1f)] [SerializeField] private float _defaultVolume = 1f;

        [Tooltip("Output group of every source played on this bus, unless the definition overrides it.")]
        [SerializeField] private AudioMixerGroup _mixerGroup;

        [Tooltip("How playback requests become sources: one replaced track, pooled one-shots, or a project behaviour.")]
        [SerializeReference] private AudioBusPlayback _playback = new OneShotPoolPlayback();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string VolumeKey => string.IsNullOrEmpty(_volumeKey) ? name : _volumeKey;
        public float DefaultVolume => Mathf.Clamp01(_defaultVolume);
        public AudioMixerGroup MixerGroup => _mixerGroup;
        public AudioBusPlayback Playback => _playback;

        /// <summary>Test and tooling hook: configures a bus created with <c>ScriptableObject.CreateInstance</c>.</summary>
        internal void Configure(string displayName, AudioBusPlayback playback, float defaultVolume = 1f, AudioMixerGroup mixerGroup = null, string volumeKey = null)
        {
            _displayName = displayName;
            _playback = playback;
            _defaultVolume = defaultVolume;
            _mixerGroup = mixerGroup;
            _volumeKey = volumeKey;
        }
    }
}
