using System.Collections.Generic;

namespace Astra.Echo
{
    /// <summary>
    /// Optional capability to register several <see cref="IAudioDatabase"/> instances at runtime.
    /// <see cref="AudioService"/> implements this contract when used as the concrete service.
    /// </summary>
    public interface IAudioDatabaseRegistry
    {
        IReadOnlyList<IAudioDatabase> Databases { get; }

        /// <returns>False when <paramref name="database"/> is null or already registered.</returns>
        bool AddDatabase(IAudioDatabase database);

        /// <returns>False when <paramref name="database"/> was not registered. Stops playbacks of its definitions.</returns>
        bool RemoveDatabase(IAudioDatabase database);
    }
}