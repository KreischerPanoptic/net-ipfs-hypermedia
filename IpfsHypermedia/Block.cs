using Ipfs.Hypermedia.Cryptography;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Security.Permissions;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Implements the IPFS block with necessary information for block retrieval
    ///   and correct processing.
    /// </summary>
    [Serializable]
    public sealed class Block
    {
        /// <summary>
        ///   Key of block in IPFS distributed file system.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        ///   Size of block in bytes.
        /// </summary>
        public ulong Size { get; set; }
        /// <summary>
        ///   Link to parent <see cref="File">File<see/> from which this block is taken.
        /// </summary>
        /// <remarks>
        ///   Return type is File which can be confusing,
        ///   but it should be that way. The use of <see cref="ISystemEntity">ISystemEntity<see/> or <see cref="IEntity">IEntity<see/>
        ///   is unwelcome due to possible confusion with <see cref="Directory">Directory<see/> or <see cref="Hypermedia">Hypermedia<see/> types.
        /// </remarks>
        public File Parent { get; set; }
        /// <summary>
        ///   Hash of block for verification purposes.
        /// </summary>
        public string Hash { get; private set; } = null;
        /// <summary>
        ///   Creates and set hash for block instance.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of block in byte array.
        /// </param>
        /// <remarks>
        ///   It would be better if block was creating it own hash during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve block and set it manually.
        /// </remarks>
        public void SetHash(byte[] content)
        {
            if (Hash is null)
            {
                KeccakManaged keccak = new KeccakManaged(512);
                var buf = keccak.ComputeHash(content);

                StringBuilder sb = new StringBuilder();
                foreach (var b in buf)
                {
                    sb.Append(b.ToString("X2"));
                }
                Hash = sb.ToString();
            }
            else
            {
                throw new Exception("Hash can only be set once");
            }
        }
    }
}
