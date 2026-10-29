namespace Astra.Echo
{
    /// <summary>
    /// Plain-data carrier for default audio channel volumes.
    /// Allows any configuration source (framework adapter, <see cref="AudioServiceSettings"/>)
    /// to feed <see cref="AudioService"/> through a single contract.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioVolumesDefaults")]
    public readonly struct AudioVolumesDefaults
    {
        public float Master { get; }
        public float Music { get; }
        public float Sfx { get; }
        public float Ambient { get; }

        public AudioVolumesDefaults(float master, float music, float sfx, float ambient)
        {
            Master = master;
            Music = music;
            Sfx = sfx;
            Ambient = ambient;
        }
    }
}
