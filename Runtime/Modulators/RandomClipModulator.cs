using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Replaces the played clip with a random one picked from <see cref="_clips"/>.
    /// When the list is empty, the original definition's clip is kept.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RandomClipModulator")]
    public sealed class RandomClipModulator : SoundModulatorBase
    {
        [SerializeField] private List<AudioClip> _clips = new List<AudioClip>();
        [Tooltip("If true, never returns the same clip twice in a row (when there are at least 2 clips).")]
        [SerializeField] private bool _avoidImmediateRepeat = true;

        [NonSerialized] private int _lastIndex = -1;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            if (_clips == null || _clips.Count == 0) return;

            int count = _clips.Count;
            int index = UnityEngine.Random.Range(0, count);

            if (_avoidImmediateRepeat && count > 1 && index == _lastIndex)
                index = (index + 1) % count;

            _lastIndex = index;
            AudioClip pick = _clips[index];
            if (pick != null) context.ClipOverride = pick;
        }
    }
}
