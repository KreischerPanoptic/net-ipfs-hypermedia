using Ipfs.Hypermedia.Cryptography;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Representation of File in both IPFS distributed file system and OS file system.
    /// </summary>
    [Serializable]
    public sealed class File : IEntity, ISystemEntity
    {
        /// <summary>
        ///   Path for this file in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this file.
        /// </remarks>
        public string Path { get; set; }
        /// <summary>
        ///   Human readable name of file.
        /// </summary>
        /// <remarks>
        ///   It is name of file without extension.
        /// </remarks>
        public string Name { get; set; }
        /// <summary>
        ///   Extension of the file for identification by OS.
        /// </summary>
        /// <remarks>
        ///   It contains ONLY extension.
        /// </remarks>
        public string Extension { get; set; }
        /// <summary>
        ///   Attributes of file.
        /// </summary>
        /// <remarks>
        ///   Made nullable to be compatible with raw ipfs files which doesn't have such information.
        /// </remarks>
        public FileAttributes? Attributes { get; set; }
        /// <summary>
        ///   Last modification date and time for file.
        /// </summary>
        /// <remarks>
        ///   Local time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs files which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTime { get; set; }
        /// <summary>
        ///   Last modification date and time for file.
        /// </summary>
        /// <remarks>
        ///   UTC time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs files which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTimeUtc { get; set; }
        /// <summary>
        ///   List of <see cref="Block">Blocks</see> of file.
        /// </summary>
        /// <remarks>
        ///   In single block file it would be empty array.
        /// </remarks>
        public List<Block> Blocks { get; set; } = new List<Block>();
        /// <summary>
        ///   Boolean flag for indication of download method of file.
        /// </summary>
        /// <remarks>
        ///   Single block file downloading by the whole part and doesn't have <see cref="Blocks">Block</see>.
        ///   Multiple block file first downloading all blocks and after it must merge them into whole file in OS filesystem.
        /// </remarks>
        public bool IsSingleBlock { get; set; }
        /// <summary>
        ///   Size of file in bytes.
        /// </summary>
        public ulong Size { get; set; }
        /// <summary>
        ///   Hash of file for verification purposes.
        /// </summary>
        public string Hash { get; private set; } = null;
        private IEntity _parent;
        /// <summary>
        ///   Link to parent entity in which this file resides.
        /// </summary>
        /// <remarks>
        ///   It can be either a <see cref="Hypermedia">Hypermedia</see>,
        ///   or a <see cref="Directory">Directory<see/>.
        ///   It should be noted, that <see cref="File">File</see> can't be parent for File.
        /// </remarks>
        public IEntity Parent 
        {
            get
            {
                return _parent;
            }
            set
            {
                if (value is File)
                    throw new ArgumentException("File can't be parent of the file!");
                _parent = value;
            }
        }
        /// <summary>
        ///   Creates and set hash for file instance.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of file if needed in byte array.
        ///   Default null.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        /// <exception cref="Exception"/>
        public void SetHash(byte[] content = null)
        {
            if (Hash is null)
            {
                KeccakManaged keccak = new KeccakManaged(512);
                if (this.IsSingleBlock)
                {
                    if (content != null)
                    {
                        throw new Exception("This file is single block and must be provided with byte array of content");
                    }
                    else
                    {
                        var buf = keccak.ComputeHash(content);

                        StringBuilder sb = new StringBuilder();
                        foreach (var b in buf)
                        {
                            sb.Append(b.ToString("X2"));
                        }
                        Hash = sb.ToString();
                    }
                }
                else
                {
                    IEntity parent = Parent;
                    while(!(parent is Hypermedia))
                    {
                        parent = parent.Parent;
                    }

                    List<string> blockHashes = new List<string>();
                    foreach (var b in Blocks)
                    {
                        blockHashes.Add(b.Hash);
                    }
                    List<byte> buffer = new List<byte>();
                    buffer.AddRange((parent as Hypermedia).Encoding.GetBytes(Name));
                    buffer.AddRange((parent as Hypermedia).Encoding.GetBytes(Extension));
                    foreach (var bh in blockHashes)
                    {
                        buffer.AddRange(Encoding.UTF8.GetBytes(bh));
                    }

                    var buf = keccak.ComputeHash(buffer.ToArray());

                    StringBuilder sb = new StringBuilder();
                    foreach (var b in buf)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    Hash = sb.ToString();
                }
            }
            else
            {
                throw new Exception("Hash can only be set once");
            }
        }
        /// <summary>
        ///   Creates and set hash for file instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of file if needed in byte array.
        ///   Default null.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        public string GetHash(byte[] content = null)
        {
            if (Hash is null)
                SetHash(content);
            return Hash;
        }
    }
}
