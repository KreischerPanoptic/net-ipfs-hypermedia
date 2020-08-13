using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ipfs.Hypermedia.Tools;

namespace Ipfs.Hypermedia.Serialization.Versions
{
    internal class HypermediaVersion10 : ISerializationVersion
    {
        private const string _startOfEntityListDeclaration = "(list<entity_interface>[";
        private const string _endOfEntityListDeclaration = "},";
        /// <summary>
        ///   Serializes passed hypermedia to string using passed encoding.
        /// </summary>
        /// <param name="hypermedia">
        ///   Hypermedia to be serialized.
        /// </param>
        public string SerializeToString(Hypermedia hypermedia)
        {
            return SerializeToString(hypermedia, Formatting.None, 0);
        }
        /// <summary>
        ///   Serializes passed hypermedia to string using passed encoding.
        /// </summary>
        /// <param name="hypermedia">
        ///   Hypermedia to be serialized.
        /// </param>
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <param name="tabulationCount">
        ///   Internal argument for count of tabulations.
        /// </param>
        public string SerializeToString(Hypermedia hypermedia, Formatting formatting, uint tabulationsCount)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                SerializationTools.InitTabulations(out outerTabulationBuilder, out innerTabulationBuilder, tabulationsCount);
            }
            StringBuilder builder = new StringBuilder();
            SerializationTools.InitStartBaseHypermediaSerializationStrings(ref builder, hypermedia, outerTabulationBuilder, innerTabulationBuilder);
            builder.AppendLine($"{innerTabulationBuilder}(string:comment)={(hypermedia.Comment is null ? "null" : EncodingTools.EncodeString(hypermedia.Comment, hypermedia.Encoding is null ? Encoding.UTF8 : hypermedia.Encoding))},");
            builder.AppendLine($"{innerTabulationBuilder}(encoding:encoding)={(hypermedia.Encoding is null ? "utf-8" : hypermedia.Encoding.WebName)},");
            builder.AppendLine($"{innerTabulationBuilder}(date_time:created_date_time)={((DateTimeOffset)hypermedia.CreatedDateTime).ToUnixTimeSeconds()},");
            builder.AppendLine($"{innerTabulationBuilder}(string:created_by)={hypermedia.CreatedBy},");
            builder.AppendLine($"{innerTabulationBuilder}(string:creator_peer)={(hypermedia.CreatorPeer is null ? "null" : hypermedia.CreatorPeer)},");
            if (hypermedia.Entities.Count <= 0)
            {
                throw new ArgumentException("Hypermedia entities list can not be empty", nameof(hypermedia));
            }
            builder.AppendLine($"{innerTabulationBuilder}{_startOfEntityListDeclaration}{hypermedia.Entities.Count}]:entities)=" + "{" +
                (formatting == Formatting.Indented
                ? EntityListSerializer(
                    hypermedia.Entities, hypermedia.Encoding is null
                    ? Encoding.UTF8
                    : hypermedia.Encoding, formatting, tabulationsCount + 1
                    )
                : EntityListSerializer(
                    hypermedia.Entities, hypermedia.Encoding is null
                    ? Encoding.UTF8
                    : hypermedia.Encoding
                    )
                ) + $"{_endOfEntityListDeclaration}");
            builder.AppendLine($"{innerTabulationBuilder}(boolean:is_directory_wrapped)={(hypermedia.IsDirectoryWrapped ? "true" : "false")},");
            builder.AppendLine($"{innerTabulationBuilder}(boolean:is_raw_ipfs)={(hypermedia.IsRawIPFS ? "true" : "false")},");
            builder.AppendLine($"{innerTabulationBuilder}(string:topic)={(hypermedia.Topic is null ? "null" : hypermedia.Topic)},");
            builder.AppendLine($"{innerTabulationBuilder}(string:default_subscription_message)={(hypermedia.DefaultSubscriptionMessage is null ? "subscribed" : hypermedia.DefaultSubscriptionMessage)},");
            builder.AppendLine($"{innerTabulationBuilder}(string:default_seeding_message)={(hypermedia.DefaultSeedingMessage is null ? "seeding" : hypermedia.DefaultSeedingMessage)},");
            builder.AppendLine($"{innerTabulationBuilder}(string:version)={hypermedia.Version},");
            SerializationTools.InitEndBaseSerializationStrings(ref builder, hypermedia, outerTabulationBuilder, innerTabulationBuilder);
            return builder.ToString();
        }
        #region Serialization Algorithms
        private string EntityListSerializer(List<IEntity> entities, Encoding encoding)
        {
            return EntityListSerializer(entities, encoding, Formatting.None, 0);
        }
        private string EntityListSerializer(List<IEntity> entities, Encoding encoding, Formatting formatting, uint tabulationsCount)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                SerializationTools.InitTabulations(out outerTabulationBuilder, out innerTabulationBuilder, tabulationsCount);
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            for (int i = 0; i < entities.Count; ++i)
            {
                if (entities[i] is File)
                {
                    builder.AppendLine($"{innerTabulationBuilder}(file:{i})={(formatting == Formatting.Indented ? File.SerializeToString(entities[i] as File, encoding, formatting, tabulationsCount + 1) : File.SerializeToString(entities[i] as File, encoding))}{(i == entities.Count - 1 ? ";" : ",")}");
                }
                else if (entities[i] is Directory)
                {
                    builder.AppendLine($"{innerTabulationBuilder}(directory:{i})={(formatting == Formatting.Indented ? Directory.SerializeToString(entities[i] as Directory, encoding, formatting, tabulationsCount + 1) : Directory.SerializeToString(entities[i] as Directory, encoding))}{(i == entities.Count - 1 ? ";" : ",")}");
                }
                else if (entities[i] is Hypermedia)
                {
                    var serializator = VersionTools.GetSerializationVersion((entities[i] as Hypermedia).Version);
                    builder.AppendLine($"{innerTabulationBuilder}(hypermedia:{i})={(formatting == Formatting.Indented ? serializator.SerializeToString(entities[i] as Hypermedia, formatting, tabulationsCount + 1) : serializator.SerializeToString(entities[i] as Hypermedia))}{(i == entities.Count - 1 ? ";" : ",")}");
                }
            }
            builder.Append($"{outerTabulationBuilder}]");
            return builder.ToString();
        }
        #endregion Serialization Algorithms
        public Hypermedia DeserializeFromString(string input)
        {
            return DeserializeFromString(input, null);
        }
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
        public Hypermedia DeserializeFromString(string input, Hypermedia parent)
        {
            string path = null;
            string name = null;
            string comment = null;
            Encoding encoding = null;
            long cDateTime = 0;
            DateTime createdDateTime;
            string createdBy = null;
            string creatorPeer = null;
            List<IEntity> entities;
            bool isDirectoryWrapped = false;
            bool isRawIPFS = false;
            string topic = null;
            string defaultSubscriptionMessage = null;
            string defaultSeedingMessage = null;
            string version = null;
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            input = input.Replace("\t", "");

            DeserializationTools.CheckStringFormat(input, false);

            int count;
            string entitiesList;
            List<string> stringList;
            DeserializationTools.SplitStringForHypermedia(input, _startOfEntityListDeclaration, _endOfEntityListDeclaration, 24, out count, out entitiesList, out stringList, false);

            DeserializationTools.ParseStartBaseHypermediaSerializationString(stringList, out path, out encoding, out name);
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
            DeserializationTools.ParseEndBaseSerializationString(stringList, 13, out size, out parent_path, out hash);

            if (parent != null)
            {
                DeserializationTools.CheckParent(parent, parent_path, false);
            }

            Hypermedia hypermedia = new Hypermedia
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
                Parent = parent,
                Hash = hash
            };

            entities = EntitiesListDeserializer(entitiesList, hypermedia, count);

            hypermedia.Entities = entities;
            return hypermedia;
        }
        #region Deserialization Algorithms
        private List<IEntity> EntitiesListDeserializer(string input, Hypermedia parent, int count)
        {
            List<IEntity> entities = new List<IEntity>();

            DeserializationTools.CheckStringFormat(input, false);

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            var stringList = SplitEntitiesList(input, parent);
            if (stringList.Count != count)
            {
                throw new ArgumentException("Parsed string list does not match expected length", "count");
            }

            for (int i = 0; i < count; ++i)
            {
                string type = new string(stringList[i].Skip(1).TakeWhile(s => s != ':').ToArray());
                int index = int.Parse(new string(stringList[i].Skip(type.Length + 2).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                {
                    throw new ArgumentException("Possible serialization error encountered. Unexpected sequence", "input");
                }
                switch (type)
                {
                    case "file":
                        entities.Add(File.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, parent.Encoding));
                        break;
                    case "directory":
                        entities.Add(Directory.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, parent.Encoding));
                        break;
                    case "hypermedia":
                        var deserializer = VersionTools.GetSerializationVersion(VersionTools.GetVersion(new string(stringList[i].SkipWhile(s => s != '[').ToArray())));
                        entities.Add(deserializer.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent));
                        break;
                    default:
                        throw new ArgumentException("Possible serialization error encountered. Unexpected type", "input");
                }
            }

            if (count != entities.Count)
            {
                throw new ArgumentException("Serialized and deserialized collection length does not match", "count");
            }

            return entities;
        }

        private List<string> SplitEntitiesList(string input, Hypermedia parent)
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
                            {
                                throw new ArgumentException("Possible serialization error encountered. Unexpected input.", "input");
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
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
                            {
                                throw new ArgumentException("Possible serialization error encountered. Unexpected input.", "input");
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
                        }
                        while (!isStringValid);
                        break;
                    case "hypermedia":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            var versions = VersionTools.GetVersions();
                            foreach(var v in versions)
                            {
                                isStringValid = v.IsSerializedStringValid(toReturn, parent);
                                if(isStringValid)
                                {
                                    break;
                                }
                            }
                            
                            string tmpEntity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                            if (tmpEntity.Length == tmpInput.Length)
                            {
                                throw new ArgumentException("Possible serialization error encountered. Unexpected input.", "input");
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
                        }
                        while (!isStringValid);
                        break;
                    default:
                        throw new ArgumentException("Possible serialization error encountered. Unexpected type", "input");

                }
                string entity = new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn;
                entities.Add(entity);
                tmpInput = tmpInput.Remove(0, entity.Length + 3);
                if (tmpInput == string.Empty)
                {
                    isProcessed = true;
                }
            }
            return entities;
        }
        #endregion Deserialization Algorithms
        #region Validation
        public bool IsSerializedStringValid(string input, Hypermedia parent = null)
        {
            Encoding encoding = null;
            long cDateTime = 0;
            ulong size = 0;

            if (!DeserializationTools.CheckStringFormat(input, true))
            {
                return false;
            }


            int count;
            string entitiesList;
            List<string> stringList;

            if (!DeserializationTools.SplitStringForHypermedia(input, _startOfEntityListDeclaration, _endOfEntityListDeclaration, 24, out count, out entitiesList, out stringList, true))
            {
                return false;
            }

            if (stringList.Count != 16)
            {
                return false;
            }

            if (!DeserializationTools.ValidateStartOfStrings(stringList))
            {
                return false;
            }

            if (!DeserializationTools.ValidateEndOfStrings(stringList, 15))
            {
                return false;
            }

            //0
            if ((new string(stringList[0].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[0].Skip(8).TakeWhile(x => x != ')').ToArray())) != "path")
            {
                return false;
            }
            //3
            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "encoding")
            {
                return false;
            }

            if ((new string(stringList[3].Skip(10).TakeWhile(x => x != ')').ToArray())) != "encoding")
            {
                return false;
            }
            //1
            if ((new string(stringList[1].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[1].Skip(8).TakeWhile(x => x != ')').ToArray())) != "name")
            {
                return false;
            }
            //2
            if ((new string(stringList[2].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[2].Skip(8).TakeWhile(x => x != ')').ToArray())) != "comment")
            {
                return false;
            }
            //4
            if ((new string(stringList[4].Skip(1).TakeWhile(x => x != ':').ToArray())) != "date_time")
            {
                return false;
            }

            if ((new string(stringList[4].Skip(11).TakeWhile(x => x != ')').ToArray())) != "created_date_time")
            {
                return false;
            }
            //5
            if ((new string(stringList[5].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[5].Skip(8).TakeWhile(x => x != ')').ToArray())) != "created_by")
            {
                return false;
            }
            //6
            if ((new string(stringList[6].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[6].Skip(8).TakeWhile(x => x != ')').ToArray())) != "creator_peer")
            {
                return false;
            }
            //7
            if ((new string(stringList[7].Skip(1).TakeWhile(x => x != ':').ToArray())) != "boolean")
            {
                return false;
            }

            if ((new string(stringList[7].Skip(9).TakeWhile(x => x != ')').ToArray())) != "is_directory_wrapped")
            {
                return false;
            }
            //8
            if ((new string(stringList[8].Skip(1).TakeWhile(x => x != ':').ToArray())) != "boolean")
            {
                return false;
            }

            if ((new string(stringList[8].Skip(9).TakeWhile(x => x != ')').ToArray())) != "is_raw_ipfs")
            {
                return false;
            }
            //9
            if ((new string(stringList[9].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[9].Skip(8).TakeWhile(x => x != ')').ToArray())) != "topic")
            {
                return false;
            }
            //10
            if ((new string(stringList[10].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[10].Skip(8).TakeWhile(x => x != ')').ToArray())) != "default_subscription_message")
            {
                return false;
            }
            //11
            if ((new string(stringList[11].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[11].Skip(8).TakeWhile(x => x != ')').ToArray())) != "default_seeding_message")
            {
                return false;
            }
            //12
            if ((new string(stringList[12].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[12].Skip(8).TakeWhile(x => x != ')').ToArray())) != "version")
            {
                return false;
            }
            //13
            if ((new string(stringList[13].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
            {
                return false;
            }

            if ((new string(stringList[13].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
            {
                return false;
            }
            //14
            if ((new string(stringList[14].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[14].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
            {
                return false;
            }
            //15
            if ((new string(stringList[15].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[15].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
            {
                return false;
            }
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
            {
                return false;
            }

            if (!(new string(stringList[7].Skip(31).TakeWhile(x => x != ',').ToArray()) == "true" || new string(stringList[7].Skip(31).TakeWhile(x => x != ',').ToArray()) == "false"))
            {
                return false;
            }

            if (!(new string(stringList[8].Skip(22).TakeWhile(x => x != ',').ToArray()) == "true" || new string(stringList[8].Skip(22).TakeWhile(x => x != ',').ToArray()) == "false"))
            {
                return false;
            }

            if (!ulong.TryParse(new string(stringList[13].Skip(14).TakeWhile(x => x != ',').ToArray()), out size))
            {
                return false;
            }

            string ver = new string(stringList[12].Skip(17).TakeWhile(x => x != ',').ToArray());
            if (!ver.Contains("/"))
            {
                return false;
            }
            if (ver.Split('/').Length != 2)
            {
                return false;
            }

            string path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());

            string parent_path = new string(stringList[14].Skip(21).TakeWhile(x => x != ',').ToArray());
            if (parent != null)
            {
                if (!DeserializationTools.CheckParent(parent, parent_path, true))
                {
                    return false;
                }
            }

            Hypermedia hypermedia = new Hypermedia
            {
                Path = path,
                Encoding = encoding,
                Parent = parent
            };
            bool isEntitiesValid = true;
            if (count > 0)
            {
                isEntitiesValid = TryEntitiesListDeserializer(entitiesList, hypermedia, count);
            }

            if (!isEntitiesValid)
            {
                return false;
            }

            return true;
        }

        private bool TryEntitiesListDeserializer(string input, Hypermedia parent, int count)
        {
            List<IEntity> entities = new List<IEntity>();

            if (!DeserializationTools.CheckStringFormat(input, true))
            {
                return false;
            }

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            List<string> stringList = new List<string>();
            bool isEntitiesValid = TrySplitEntitiesList(input, parent, out stringList);
            if (!isEntitiesValid)
            {
                return false;
            }
            if (stringList.Count != count)
            {
                return false;
            }

            bool result = true;
            for (int i = 0; i < count; ++i)
            {
                string type = new string(stringList[i].Skip(1).TakeWhile(s => s != ':').ToArray());
                int index = int.Parse(new string(stringList[i].Skip(type.Length + 2).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                {
                    return false;
                }
                switch (type)
                {
                    case "file":
                        if (!File.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                        {
                            result = false;
                        }
                        break;
                    case "directory":
                        if (!Directory.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                        {
                            result = false;
                        }
                        break;
                    case "hypermedia":
                        var deserializer = VersionTools.GetSerializationVersion(VersionTools.GetVersion(new string(stringList[i].SkipWhile(s => s != '[').ToArray())));
                        if (!deserializer.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                        {
                            result = false;
                        }
                        break;
                    default:
                        return false;
                }
            }

            return result;
        }

        private bool TrySplitEntitiesList(string input, Hypermedia parent, out List<string> entities)
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
                            {
                                return false;
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
                        }
                        while (!isStringValid);
                        break;
                    case "directory":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            isStringValid = Directory.IsSerializedStringValid(toReturn, parent);
                            if ((new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn).Length == tmpInput.Length)
                            {
                                return false;
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
                        }
                        while (!isStringValid);
                        break;
                    case "hypermedia":
                        toReturn = new string(tmpInput.SkipWhile(s => s != '[').Take(1).ToArray());
                        do
                        {
                            var versions = VersionTools.GetVersions();
                            foreach (var v in versions)
                            {
                                isStringValid = v.IsSerializedStringValid(toReturn, parent);
                                if (isStringValid)
                                {
                                    break;
                                }
                            }

                            if ((new string(tmpInput.TakeWhile(s => s != '[').ToArray()) + toReturn).Length == tmpInput.Length)
                            {
                                return false;
                            }
                            if (!isStringValid)
                            {
                                toReturn += new string(tmpInput.Skip(new string(tmpInput.TakeWhile(s => s != '[').ToArray()).Length + toReturn.Length).Take(1).ToArray());
                            }
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
                {
                    isProcessed = true;
                }
            }
            if (entities.Count <= 0)
            {
                return false;
            }
            return true;
        }
        #endregion Validation
    }
}
