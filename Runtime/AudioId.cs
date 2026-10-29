using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Stable, typed identifier for an <see cref="AudioDefinition"/>.
    /// The underlying value is the asset GUID hex string, which means:
    /// <list type="bullet">
    ///   <item>Identifiers stay valid when the definition asset is renamed or moved.</item>
    ///   <item>Two identifiers compare equal iff they target the exact same asset.</item>
    ///   <item>The empty / default value <see cref="None"/> represents "no audio".</item>
    /// </list>
    /// In a runtime context an <see cref="AudioId"/> is just data — no Unity reference is held.
    /// Use <see cref="IAudioDatabase.TryResolve"/> to recover the matching definition.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioId")]
    public struct AudioId : IEquatable<AudioId>
    {
        [SerializeField] private string _guid;

        public AudioId(string guid)
        {
            _guid = guid;
        }

        public string Guid => _guid;

        public bool IsValid => !string.IsNullOrEmpty(_guid);

        public static readonly AudioId None = new AudioId(null);

        public bool Equals(AudioId other) => string.Equals(_guid, other._guid, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AudioId other && Equals(other);
        public override int GetHashCode() => _guid != null ? _guid.GetHashCode() : 0;
        public override string ToString() => IsValid ? $"AudioId({_guid})" : "AudioId(None)";

        public static bool operator ==(AudioId left, AudioId right) => left.Equals(right);
        public static bool operator !=(AudioId left, AudioId right) => !left.Equals(right);
    }
}
