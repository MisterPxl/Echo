namespace Astra.Echo
{
    /// <summary>
    /// Logical role of an audio definition. Drives default routing
    /// (mixer group + playback channel) inside <see cref="AudioService"/>.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioType")]
    public enum AudioType
    {
        Sfx = 0,
        Music = 1,
        Ambient = 2
    }
}
