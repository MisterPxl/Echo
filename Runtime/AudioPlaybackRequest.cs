using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Structured request for playing an <see cref="AudioDefinition"/>.
    /// Carries optional caller-side overrides that take precedence over the definition's defaults.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioPlaybackRequest")]
    public struct AudioPlaybackRequest
    {
        public AudioId Id;

        /// <summary>Optional: when set, used as-is instead of looking up the database.</summary>
        public AudioDefinition Definition;

        public float? VolumeScale;
        public float? PitchOverride;
        public bool? LoopOverride;
        public Vector3? WorldPosition;

        public List<ISoundOption> Options;
        public List<ISoundModulator> Modulators;

        public static AudioPlaybackRequest ForId(AudioId id)
        {
            return new AudioPlaybackRequest { Id = id };
        }

        public static AudioPlaybackRequest ForDefinition(AudioDefinition definition)
        {
            return new AudioPlaybackRequest
            {
                Id = definition != null ? definition.Id : AudioId.None,
                Definition = definition
            };
        }
    }
}
