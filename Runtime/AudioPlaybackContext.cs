using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Mutable, per-playback context passed to <see cref="ISoundOption.Apply"/> and
    /// <see cref="ISoundModulator.Modulate"/>. Modulators may:
    /// <list type="bullet">
    ///   <item>Inspect the resolved <see cref="Definition"/>, <see cref="Bus"/>,
    ///         <see cref="EffectiveVolume"/>, <see cref="BasePitch"/>.</item>
    ///   <item>Override the played <see cref="ClipOverride"/> (selection modulators).</item>
    ///   <item>Cancel the playback altogether by calling <see cref="Veto"/> with a reason
    ///         (cooldown / max-concurrent / custom rules).</item>
    /// </list>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioPlaybackContext")]
    public sealed class AudioPlaybackContext
    {
        public AudioDefinition Definition { get; }
        public AudioBusAsset Bus { get; }
        public float EffectiveVolume { get; }
        public float BasePitch { get; }

        /// <summary>Selection modulators set this to swap the clip about to be played.</summary>
        public AudioClip ClipOverride { get; set; }

        /// <summary>True after any modulator / option called <see cref="Veto"/>.</summary>
        public bool IsVetoed { get; private set; }
        public string VetoReason { get; private set; }

        /// <summary>
        /// Set by options that take ownership of triggering the source themselves
        /// (e.g. <c>DelayedPlayOption</c> using <see cref="AudioSource.PlayDelayed"/>).
        /// When true, AudioService will not call the final <see cref="AudioSource.Play"/>.
        /// </summary>
        public bool SuppressDefaultPlay { get; set; }

        public AudioPlaybackContext(AudioDefinition definition, AudioBusAsset bus, float effectiveVolume, float basePitch)
        {
            Definition = definition;
            Bus = bus;
            EffectiveVolume = effectiveVolume;
            BasePitch = basePitch;
        }

        /// <summary>
        /// Cancels the current playback. Subsequent modulators / options are still invoked
        /// (they may want to reset internal state) but the AudioService will not actually
        /// trigger the source. The first reason wins — later veto calls are ignored.
        /// </summary>
        public void Veto(string reason)
        {
            if (IsVetoed) return;
            IsVetoed = true;
            VetoReason = reason ?? "vetoed";
        }
    }
}
