﻿using Ipfs.Hypermedia.Cryptography;
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
                        throw new Exception("Hypermedia name can not have more than 255 symbols");
                    if (value.Length <= 0)
                        throw new Exception("Hypermedia name can not be empty");
                    _name = value;
                }
                else
                    throw new Exception("Hypermedia name can not be null");
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
        public string Comment { get; set; } = null;
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
            get { return _parent; }
            set
            {
                if (!(value is Hypermedia || value is null))
                    throw new ArgumentException("Only hypermedia and can be parent for hypermedia! Or null.");
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
        ///   Default constructor witn one default parameter. Initializes <see cref="Parent">Parent</see> with passed <paramref name="parent"/>.
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

                List<string> entitesHashes = new List<string>();
                foreach (var e in Entities)
                {
                    entitesHashes.Add(e.GetHash());

                }
                List<byte> buffer = new List<byte>();
                buffer.AddRange(Encoding.GetBytes(Name));
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
        public void SetTopic()
        {
            if (Hash is null)
                throw new Exception("Hash must be created for hypermedia before topic address creation");
            if (!(Topic is null))
                throw new Exception("Topic can only be set once");
            Topic = $"{Path}_{Hash}";
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
        public static async Task SerializeAsync(Stream outputStream, Hypermedia instance)
        {
            await Task.Run(async () =>
            {
                var buffer = Encoding.UTF8.GetBytes(SerializeToString(instance));
                await outputStream.WriteAsync(buffer, 0, buffer.Length);
            });
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
        /// <returns>
        ///   void. 
        /// </returns>
        public static void Serialize(Stream outputStream, Hypermedia instance)
        {
            SerializeAsync(outputStream, instance).ConfigureAwait(false).GetAwaiter().GetResult();
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
                        buffer.Add((byte)b);
                    else
                        isEndOfStream = true;
                }
                return Hypermedia.DeserializeFromString(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
            });
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
        /// <summary>
        ///   Serializes passed hypermedia to string using passed encoding.
        /// </summary>
        /// <param name="hypermedia">
        ///   Hypermedia to be serialized.
        /// </param>
        public static string SerializeToString(Hypermedia hypermedia)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            builder.AppendLine($"(string:path)={hypermedia.Path},");
            builder.AppendLine($"(string:name)={EncodingTools.EncodeString(hypermedia.Name, hypermedia.Encoding is null ? Encoding.UTF8 : hypermedia.Encoding)},");
            builder.AppendLine($"(string:comment)={(hypermedia.Comment is null ? "null" : EncodingTools.EncodeString(hypermedia.Comment, hypermedia.Encoding is null ? Encoding.UTF8 : hypermedia.Encoding))},");
            builder.AppendLine($"(encoding:encoding)={(hypermedia.Encoding is null ? "utf-8" : hypermedia.Encoding.WebName)},");
            builder.AppendLine($"(date_time:created_date_time)={((DateTimeOffset)hypermedia.CreatedDateTime).ToUnixTimeSeconds()},");
            builder.AppendLine($"(string:created_by)={hypermedia.CreatedBy},");
            builder.AppendLine($"(string:creator_peer)={(hypermedia.CreatorPeer is null ? "null" : hypermedia.CreatorPeer)},");
            if (hypermedia.Entities.Count <= 0)
                throw new Exception("Hypermedia entities list can not be empty");
            builder.AppendLine($"(list<entity_interface>[{hypermedia.Entities.Count}]:entities)=" + "{" + EntityListSerializer(hypermedia.Entities, hypermedia.Encoding is null ? Encoding.UTF8 : hypermedia.Encoding) + "},");
            builder.AppendLine($"(boolean:is_directory_wrapped)={(hypermedia.IsDirectoryWrapped ? "true" : "false")},");
            builder.AppendLine($"(boolean:is_raw_ipfs)={(hypermedia.IsRawIPFS ? "true" : "false")},");
            builder.AppendLine($"(string:topic)={(hypermedia.Topic is null ? "null" : hypermedia.Topic)},");
            builder.AppendLine($"(string:default_subscription_message)={(hypermedia.DefaultSubscriptionMessage is null ? "subscribed" : hypermedia.DefaultSubscriptionMessage)},");
            builder.AppendLine($"(string:default_seeding_message)={(hypermedia.DefaultSeedingMessage is null ? "seeding" : hypermedia.DefaultSeedingMessage)},");
            builder.AppendLine($"(string:version)={hypermedia.Version},");
            builder.AppendLine($"(uint64:size)={hypermedia.Size},");
            builder.AppendLine($"(string:parent_path)={(hypermedia.Parent is null ? "null" : hypermedia.Parent.Path)},");
            builder.AppendLine($"(string:hash)={hypermedia.Hash};");
            builder.Append("]");
            return builder.ToString();
        }
        #region Serialization Algorithms
        private static string EntityListSerializer(List<IEntity> entities, Encoding encoding)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            for (int i = 0; i < entities.Count; ++i)
            {
                if (entities[i] is File)
                    builder.AppendLine($"(file:{i})={File.SerializeToString(entities[i] as File, encoding)}{(i == entities.Count - 1 ? ";" : ",")}");
                else if (entities[i] is Directory)
                    builder.AppendLine($"(directory:{i})={Directory.SerializeToString(entities[i] as Directory, encoding)}{(i == entities.Count - 1 ? ";" : ",")}");
                else if(entities[i] is Hypermedia)
                    builder.AppendLine($"(hypermedia:{i})={Hypermedia.SerializeToString(entities[i] as Hypermedia)}{(i == entities.Count - 1 ? ";" : ",")}");
            }
            builder.Append("]");
            return builder.ToString();
        }
        #endregion Serialization Algorithms
        /// <summary>
        ///   Deserializes passed string to hypermedia.
        /// </summary>
        /// <param name="input">
        ///   String to be deserialized.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="Hypermedia">hypermedia</see> for serialized hypermedia.
        ///   Default - null.
        /// </param>
        public static Hypermedia DeserializeFromString(string input, Hypermedia parent = null)
        {
            string path = null;
            string name = null;
            string comment = null;
            Encoding encoding = null;
            long cDateTime = 0;
            DateTime createdDateTime = DateTime.UtcNow;
            string createdBy = null;
            string creatorPeer = null;
            List<IEntity> entities = new List<IEntity>();
            bool isDirectoryWrapped = false;
            bool isRawIPFS = false;
            string topic = null;
            string defaultSubscriptionMessage = null;
            string defaultSeedingMessage = null;
            string version = null;
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            if (!input.StartsWith("["))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.");
            if (!input.EndsWith("]"))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.");

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');

            int count = EntitiesListCount(input);
            if (count <= 0)
                throw new Exception("Possible serialization error encountered. Hypermedia entities list can not be empty");
            string entitiesList = ExtractSerializedEntitiesList(input);
            input = RemoveEntitiesList(input);
            var stringList = input.Split('\n').ToList();

            path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());
            try
            {
                encoding = Encoding.GetEncoding(new string(stringList[3].Skip(20).TakeWhile(x => x != ',').ToArray()));
            }
            catch
            {
                encoding = Encoding.GetEncoding("utf-8");
            }    
            name = EncodingTools.DecodeString(new string(stringList[1].Skip(14).TakeWhile(x => x != ',').ToArray()), encoding);
            comment = EncodingTools.DecodeString(new string(stringList[2].Skip(17).TakeWhile(x => x != ',').ToArray()), encoding);
            cDateTime = long.Parse(new string(stringList[4].Skip(30).TakeWhile(x => x != ',').ToArray()));
            createdDateTime = DateTimeOffset.FromUnixTimeSeconds(cDateTime).UtcDateTime;
            createdBy = new string(stringList[5].Skip(20).TakeWhile(x => x != ',').ToArray());
            creatorPeer = new string(stringList[6].Skip(22).TakeWhile(x => x != ',').ToArray());
            isDirectoryWrapped = (new string(stringList[7].Skip(31).TakeWhile(x => x != ',').ToArray()) == "true") ? true : false;
            isRawIPFS = (new string(stringList[8].Skip(22).TakeWhile(x => x != ',').ToArray()) == "true") ? true : false;
            topic = new string(stringList[9].Skip(15).TakeWhile(x => x != ',').ToArray());
            defaultSubscriptionMessage = new string(stringList[10].Skip(38).TakeWhile(x => x != ',').ToArray());
            defaultSeedingMessage = new string(stringList[11].Skip(33).TakeWhile(x => x != ',').ToArray());
            version = new string(stringList[12].Skip(17).TakeWhile(x => x != ',').ToArray());
            size = ulong.Parse(new string(stringList[13].Skip(14).TakeWhile(x => x != ',').ToArray()));
            parent_path = new string(stringList[14].Skip(21).TakeWhile(x => x != ',').ToArray());
            hash = new string(stringList[15].Skip(14).TakeWhile(x => x != ';').ToArray());

            if(parent != null)
                if (parent.Path != parent_path)
                    throw new ArgumentException("Deserialized parent path is not the expected one");

            Hypermedia hypermedia = new Hypermedia(parent)
            {
                Path = path,
                Name = name,
                Comment = comment,
                Encoding = encoding,
                CreatedDateTime = createdDateTime,
                CreatedBy = createdBy,
                CreatorPeer = creatorPeer,
                IsDirectoryWrapped = isDirectoryWrapped,
                IsRawIPFS = isRawIPFS,
                Topic = topic,
                DefaultSubscriptionMessage = defaultSubscriptionMessage,
                DefaultSeedingMessage = defaultSeedingMessage,
                Version = version,
                Size = size,
                Hash = hash
            };
            
            entities = EntitiesListDeserializer(entitiesList, hypermedia, count);

            hypermedia.Entities = entities;
            return hypermedia;
        }
        #region Deserialization Algorithms
        private static List<IEntity> EntitiesListDeserializer(string input, Hypermedia parent, int count)
        {
            List<IEntity> entities = new List<IEntity>();

            if (!input.StartsWith("["))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.");
            if (!input.EndsWith("]"))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.");

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            var stringList = SplitEntitiesList(input, parent);
            if (stringList.Count != count)
                throw new Exception("Parsed string list does not match expected length");

            for (int i = 0; i < count; ++i)
            {
                string type = new string(stringList[i].Skip(1).TakeWhile(s => s != ':').ToArray());
                int index = int.Parse(new string(stringList[i].Skip(type.Length + 2).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                    throw new Exception("Possible serialization error encountered. Unexpected sequence");
                switch (type)
                {
                    case "file":
                        entities.Add(File.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, parent.Encoding));
                        break;
                    case "directory":
                        entities.Add(Directory.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, parent.Encoding));
                        break;
                    case "hypermedia":
                        entities.Add(Hypermedia.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent));
                        break;
                    default:
                        throw new Exception("Possible serialization error encountered. Unexpected type");
                }
            }

            if (count != entities.Count)
                throw new Exception("Serialized and deserialized collection length does not match");

            return entities;
        }

        private static List<string> SplitEntitiesList(string input, Hypermedia parent)
        {
            List<string> entities = new List<string>();

            string tmpInput = input;
            tmpInput += "\r\n";
            bool isProcessed = false;
            while (!isProcessed)
            {
                bool isStringValid = false;
                string toReturn = string.Empty;

                string type = new string(tmpInput.Skip(1).TakeWhile(s => s != ':').ToArray());
                switch (type)
                {
                    case "file":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = File.IsSerializedStringValid(toReturn, parent);
                            string tmpEntity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                            if (tmpEntity.Length == tmpInput.Length)
                                throw new Exception("Possible serialization error encountered. Unexpected input.");
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    case "directory":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = Directory.IsSerializedStringValid(toReturn, parent);
                            string tmpEntity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                            if (tmpEntity.Length == tmpInput.Length)
                                throw new Exception("Possible serialization error encountered. Unexpected input.");
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    case "hypermedia":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = Hypermedia.IsSerializedStringValid(toReturn, parent);
                            string tmpEntity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                            if (tmpEntity.Length == tmpInput.Length)
                                throw new Exception("Possible serialization error encountered. Unexpected input.");
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    default:
                        throw new Exception("Possible serialization error encountered. Unexpected type");

                }
                string entity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                entities.Add(entity);
                tmpInput = tmpInput.Remove(0, entity.Length + 3);
                if (tmpInput == string.Empty)
                    isProcessed = true;
            }
            return entities;
        }

        private static string RemoveEntitiesList(string input)
        {
            string redacted = null;

            int start_block_index = input.IndexOf("(list<entity_interface>[");
            int end_block_index = input.LastIndexOf("},");
            redacted = input.Remove(start_block_index - 1,
                    (end_block_index + 3) - start_block_index - 1);
            return redacted;
        }

        private static string ExtractSerializedEntitiesList(string input)
        {
            string extracted = null;

            int start_block_index = input.IndexOf("(list<entity_interface>[");
            int end_block_index = input.LastIndexOf("},");

            int count = int.Parse(new string(input.Skip(start_block_index + 24).TakeWhile(s => s != ']').ToArray()));
            if (count != 0)
            {
                extracted = new string
                (
                    input.Skip
                    (
                        start_block_index + input.Skip(start_block_index)
                        .TakeWhile(s => s != '{').ToArray().Length + 1
                    ).Take(
                        (end_block_index - 2) - (start_block_index + input.Skip(start_block_index)
                        .TakeWhile(s => s != '{').Skip(1).ToArray().Length)
                    ).ToArray()
                );
            }
            return extracted;
        }

        private static int EntitiesListCount(string input)
        {
            int start_block_index = input.IndexOf("(list<entity_interface>[");
            int end_block_index = input.LastIndexOf("},");

            int count = int.Parse(new string(input.Skip(start_block_index + 24).TakeWhile(s => s != ']').ToArray()));
            return count;
        }
        #endregion Deserialization Algorithms
        #region Validation
        public static bool IsSerializedStringValid(string input, Hypermedia parent = null)
        {
            Encoding encoding = null;
            long cDateTime = 0;
            ulong size = 0;

            if (!input.StartsWith("["))
                return false;
            if (!input.EndsWith("]"))
                return false;

            string tmp = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');

            int count = -1;
            if (!TryParseEntitiesListCount(tmp, out count))
                return false;
            string entityList = ExtractSerializedEntitiesList(tmp);
            tmp = RemoveEntitiesList(tmp);

            var stringList = tmp.Split('\n').ToList();
            if (stringList.Count != 16)
                return false;

            foreach (var s in stringList)
            {
                if (!s.StartsWith("("))
                    return false;
            }

            for (int i = 0; i < 15; ++i)
            {
                if (!stringList[i].EndsWith(",\r"))
                    return false;
            }
            if (!stringList[15].EndsWith(";\r"))
                return false;

            //0
            if ((new string(stringList[0].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[0].Skip(8).TakeWhile(x => x != ')').ToArray())) != "path")
                return false;
            //3
            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "encoding")
                return false;

            if ((new string(stringList[3].Skip(10).TakeWhile(x => x != ')').ToArray())) != "encoding")
                return false;
            //1
            if ((new string(stringList[1].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[1].Skip(8).TakeWhile(x => x != ')').ToArray())) != "name")
                return false;
            //2
            if ((new string(stringList[2].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[2].Skip(8).TakeWhile(x => x != ')').ToArray())) != "comment")
                return false;
            //4
            if ((new string(stringList[4].Skip(1).TakeWhile(x => x != ':').ToArray())) != "date_time")
                return false;

            if ((new string(stringList[4].Skip(11).TakeWhile(x => x != ')').ToArray())) != "created_date_time")
                return false;
            //5
            if ((new string(stringList[5].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[5].Skip(8).TakeWhile(x => x != ')').ToArray())) != "created_by")
                return false;
            //6
            if ((new string(stringList[6].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[6].Skip(8).TakeWhile(x => x != ')').ToArray())) != "creator_peer")
                return false;
            //7
            if ((new string(stringList[7].Skip(1).TakeWhile(x => x != ':').ToArray())) != "boolean")
                return false;

            if ((new string(stringList[7].Skip(9).TakeWhile(x => x != ')').ToArray())) != "is_directory_wrapped")
                return false;
            //8
            if ((new string(stringList[8].Skip(1).TakeWhile(x => x != ':').ToArray())) != "boolean")
                return false;

            if ((new string(stringList[8].Skip(9).TakeWhile(x => x != ')').ToArray())) != "is_raw_ipfs")
                return false;
            //9
            if ((new string(stringList[9].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[9].Skip(8).TakeWhile(x => x != ')').ToArray())) != "topic")
                return false;
            //10
            if ((new string(stringList[10].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[10].Skip(8).TakeWhile(x => x != ')').ToArray())) != "default_subscription_message")
                return false;
            //11
            if ((new string(stringList[11].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[11].Skip(8).TakeWhile(x => x != ')').ToArray())) != "default_seeding_message")
                return false;
            //12
            if ((new string(stringList[12].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[12].Skip(8).TakeWhile(x => x != ')').ToArray())) != "version")
                return false;
            //13
            if ((new string(stringList[13].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
                return false;

            if ((new string(stringList[13].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
                return false;
            //14
            if ((new string(stringList[14].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[14].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
                return false;
            //15
            if ((new string(stringList[15].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[15].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
                return false;
            //TryParse
            try
            {
                encoding = Encoding.GetEncoding(new string(stringList[3].Skip(20).TakeWhile(x => x != ',').ToArray()));
            }
            catch
            {
                return false;
            }

            if (!long.TryParse(new string(stringList[4].Skip(30).TakeWhile(x => x != ',').ToArray()), out cDateTime))
                return false;

            if (!(new string(stringList[7].Skip(31).TakeWhile(x => x != ',').ToArray()) == "true" || new string(stringList[7].Skip(31).TakeWhile(x => x != ',').ToArray()) == "false"))
                return false;

            if (!(new string(stringList[8].Skip(22).TakeWhile(x => x != ',').ToArray()) == "true" || new string(stringList[8].Skip(22).TakeWhile(x => x != ',').ToArray()) == "false"))
                return false;

            if (!ulong.TryParse(new string(stringList[13].Skip(14).TakeWhile(x => x != ',').ToArray()), out size))
                return false;

            string ver = new string(stringList[12].Skip(17).TakeWhile(x => x != ',').ToArray());
            if (!ver.Contains("/"))
                return false;
            if (ver.Split('/').Length != 2)
                return false;

            string path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());

            string parent_path = new string(stringList[14].Skip(21).TakeWhile(x => x != ',').ToArray());
            if (parent != null)
                if (parent.Path != parent_path)
                    return false;

            Hypermedia hypermedia = new Hypermedia(parent)
            {
                Path = path,
                Encoding = encoding
            };
            bool isEntitiesValid = true;
            if (count > 0)
                isEntitiesValid = TryEntitiesListDeserializer(entityList, hypermedia, count);

            if (!isEntitiesValid)
                return false;

            return true;
        }

        private static bool TryEntitiesListDeserializer(string input, Hypermedia parent, int count)
        {
            List<IEntity> entities = new List<IEntity>();

            if (!input.StartsWith("["))
                return false;
            if (!input.EndsWith("]"))
                return false;

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            List<string> stringList = new List<string>();
            bool isEntitiesValid = TrySplitEntitiesList(input, parent, out stringList);
            if (!isEntitiesValid)
                return false;
            if (stringList.Count != count)
                return false;

            bool result = true;
            for (int i = 0; i < count; ++i)
            {
                string type = new string(stringList[i].Skip(1).TakeWhile(s => s != ':').ToArray());
                int index = int.Parse(new string(stringList[i].Skip(type.Length + 2).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                    return false;
                switch (type)
                {
                    case "file":
                        if (!File.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                            result = false;
                        break;
                    case "directory":
                        if (!Directory.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                            result = false;
                        break;
                    case "hypermedia":
                        if (!Hypermedia.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                            result = false;
                        break;
                    default:
                        return false;
                }
            }

            return result;
        }

        private static bool TrySplitEntitiesList(string input, Hypermedia parent, out List<string> entities)
        {
            entities = new List<string>();

            string tmpInput = input;
            tmpInput += "\r\n";
            bool isProcessed = false;
            while (!isProcessed)
            {
                bool isStringValid = false;
                string toReturn = string.Empty;

                string type = new string(tmpInput.Skip(1).TakeWhile(s => s != ':').ToArray());
                switch (type)
                {
                    case "file":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = File.IsSerializedStringValid(toReturn, parent);
                            if ((new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn).Length == tmpInput.Length)
                                return false;
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    case "directory":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = Directory.IsSerializedStringValid(toReturn, parent);
                            if ((new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn).Length == tmpInput.Length)
                                return false;
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    case "hypermedia":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = Hypermedia.IsSerializedStringValid(toReturn, parent);
                            if ((new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn).Length == tmpInput.Length)
                                return false;
                            if (!isStringValid)
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                        }
                        while (!isStringValid);
                        break;
                    default:
                        return false;

                }
                string entity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                entities.Add(entity);
                int il = tmpInput.Length;
                tmpInput = tmpInput.Remove(0, entity.Length + 3);
                if (tmpInput == string.Empty)
                    isProcessed = true;
            }
            if (entities.Count <= 0)
                return false;
            return true;
        }

        private static bool TryParseEntitiesListCount(string input, out int count)
        {
            count = -1;
            if (!input.Contains("(list<entity_interface>["))
                return false;
            int start_block_index = input.IndexOf("(list<entity_interface>[");
            if (!input.Contains("},"))
                return false;
            int end_block_index = input.IndexOf("},");

            if (!int.TryParse(new string(input.Skip(start_block_index + 24).TakeWhile(s => s != ']').ToArray()), out count))
                return false;
            return true;
        }
        #endregion Validation
    }
}
