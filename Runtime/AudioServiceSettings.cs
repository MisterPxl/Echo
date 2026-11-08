using System.Collections.Generic;

namespace Astra.Echo
{
    /// <summary>
    /// Everything <see cref="AudioService.Initialize"/> needs to build the audio root:
    /// database, Master volume, the buses to prepare and the bus of raw clips.
    /// Plain data, framework-free: adapters convert their own serialized configuration into it.
    /// </summary>
    public sealed class AudioServiceSettings
    {
        public string RootName = "AudioRoot";
        public AudioDatabase Database;
        public float MasterVolume = 1f;

        /// <summary>Buses whose channel is created at initialization. A bus first met through a definition is created on demand.</summary>
        public List<AudioBusAsset> Buses = new List<AudioBusAsset>();

        /// <summary>Bus of <see cref="IRawClipAudioService.PlaySfx"/> clips that no definition describes.</summary>
        public AudioBusAsset RawClipBus;
    }
}
