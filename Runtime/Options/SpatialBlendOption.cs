using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Forces a specific spatial blend on the source: 0 = pure 2D, 1 = pure 3D.
    /// Overrides the value from <see cref="AudioDefinition.SpatialBlend"/>.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "SpatialBlendOption")]
    public sealed class SpatialBlendOption : SoundOptionBase
    {
        [SerializeField, Range(0f, 1f)] private float _blend;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.spatialBlend = _blend;
        }
    }
}
