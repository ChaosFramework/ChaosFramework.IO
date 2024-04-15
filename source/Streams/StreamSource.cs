using System.IO;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public interface StreamSource
    {
        /// <summary>
        ///     Returns whether there is a stream associated with <paramref name="key"/>.
        ///     Must not throw any exceptions as long as this <see cref="StreamSource"/> is <see cref="alive"/>.
        ///     If this <see cref="StreamSource"/> is not <see cref="alive"/>, the behavior is undefined.
        /// </summary>
        /// <param name="key"> The key to check for. </param>
        bool ContainsKey(string key);

        /// <summary>
        ///     Returns a stream associated with the provided <paramref name="key"/>.
        ///     For each call of <see cref="OpenRead(string)"/> a unique stream instance will be retrieved.
        ///     Only explicitly throws exceptions that derive from <see cref="StreamSourceException"/>.
        ///     If this <see cref="StreamSource"/> is not <see cref="alive"/>, the behavior is undefined.
        /// </summary>
        /// <param name="key"> The key to retrieve a stream for. </param>
        /// <exception cref="KeyFormatException">
        ///     Thrown if the provided <paramref name="key"/> has an illegal format.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown if no stream could be associated with <paramref name="key"/>.
        /// </exception>
        /// <exception cref="StreamAccessException">
        ///     Thrown if the stream associated with <paramref name="key"/> was found, but cannot be read.
        /// </exception>
        /// <exception cref="StreamSourceException">
        ///     Thrown if retrieving the stream failed for any other reason.
        /// </exception>
        Stream OpenRead(string key);

        /// <summary>
        ///     Yields all keys known to this <see cref="StreamSource"/>.
        ///     Must not throw any exceptions as long as this <see cref="StreamSource"/> is <see cref="alive"/>.
        ///     If this <see cref="StreamSource"/> is not <see cref="alive"/>, the behavior is undefined.
        /// </summary>
        SysCol.IEnumerable<string> EnumerateKeys();

        /// <summary>
        ///     Returns whether this <see cref="StreamSource"/> is still alive.
        ///     If it is not, the behavior of any other call on this <see cref="StreamSource"/> is undefined.
        /// </summary>
        bool alive { get; }
    }
}
