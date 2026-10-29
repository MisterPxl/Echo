namespace Astra.Echo
{
    /// <summary>
    /// Audio service contract — the primary, injectable entry point for audio playback.
    /// All playback is driven by typed <see cref="AudioId"/> values that resolve to
    /// <see cref="AudioDefinition"/> assets through an <see cref="IAudioDatabase"/>.
    ///
    /// <para>
    /// Volume controls are organized by <see cref="AudioCategory"/> (Master / Music / Sfx / Ambient).
    /// Music and Ambient are single-channel (calling Play replaces the current track).
    /// SFX are pooled and can play concurrently.
    /// </para>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IAudioService")]
    public interface IAudioService
    {
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }
        float AmbientVolume { get; set; }

        bool IsMusicPlaying { get; }
        bool IsAmbientPlaying { get; }

        void SetVolume(AudioCategory category, float volume);
        float GetVolume(AudioCategory category);

        /// <summary>
        /// Plays the definition associated with the given id using its declared <see cref="AudioType"/>.
        /// SFX → pooled one-shot. Music → single music channel. Ambient → single ambient channel.
        /// </summary>
        AudioPlaybackResult Play(AudioId id);

        /// <summary>
        /// Plays the definition with caller-side overrides (volume, pitch, loop, options, modulators).
        /// </summary>
        AudioPlaybackResult Play(in AudioPlaybackRequest request);

        /// <summary>
        /// Starts a fluent playback request for the given id. Call <see cref="AudioRequest.Play"/> to commit.
        /// </summary>
        AudioRequest CreateRequest(AudioId id);

        AudioPlaybackResult StopMusic();
        AudioPlaybackResult StopAmbient();

        /// <summary>
        /// Stops every active SFX returned by the pool. Music / Ambient channels are untouched.
        /// </summary>
        void StopAllSfx();
    }
}
