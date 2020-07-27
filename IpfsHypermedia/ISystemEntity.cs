using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Public interface for realization of any file system entity that have basic information for OS.
    /// </summary>
    /// <remarks>
    ///   <see cref="Hypermedia">Hypermedia</see> type shouldn't implement this interface
    ///   due to nature of this entity (not OS related).
    /// </remarks>
    public interface ISystemEntity
    {
        /// <summary>
        ///   Attributes of entity.
        /// </summary>
        /// <remarks>
        ///   Shared by <see cref="File">Files</see> and <see cref="Directory">Directories</see>.
        ///   Made nullable to be compatible with raw ipfs entities which doesn't have such information.
        /// </remarks>
        FileAttributes? Attributes { get; set; }
        /// <summary>
        ///   Last modification date and time for entity.
        /// </summary>
        /// <remarks>
        ///   Local time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs entities which doesn't have such information.
        /// </remarks>
        DateTime? LastModifiedDateTime { get; set; }
        /// <summary>
        ///   Last modification date and time for entity.
        /// </summary>
        /// <remarks>
        ///   UTC time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs entities which doesn't have such information.
        /// </remarks>
        DateTime? LastModifiedDateTimeUtc { get; set; }
    }
}
