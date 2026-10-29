using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Weighted random selection between several clips. Heavier weights are picked more often.
    /// Total weight is computed every call (cheap, n is small in practice) so designers can
    /// tweak weights live without rebuilding any cache.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "WeightedRandomClipModulator")]
    public sealed class WeightedRandomClipModulator : SoundModulatorBase
    {
        [Serializable]
        public struct WeightedClip
        {
            public AudioClip Clip;
            [Min(0f)] public float Weight;
        }

        [SerializeField] private List<WeightedClip> _entries = new List<WeightedClip>();

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            if (_entries == null || _entries.Count == 0) return;

            float total = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Clip != null) total += Mathf.Max(0f, _entries[i].Weight);
            }
            if (total <= 0f) return;

            float roll = UnityEngine.Random.value * total;
            float accumulator = 0f;

            for (int i = 0; i < _entries.Count; i++)
            {
                WeightedClip entry = _entries[i];
                if (entry.Clip == null || entry.Weight <= 0f) continue;

                accumulator += entry.Weight;
                if (roll <= accumulator)
                {
                    context.ClipOverride = entry.Clip;
                    return;
                }
            }
        }
    }
}
