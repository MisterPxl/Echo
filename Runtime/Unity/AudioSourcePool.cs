using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Pool of <see cref="AudioSource"/> components of a <see cref="OneShotPoolPlayback"/> bus.
    /// Reuses idle sources and grows on demand (capped by <c>maxSize</c> if set).
    /// Sources are reset before each use so caller-side spatial / pitch tweaks
    /// don't leak into the next playback.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioSourcePool")]
    public sealed class AudioSourcePool : MonoBehaviour
    {
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private AudioMixerGroup _defaultMixerGroup;
        private int _maxSize;

        internal void Setup(int initialSize, int maxSize, AudioMixerGroup defaultMixerGroup)
        {
            _defaultMixerGroup = defaultMixerGroup;
            _maxSize = maxSize;
            for (int i = 0; i < initialSize; i++)
                CreateSource();
        }

        /// <summary>
        /// Acquires a free source, configured with safe defaults and the default mixer group.
        /// Returns null only when the hard cap is reached and every slot is busy.
        /// </summary>
        internal AudioSource Acquire()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                AudioSource s = _sources[i];
                if (!s.isPlaying)
                {
                    ResetSource(s);
                    return s;
                }
            }

            if (_maxSize > 0 && _sources.Count >= _maxSize)
                return null;

            AudioSource created = CreateSource();
            ResetSource(created);
            return created;
        }

        internal bool IsAnyPlaying
        {
            get
            {
                for (int i = 0; i < _sources.Count; i++)
                {
                    if (_sources[i].isPlaying) return true;
                }
                return false;
            }
        }

        internal void StopAll()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].isPlaying) _sources[i].Stop();
            }
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.outputAudioMixerGroup = _defaultMixerGroup;
            // Adding an AudioSource can schedule play-on-awake before the flag is cleared.
            source.Stop();
            _sources.Add(source);
            return source;
        }

        private void ResetSource(AudioSource source)
        {
            source.clip = null;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.transform.localPosition = Vector3.zero;
            source.outputAudioMixerGroup = _defaultMixerGroup;
        }
    }
}
