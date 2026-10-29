using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Single-channel player used for music or ambient tracks.
    /// Wraps one persistent <see cref="AudioSource"/> and exposes
    /// minimal play / stop / volume controls. Implements
    /// <see cref="IAudioHandle"/> so callers can keep stopping the active track.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioPlayer")]
    public sealed class AudioPlayer : MonoBehaviour, IAudioHandle
    {
        private AudioSource _source;
        private AudioMixerGroup _defaultMixerGroup;

        public bool IsPlaying => _source != null && _source.isPlaying;
        public AudioClip CurrentClip => _source != null ? _source.clip : null;

        internal void Setup(AudioMixerGroup defaultMixerGroup)
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _defaultMixerGroup = defaultMixerGroup;
            _source.outputAudioMixerGroup = defaultMixerGroup;
            // Adding an AudioSource can schedule play-on-awake before the flag is cleared.
            _source.Stop();
        }

        internal void Play(AudioClip clip, bool loop, float volume, float pitch, AudioMixerGroup mixerGroup)
        {
            _source.clip = clip;
            _source.loop = loop;
            _source.volume = volume;
            _source.pitch = pitch;
            _source.outputAudioMixerGroup = mixerGroup != null ? mixerGroup : _defaultMixerGroup;
            _source.Play();
        }

        public void Stop()
        {
            if (_source == null) return;
            if (_source.isPlaying) _source.Stop();
            _source.clip = null;
        }

        internal void SetVolume(float volume)
        {
            if (_source != null) _source.volume = volume;
        }
    }
}
