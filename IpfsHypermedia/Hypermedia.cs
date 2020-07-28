using Ipfs.Hypermedia.Cryptography;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Hypermedia entity that contains metadata for any complex IPFS entities.
    ///   It have all necessary information for right download and upload of files and folders in IPFS.
    /// </summary>
    /// <remarks>
    ///   In simple words - it just a directory with necessary information for clients of hypermedia network to
    ///   describe entity.
    ///   It implements <see cref="IEntity">IEntity</see>, so it could store others Hypermedia inside it.
    ///   It DOESN'T implements <see cref="ISystemEntity">ISystemEntity</see>, because it's not managed by OS.
    /// </remarks>
    [Serializable]
    public sealed class Hypermedia : IEntity
    {
        /// <summary>
        ///   Path for this hypermedia in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this hypermedia.
        ///   It should be noted, that due to realization of IPFS protocol, the outer hypermedia Path - is null,
        ///   but included hypermedia will have path for retrival.
        /// </remarks>
        public string Path { get; set; } = null;
        /// <summary>
        ///   Human readable name of hypermedia.
        /// </summary>
        /// <remarks>
        ///   It serves as name of directory where all downloaded entities stored.
        ///   And as display name for clients of hypermedia network.
        /// </remarks>
        public string Name { get; set; }
        /// <summary>
        ///   Size of hypermedia in bytes.
        /// </summary>
        /// <remarks>
        ///   The size of hypermedia - is the size of all entities inside of it.
        /// </remarks>
        public ulong Size { get; set; }
        /// <summary>
        ///   Comment of creator of hypermedia.
        /// </summary>
        /// <remarks>
        ///   It can be any information. Or description. As well as null.
        /// </remarks>
        public string Comment { get; set; } = null;
        /// <summary>
        ///   Encoding of metadata properties.
        /// </summary>
        /// <remarks>
        ///   It used in hashing process.
        ///   Each hypermedia can use it own encoding type.
        /// </remarks>
        public Encoding Encoding { get; set; }
        /// <summary>
        ///   Date and time of creation of hypermedia.
        /// </summary>
        /// <remarks>
        ///   Local time of creator of Hypermedia.
        ///   Made NOT nullable because Hypermedia is nor OS file system entity, nor IPFS. So it could be setted during
        ///   creation of Hypermedia.
        /// </remarks>
        public DateTime CreatedDateTime { get; set; }
        /// <summary>
        ///   Date and time of creation of hypermedia.
        /// </summary>
        /// <remarks>
        ///   UTC time of creator of Hypermedia.
        ///   Made NOT nullable because Hypermedia is nor OS file system entity, nor IPFS. So it could be setted during
        ///   creation of Hypermedia.
        /// </remarks>
        public DateTime CreatedDateTimeUTC { get; set; }
        /// <summary>
        ///   Information about client that created this Hypermedia.
        /// </summary>
        /// <remarks>
        ///   It can be used in identification of clients and inter-operability of different clients.
        ///   Default - hypermedia
        /// </remarks>
        public string CreatedBy { get; set; } = "hypermedia";
        /// <summary>
        ///   Boolean flag for indication if entities inside hypermedia wrapped in directory and thus storing their name and extension without metadata.
        /// </summary>
        /// <remarks>
        ///   By default all hypermedia clients should set this parameter to true, but if <see cref="IsRawIPFS"/> is used - it can be false.
        ///   If set to false - hypermedia client should use metadata of entities instead of IPFS ones.
        /// </remarks>
        public bool IsDirectoryWrapped { get; set; }
        /// <summary>
        ///   List of <see cref="IEntity">Enteties</see> of hypermedia.
        /// </summary>
        /// <remarks>
        ///   It's not limited to storing only <see cref="Directory">Directories</see> or <see cref="File">Files</see>,
        ///   but <see cref="Hypermedia">Hypermedia</see> as well.
        ///   Hypermedia can be stored for transparent include and update of not owned hypermedia.
        /// </remarks>
        public List<IEntity> Entities { get; set; } = new List<IEntity>();
        /// <summary>
        /// Boolean flag for indication if entities inside hypermedia doesn't contain hypermedia metadata, thus metadata can be unreliable.
        /// </summary>
        /// <remarks>
        /// <see langword="true"/> means that hypermedia client tried and parsed maximum amount of metadata that is available in IPFS,
        /// which means that some of metadata could be incorrect.
        /// </remarks>
        public bool IsRawIPFS { get; set; }
        /// <summary>
        ///   PubSub Topic.
        ///   Used for tracking peers that have this hypermedia.
        /// </summary>
        /// <remarks>
        ///   Unnesessary property.
        ///   Different clients can implement different uses of PubSub Topic property.
        /// </remarks>
        public string Topic { get; private set; } = null;
        /// <summary>
        ///   Default message of client after subscribing to topic.
        /// </summary>
        /// <remarks>
        ///   Unnesessary property.
        ///   Used for standartisation of clients working in Hypermedia network.
        /// </remarks>
        public string DefaultSubscriptionMessage { get; private set; } = "subscribed";
        /// <summary>
        ///   Default message of client after downloading hypermedia.
        /// </summary>
        /// <remarks>
        ///   Unnesessary property.
        ///   Such clients have priority for downloading client to connect with.
        ///   Used for standartisation of clients working in Hypermedia network.
        /// </remarks>
        public string DefaultSeedingMessage { get; private set; } = "seeding";
        /// <summary>
        ///   Hash of hypermedia for verification purposes.
        /// </summary>
        public string Hash { get; private set; } = null;
        private IEntity _parent;
        /// <summary>
        ///   Link to parent entity in which this Hypermedia resides.
        ///   If it's outer hypermedia - it equals null.
        /// </summary>
        /// <remarks>
        ///   It can be only a <see cref="Hypermedia">Hypermedia</see>.
        /// </remarks>
        public IEntity Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (!(value is Hypermedia))
                    throw new ArgumentException("Only hypermedia can be parent for hypermedia!");
                _parent = value;
            }
        }
        /// <summary>
        ///   Information about version of hypermedia metadata.
        /// </summary>
        /// <remarks>
        ///   It used in clients for choosing correct algorithms of parsing a hypermedia.
        ///   Current default - hypermedia/0.1.0
        /// </remarks>
        public string Version { get; private set; } = "hypermedia/0.1.0";
        /// <summary>
        ///   Default parameterless constructor. Initializes <see cref="Parent">Parent</see> with passed <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">
        ///   Parent hypermedia for this hypermedia.
        ///   By default equals null;
        /// </param>
        public Hypermedia(Hypermedia parent = null)
        {
            Parent = parent;
        }
        /// <summary>
        ///   Creates and set hash for hypermedia instance.
        /// </summary>
        /// <param name="content">
        ///   Always null.
        /// </param>
        /// <remarks>
        ///   You should never manually set content parameter of SetHash for Hypermedia.
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
                    entitesHashes.Add(e.GetHash());

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
        ///   Creates and set hash for hypermedia instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Always null.
        /// </param>
        /// <remarks>
        ///   You should never manually set content parameter of GetHash for Hypermedia.
        /// </remarks>
        public string GetHash(byte[] content = null)
        {
            if (Hash is null)
                SetHash();
            return Hash;
        }
        /// <summary>
        ///   Creates and set topic for PubSub, using <see cref="Name">Name</see> and <see cref="Hash">Hash</see> as identifier.
        /// </summary>
        private void SetTopic()
        {
            if (Hash is null)
                throw new Exception("Hash must be created for hypermedia before topic address creation");
            if (!(Topic is null))
                throw new Exception("Topic can only be set once");
            Topic = $"{Name}_{Hash}";
        }
        /// <summary>
        ///   Serializes given hypermedia to stream asynchronously.
        /// </summary>
        /// <param name="outputStream">
        ///   The output stream in which hypermedia would be serialized.
        /// </param>
        /// <param name="instance">
        ///   The instance of hypermedia that would be serialized.
        /// </param>
        /// <returns>
        ///   Task or void. 
        /// </returns>
        public static async Task Serialize(Stream outputStream, Hypermedia instance)
        {
            await JsonSerializer.SerializeAsync<Hypermedia>(outputStream, instance, options: new JsonSerializerOptions() { MaxDepth = 0, WriteIndented = true });
        }
        /// <summary>
        ///   Deserializes given stream to hypermedia asynchronously.
        /// </summary>
        /// <param name="inputStream">
        ///   The input stream from which hypermedia would be deserialized.
        /// </param>
        /// <returns>
        ///   Task with Hypermedia.. 
        /// </returns>
        public static async Task<Hypermedia> Deserialize(Stream inputStream)
        {
            return await JsonSerializer.DeserializeAsync<Hypermedia>(inputStream, options: new JsonSerializerOptions() { MaxDepth = 0, WriteIndented = true });
        }
    }
}
