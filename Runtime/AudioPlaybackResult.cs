using System;

namespace Astra.Echo
{
    /// <summary>
    /// Structured result of an audio playback operation.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioPlaybackResult")]
    public sealed class AudioPlaybackResult
    {
        public bool Success { get; }
        public string Message { get; }
        public IAudioHandle Handle { get; }
        public Exception Exception { get; }

        private AudioPlaybackResult(bool success, string message, IAudioHandle handle, Exception exception)
        {
            Success = success;
            Message = message;
            Handle = handle;
            Exception = exception;
        }

        public static AudioPlaybackResult Succeed(string message, IAudioHandle handle = null)
        {
            return new AudioPlaybackResult(true, message, handle, null);
        }

        public static AudioPlaybackResult Fail(string message, Exception exception = null)
        {
            return new AudioPlaybackResult(false, message, null, exception);
        }
    }
}
