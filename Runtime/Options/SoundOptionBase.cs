using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Abstract base class for sound options authored inside <see cref="AudioDefinition"/> assets
    /// or attached to a single playback via <see cref="AudioRequest.With(ISoundOption)"/>.
    /// Concrete subclasses are persisted via <c>[SerializeReference]</c>, so they need
    /// <see cref="SerializableAttribute"/> and a parameterless constructor.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "SoundOptionBase")]
    public abstract class SoundOptionBase : ISoundOption
    {
        public abstract void Apply(AudioSource source, AudioPlaybackContext context);
    }
}
