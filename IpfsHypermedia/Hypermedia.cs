using Ipfs.Hypermedia.Cryptography;
using Ipfs.Hypermedia.Serialization;
using Ipfs.Hypermedia.Serialization.Versions;
using Ipfs.Hypermedia.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public abstract class Hypermedia : IEntity
    {
        /// <summary>
        ///   Path for this hypermedia in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this hypermedia.
        ///   It should be noted, that due to realization of IPFS protocol, the outer hypermedia Path - is null,
        ///   but included hypermedia will have path for retrival.
        /// </remarks>
        public string Path { get; set; }
        private string _name;
        /// <summary>
        ///   Human readable name of hypermedia.
        /// </summary>
        /// <remarks>
        ///   It serves as name of directory where all downloaded entities stored.
        ///   And as display name for clients of hypermedia network.
        /// </remarks>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != null)
                {
                    if (value.Length > 255)
                    {
                        throw new ArgumentException("Hypermedia name can not have more than 255 symbols");
                    }
                    if (value.Length <= 0)
                    {
                        throw new ArgumentException("Hypermedia name can not be empty");
                    }
                    _name = value;
                }
                else
                {
                    throw new ArgumentException("Hypermedia name can not be null");
                }
            }
        }
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
        public string Comment { get; set; }
        /// <summary>
        ///   Encoding of metadata properties.
        /// </summary>
        /// <remarks>
        ///   It used in hashing process and storing of directory/file names.
        ///   Each hypermedia can use it own encoding type.
        ///   Default - UTF8
        /// </remarks>
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        private DateTime _createdDateTime;
        /// <summary>
        ///   Date and time of creation of hypermedia in UTC.
        /// </summary>
        /// <remarks>
        ///   Serializes to total seconds passed from Unix Epoch.
        ///   Made NOT nullable because Hypermedia is nor OS file system entity, nor IPFS. So it could be setted during
        ///   creation of Hypermedia.
        /// </remarks>
        public DateTime CreatedDateTime
        {
            get { return _createdDateTime; }
            set { _createdDateTime = value.ToUniversalTime(); }
        }
        /// <summary>
        ///   Information about client that created this Hypermedia.
        /// </summary>
        /// <remarks>
        ///   It can be used in identification of clients and inter-operability of different clients.
        ///   Default - hypermedia
        /// </remarks>
        public string CreatedBy { get; set; } = "hypermedia";
        /// <summary>
        ///   Unnesessary property of address of creator seed peer,
        ///   for clients which doesn't implement PubSub Hypermedia Network discovery of seeding peers.
        /// </summary>
        /// <remarks>
        ///   Must be valid IPFS Multiaddress.
        /// </remarks>
        public string CreatorPeer { get; set; }
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
        public string Topic { get; internal set; }
        /// <summary>
        ///   Default message of client after subscribing to topic.
        /// </summary>
        /// <remarks>
        ///   Unnesessary property.
        ///   Used for standartisation of clients working in Hypermedia network.
        /// </remarks>
        public string DefaultSubscriptionMessage { get; internal set; } = "subscribed";
        /// <summary>
        ///   Default message of client after downloading hypermedia.
        /// </summary>
        /// <remarks>
        ///   Unnesessary property.
        ///   Such clients have priority for downloading client to connect with.
        ///   Used for standartisation of clients working in Hypermedia network.
        /// </remarks>
        public string DefaultSeedingMessage { get; internal set; } = "seeding";
        /// <summary>
        ///   Hash of hypermedia for verification purposes.
        /// </summary>
        public string Hash { get; internal set; }
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
            get { return _parent; }
            set
            {
                if (!(value is Hypermedia || value is null))
                {
                    throw new ArgumentException("Only hypermedia and can be parent for hypermedia! Or null.");
                }
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
        public abstract string Version { get; internal set; }
        /// <summary>
        ///   Creates and set hash for hypermedia instance.
        /// </summary>
        public abstract void SetHash();
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
        public abstract void SetHash(byte[] content);
        /// <summary>
        ///   Creates and set hash for hypermedia instance, and returns it as string.
        /// </summary>
        public abstract string GetHash();
        /// <summary>
        ///   Creates and set hash for hypermedia instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Always null.
        /// </param>
        /// <remarks>
        ///   You should never manually set content parameter of GetHash for Hypermedia.
        /// </remarks>
        public abstract string GetHash(byte[] content);
        /// <summary>
        ///   Creates and set topic for PubSub, using <see cref="Name">Name</see> and <see cref="Hash">Hash</see> as identifier.
        /// </summary>
        public abstract void SetTopic();
        /// <summary>
        ///   Serializes given hypermedia to stream asynchronously.
        /// </summary>
        /// <param name="outputStream">
        ///   The output stream in which hypermedia would be serialized.
        /// </param>
        /// <param name="instance">
        ///   The in
        public static async Task SerializeAsync(Stream outputStream, Hypermedia instance)
        {
            await SerializeAsync(outputStream, instance, Formatting.None).ConfigureAwait(false);
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
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <returns>
        ///   Task or void. 
        /// </returns>
        public static async Task SerializeAsync(Stream outputStream, Hypermedia instance, Formatting formatting)
        {
            await Task.Run(async () =>
            {
                var serializator = SerializationVersionTools.GetSerializationVersion(instance.Version);
                var buffer = Encoding.UTF8.GetBytes(serializator.SerializeToString(instance, formatting, 0));
                await outputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        /// <summary>
        ///   Serializes given hypermedia to stream synchronously.
        /// </summary>
        /// <param name="outputStream">
        ///   The output stream in which hypermedia would be serialized.
        /// </param>
        /// <param name="instance">
        ///   The instance of hypermedia that would be serialized.
        /// </param>
        public static void Serialize(Stream outputStream, Hypermedia instance)
        {
            SerializeAsync(outputStream, instance, Formatting.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        /// <summary>
        ///   Serializes given hypermedia to stream synchronously.
        /// </summary>
        /// <param name="outputStream">
        ///   The output stream in which hypermedia would be serialized.
        /// </param>
        /// <param name="instance">
        ///   The instance of hypermedia that would be serialized.
        /// </param>
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <returns>
        ///   void. 
        /// </returns>
        public static void Serialize(Stream outputStream, Hypermedia instance, Formatting formatting)
        {
            SerializeAsync(outputStream, instance, formatting).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        /// <summary>
        ///   Deserializes given stream to hypermedia asynchronously.
        /// </summary>
        /// <param name="inputStream">
        ///   The input stream from which hypermedia would be deserialized.
        /// </param>
        /// <returns>
        ///   Task with Hypermedia. 
        /// </returns>
        public static async Task<Hypermedia> DeserializeAsync(Stream inputStream)
        {
            return await Task<Hypermedia>.Run(() =>
            {
                List<byte> buffer = new List<byte>();
                bool isEndOfStream = false;
                while (!isEndOfStream)
                {
                    int b = inputStream.ReadByte();
                    if (b != -1)
                    {
                        buffer.Add((byte)b);
                    }
                    else
                    {
                        isEndOfStream = true;
                    }
                }
                string input = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
                var deserializer = SerializationVersionTools.GetSerializationVersion(SerializationVersionTools.GetVersion(input));
                return deserializer.DeserializeFromString(input);
            }).ConfigureAwait(false);
        }
        /// <summary>
        ///   Deserializes given stream to hypermedia synchronously.
        /// </summary>
        /// <param name="inputStream">
        ///   The input stream from which hypermedia would be deserialized.
        /// </param>
        /// <returns>
        ///   <see cref="Hypermedia">Hypermedia</see>. 
        /// </returns>
        public static Hypermedia Deserialize(Stream inputStream)
        {
            return DeserializeAsync(inputStream).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
