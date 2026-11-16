using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Track player of a <see cref="SingleTrackPlayback"/> bus.
    /// Uses a staging <see cref="AudioSource"/> for the next playback and promotes it to the
    /// active source only after a successful start, so vetoed requests never disturb the track.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioPlayer")]
    public sealed class AudioPlayer : MonoBehaviour, IAudioHandle
    {
        private AudioSource _active;
        private AudioSource _staging;
        private AudioMixerGroup _defaultMixerGroup;

        public bool IsPlaying => _active != null && _active.isPlaying;
        public AudioClip CurrentClip => _active != null ? _active.clip : null;

        /// <summary>Staging source passed to <see cref="AudioService"/> for configuration.</summary>
        internal AudioSource Source => _staging;

        internal AudioSource ActiveSource => _active;

        internal void Setup(AudioMixerGroup defaultMixerGroup)
        {
            _defaultMixerGroup = defaultMixerGroup;
            _active = CreateSource();
            _staging = CreateSource();
        }

        /// <summary>Called by the channel after a successful <see cref="AudioSource.Play"/> on staging.</summary>
        internal void Promote(AudioSource source)
        {
            if (source != _staging)
                return;

            if (_active != null && _active.isPlaying)
                _active.Stop();

            AudioSource previousActive = _active;
            _active = _staging;
            _staging = previousActive;
            ResetSource(_staging);
        }

        internal void DiscardStaging()
        {
            if (_staging == null)
                return;

            ResetSource(_staging);
        }

        public void Stop()
        {
            if (_active != null)
            {
                if (_active.isPlaying)
                    _active.Stop();
                _active.clip = null;
            }

            if (_staging != null)
            {
                if (_staging.isPlaying)
                    _staging.Stop();
                _staging.clip = null;
            }
        }

        internal void SetVolume(float volume)
        {
            if (_active != null)
                _active.volume = volume;
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.outputAudioMixerGroup = _defaultMixerGroup;
            source.Stop();
            return source;
        }

        private void ResetSource(AudioSource source)
        {
            if (source == null)
                return;

            if (source.isPlaying)
                source.Stop();

            source.clip = null;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
        }
    }
}