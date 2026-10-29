using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Forces a specific <see cref="AudioMixerGroup"/> on the source, overriding both
    /// the AudioService-resolved routing and any <see cref="AudioDefinition.MixerGroupOverride"/>.
    /// Set the field to <c>null</c> to actively unroute the source from any group.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "MixerGroupOverrideOption")]
    public sealed class MixerGroupOverrideOption : SoundOptionBase
    {
        [SerializeField] private AudioMixerGroup _mixerGroup;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.outputAudioMixerGroup = _mixerGroup;
        }
    }
}
