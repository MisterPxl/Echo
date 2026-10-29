namespace Astra.Echo
{
    /// <summary>
    /// User-facing volume bus. Master applies on top of every other bus.
    /// Mirrors <see cref="AudioType"/> for content classification but adds
    /// a Master entry that has no equivalent in <see cref="AudioType"/>.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioCategory")]
    public enum AudioCategory
    {
        Master = 0,
        Music = 1,
        Sfx = 2,
        Ambient = 3
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioCategoryExtensions")]
    public static class AudioCategoryExtensions
    {
        /// <summary>
        /// Maps an <see cref="AudioType"/> to its volume bus.
        /// </summary>
        public static AudioCategory ToCategory(this AudioType type)
        {
            return type switch
            {
                AudioType.Music   => AudioCategory.Music,
                AudioType.Ambient => AudioCategory.Ambient,
                _                 => AudioCategory.Sfx
            };
        }
    }
}
