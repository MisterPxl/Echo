using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Runtime side of an <see cref="AudioBusPlayback"/>: hands <see cref="AudioService"/> the
    /// source of each playback on one bus. The service then configures the source (clip, volume,
    /// pitch, mixer group, modulators, options) and starts it.
    /// </summary>
    public abstract class AudioBusChannel
    {
        protected AudioBusChannel(AudioBusAsset bus)
        {
            Bus = bus != null ? bus : throw new ArgumentNullException(nameof(bus));
        }

        public AudioBusAsset Bus { get; }

        public abstract bool IsPlaying { get; }

        /// <summary>True when the definition's spatial settings and the request position apply to this channel's sources.</summary>
        public abstract bool IsSpatial { get; }

        /// <summary>
        /// Returns the source of the next playback, or null with a <paramref name="failure"/> reason
        /// when the channel cannot play now (for example a saturated pool).
        /// </summary>
        public abstract AudioSource Acquire(out string failure);

        /// <summary>Handle returned to the caller once <paramref name="source"/> has started; null when the playback is not addressable.</summary>
        public abstract IAudioHandle HandleFor(AudioSource source);

        /// <summary>Called with Master × bus volume whenever either changes.</summary>
        public abstract void OnVolumeChanged(float effectiveVolume);

        public abstract void Stop();
    }
}
