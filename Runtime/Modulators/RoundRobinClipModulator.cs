using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Cycles deterministically through <see cref="_clips"/>, returning each entry in order
    /// before wrapping around. Good for ordered variants (counted footsteps, dialog beats).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RoundRobinClipModulator")]
    public sealed class RoundRobinClipModulator : SoundModulatorBase
    {
        [SerializeField] private List<AudioClip> _clips = new List<AudioClip>();

        [NonSerialized] private int _index;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            if (_clips == null || _clips.Count == 0) return;

            int count = _clips.Count;
            int safeIndex = ((_index % count) + count) % count; // robust to negative state.

            AudioClip pick = _clips[safeIndex];
            if (pick != null) context.ClipOverride = pick;

            _index = (safeIndex + 1) % count;
        }
    }
}
