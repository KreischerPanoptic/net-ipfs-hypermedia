using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Public interface for realization of any entity that have basic information for human readable identification.
    /// </summary>
    /// <remarks>
    ///   <see cref="Block">Block</see> type shouldn't implement this interface
    ///   due to lack of information.
    /// </remarks>
    public interface IEntity
    {
        /// <summary>
        ///   Path for this entity in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this entity.
        /// </remarks>
        string Path { get; set; }
        /// <summary>
        ///   Human readable name of entity.
        /// </summary>
        /// <remarks>
        ///   For <see cref="Directory">Directory<see/> it would be a name of Directory, for <see cref="File">File<see/> it would be a name of file without extension,
        ///   and for <see cref="Hypermedia">Hypermedia</see> it would be a name of hypermedia given to it be creator.
        /// </remarks>
        string Name { get; set; }
        /// <summary>
        ///   Size of entity in bytes.
        /// </summary>
        ulong Size { get; set; }
        /// <summary>
        ///   Hash of entity for verification purposes.
        /// </summary>
        string Hash { get; }
        /// <summary>
        ///   Creates and set hash for entity instance.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of entity if needed in byte array.
        ///   Default null.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        void SetHash(byte[] content = null);
        /// <summary>
        ///   Creates and set hash for entity instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of entity if needed in byte array.
        ///   Default null.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        string GetHash(byte[] content = null);
        /// <summary>
        ///   Link to parent entity in which this entity resides.
        /// </summary>
        /// <remarks>
        ///   It can be either a <see cref="Hypermedia">Hypermedia</see>,
        ///   or a <see cref="Directory">Directory<see/>.
        /// </remarks>
        IEntity Parent { get; set; }
    }
}
