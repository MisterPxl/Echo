using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Picks a random pitch multiplier in <c>[Min, Max]</c> and multiplies the source pitch.
    /// The base pitch comes from <see cref="AudioPlaybackContext.BasePitch"/>, so combining
    /// this with other pitch modulators behaves multiplicatively.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RandomPitchModulator")]
    public sealed class RandomPitchModulator : SoundModulatorBase
    {
        [SerializeField, Range(0.1f, 3f)] private float _min = 0.95f;
        [SerializeField, Range(0.1f, 3f)] private float _max = 1.05f;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            float min = Mathf.Min(_min, _max);
            float max = Mathf.Max(_min, _max);
            float multiplier = UnityEngine.Random.Range(min, max);
            source.pitch = context.BasePitch * multiplier;
        }
    }
}
