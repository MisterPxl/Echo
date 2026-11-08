using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Playback behaviour of an <see cref="AudioBusAsset"/>, serialized by reference on the bus.
    /// Echo ships <see cref="SingleTrackPlayback"/> and <see cref="OneShotPoolPlayback"/>; derive
    /// from this class in any assembly to add another (crossfade, voice queue…). No enum, no registry.
    /// </summary>
    [Serializable]
    public abstract class AudioBusPlayback
    {
        /// <summary>
        /// Builds the runtime channel of <paramref name="bus"/>. Called once per service root; the
        /// channel's objects must live under <paramref name="parent"/> so they die with the root.
        /// </summary>
        public abstract AudioBusChannel CreateChannel(AudioBusAsset bus, Transform parent);
    }
}
