using Ipfs.Hypermedia.Cryptography;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Representation of Directory in both IPFS distributed file system and OS file system.
    /// </summary>
    [Serializable]
    public sealed class Directory : IEntity, ISystemEntity
    {
        /// <summary>
        ///   Path for this directory in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this directory.
        /// </remarks>
        public string Path { get; set; }
        /// <summary>
        ///   Human readable name of directory.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///   Attributes of directory.
        /// </summary>
        /// <remarks>
        ///   Made nullable to be compatible with raw ipfs directories which doesn't have such information.
        /// </remarks>
        public FileAttributes? Attributes { get; set; }
        /// <summary>
        ///   Last modification date and time for directory.
        /// </summary>
        /// <remarks>
        ///   Local time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs directories which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTime { get; set; }
        /// <summary>
        ///   Last modification date and time for directory.
        /// </summary>
        /// <remarks>
        ///   UTC time of creator of Hypermedia.
        ///   Made nullable to be compatible with raw ipfs directories which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTimeUtc { get; set; }
        /// <summary>
        ///   List of <see cref="ISystemEntity">System Enteties</see> of directory.
        /// </summary>
        /// <remarks>
        ///   Due to limitation, directories can store only file system entities that have basic information for OS.
        /// </remarks>
        public List<ISystemEntity> Entities { get; set; }
        /// <summary>
        ///   Size of directory in bytes.
        /// </summary>
        /// <remarks>
        ///   The size of directory - is the size of all entities inside of it.
        /// </remarks>
        public ulong Size { get; set; }
        /// <summary>
        ///   Hash of directory for verification purposes.
        /// </summary>
        public string Hash { get; private set; } = null;
        private IEntity _parent;
        /// <summary>
        ///   Link to parent entity in which this directory resides.
        /// </summary>
        /// <remarks>
        ///   It can be either a <see cref="Hypermedia">Hypermedia</see>,
        ///   or a <see cref="Directory">Directory<see/>.
        ///   It should be noted, that <see cref="File">File</see> can't be parent for Directory.
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
                    throw new ArgumentException("File can't be parent of the directory!");
                _parent = value;
            }
        }
        /// <summary>
        ///   Creates and set hash for directory instance.
        /// </summary>
        /// <param name="content">
        ///   Always null.
        /// </param>
        /// <remarks>
        ///   You should never manually set content parameter of SetHash for Directory.
        ///   Preferable method is adding files with single part to directory after setting hash for this file.
        /// </remarks>
        /// <exception cref="Exception"/>
        public void SetHash(byte[] content = null)
        {
            if (Hash is null)
            {
                KeccakManaged keccak = new KeccakManaged(512);

                IEntity parent = Parent;
                while (!(parent is Hypermedia))
                {
                    parent = parent.Parent;
                }

                List<string> entitesHashes = new List<string>();
                foreach (var e in Entities)
                {
                    if(e is File)
                        entitesHashes.Add((e as File).GetHash());
                    else if(e is Directory)
                        entitesHashes.Add((e as Directory).GetHash());
                }
                List<byte> buffer = new List<byte>();
                buffer.AddRange((parent as Hypermedia).Encoding.GetBytes(Name));
                foreach (var eh in entitesHashes)
                {
                    buffer.AddRange(Encoding.UTF8.GetBytes(eh));
                }

                var buf = keccak.ComputeHash(buffer.ToArray());

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
        /// <summary>
        ///   Creates and set hash for directory instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Always null.
        /// </param>
        /// <remarks>
        ///   You should never manually set content parameter of GetHash for Directory.
        ///   Preferable method is adding files with single part to directory after setting hash for this file.
        /// </remarks>
        public string GetHash(byte[] content = null)
        {
            if (Hash is null)
                SetHash();
            return Hash;
        }
    }
}
