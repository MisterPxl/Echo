namespace Astra.Echo
{
    /// <summary>
    /// Lightweight handle to an active audio playback.
    /// Can be used to query status or stop playback later.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IAudioHandle")]
    public interface IAudioHandle
    {
        bool IsPlaying { get; }
        void Stop();
    }
}
