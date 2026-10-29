using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Abstract base class for modulators authored inside <see cref="AudioDefinition"/> assets.
    /// Concrete subclasses are persisted via <c>[SerializeReference]</c>, so they need
    /// the <see cref="SerializableAttribute"/> attribute and a parameterless constructor.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "SoundModulatorBase")]
    public abstract class SoundModulatorBase : ISoundModulator
    {
        public abstract void Modulate(AudioSource source, AudioPlaybackContext context);
    }
}
