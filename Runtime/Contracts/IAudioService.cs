using System.Collections.Generic;

namespace Astra.Echo
{
    /// <summary>
    /// Audio service contract — the primary, injectable entry point for audio playback.
    /// All playback is driven by typed <see cref="AudioId"/> values that resolve to
    /// <see cref="AudioDefinition"/> assets through an <see cref="IAudioDatabase"/>.
    ///
    /// <para>
    /// Volumes are organized by <see cref="AudioBusAsset"/>, under <see cref="MasterVolume"/>:
    /// a source plays at definition volume × bus volume × Master. The bus playback decides the
    /// channel: a single track replaced by the next Play, or pooled one-shots.
    /// </para>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IAudioService")]
    public interface IAudioService
    {
        float MasterVolume { get; set; }

        /// <summary>Buses known to the service: the configured ones, then every bus met since.</summary>
        IReadOnlyList<AudioBusAsset> Buses { get; }

        /// <summary>Bus volume before Master; the bus default until set.</summary>
        float GetVolume(AudioBusAsset bus);
        void SetVolume(AudioBusAsset bus, float volume);

        bool IsPlaying(AudioBusAsset bus);

        /// <summary>
        /// Plays the definition associated with the given id on its <see cref="AudioDefinition.Bus"/>.
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

        /// <summary>Stops everything playing on the bus. Other buses are untouched.</summary>
        AudioPlaybackResult Stop(AudioBusAsset bus);
    }
}
