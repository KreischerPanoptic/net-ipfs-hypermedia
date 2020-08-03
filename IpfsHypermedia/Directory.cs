using Ipfs.Hypermedia.Cryptography;
using Ipfs.Hypermedia.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Representation of Directory in both IPFS distributed file system and OS file system.
    /// </summary>
    public sealed class Directory : IEntity, ISystemEntity
    {
        /// <summary>
        ///   Path for this directory in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this directory.
        /// </remarks>
        public string Path { get; set; }
        private string _name;
        /// <summary>
        ///   Human readable name of directory.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != null)
                {
                    if (value.Length > 255)
                        throw new Exception("Directory name can not have more than 255 symbols");
                    if (value.Length <= 0)
                        throw new Exception("Directory name can not be empty");
                    _name = value;
                }
                else
                    throw new Exception("Directory name can not be null");
            }
        }
        private FileAttributes? _attributes;
        /// <summary>
        ///   Attributes of directory.
        /// </summary>
        /// <remarks>
        ///   Made nullable to be compatible with raw ipfs directories which doesn't have such information.
        ///   ///   For file appliable attributes is Directory, Directory|Hidden, Directory|Read-Only and Directory|Hidden|Read-Only to be compatible with all major platforms.
        /// </remarks>
        public FileAttributes? Attributes 
        {
            get { return _attributes; }
            set
            {
                if (!value.HasValue)
                    _attributes = value;
                else if (value.Value == FileAttributes.Directory)
                    _attributes = value;
                else if (value.Value == (FileAttributes.Directory| FileAttributes.Hidden) || value.Value == (FileAttributes.Directory | FileAttributes.ReadOnly))
                    _attributes = value;
                else if (value.Value == (FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.ReadOnly))
                    _attributes = value;
                else
                    throw new Exception("Directory attributes can be only Directory, Directory|Hidden, Directory|Read-Only and Directory|Hidden|Read-Only");
            }
        }
        private DateTime? _lastModifiedDateTime;
        /// <summary>
        ///   Last modification date and time for directory in UTC.
        /// </summary>
        /// <remarks>
        ///   Serializes to total seconds passed from Unix Epoch.
        ///   Made nullable to be compatible with raw ipfs directories which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTime
        {
            get { return _lastModifiedDateTime; }
            set
            {
                if (!value.HasValue)
                    _lastModifiedDateTime = value;
                else
                    _lastModifiedDateTime = value.Value.ToUniversalTime();
            }
        }
        /// <summary>
        ///   List of <see cref="ISystemEntity">System Enteties</see> of directory.
        /// </summary>
        /// <remarks>
        ///   Due to limitation, directories can store only file system entities that have basic information for OS.
        /// </remarks>
        public List<ISystemEntity> Entities { get; set; } = new List<ISystemEntity>();
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
            get { return _parent; }
            set
            {
                if (value != null)
                {
                    if (value is File)
                        throw new ArgumentException("File can't be parent of the directory!");
                    _parent = value;
                }
                else
                    throw new ArgumentException("Parent must be set for directory!");
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
        /// <summary>
        ///   Serializes passed directory to string using passed encoding.
        /// </summary>
        /// <param name="directory">
        ///   Directory to be serialized.
        /// </param>
        /// <param name="encoding">
        ///   Encoding used for serialization of directory name.
        ///   Usually passed from parent <see cref="Hypermedia">hypermedia</see> where directory resides.
        /// </param>
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <param name="tabulationCount">
        ///   Internal argument for count of tabulations.
        /// </param>
        public static string SerializeToString(Directory directory, Encoding encoding, Formatting formatting = Formatting.None, uint tabulationCount = 0)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                innerTabulationBuilder += '\t';
                for (int i = 0; i < tabulationCount; ++i)
                {
                    outerTabulationBuilder += '\t';
                    innerTabulationBuilder += '\t';
                }
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            builder.AppendLine($"{innerTabulationBuilder}(string:path)={directory.Path},");
            builder.AppendLine($"{innerTabulationBuilder}(string:name)={EncodingTools.EncodeString(directory.Name, encoding)},");
            builder.AppendLine($"{innerTabulationBuilder}(file_attributes_null:attributes)={FileAttributesSerializer(directory.Attributes)},");
            builder.AppendLine($"{innerTabulationBuilder}(date_time_null:last_modified_date_time)={(directory.LastModifiedDateTime is null ? "null" : ((DateTimeOffset)directory.LastModifiedDateTime.Value).ToUnixTimeSeconds().ToString())},");
            builder.AppendLine($"{innerTabulationBuilder}(list<system_entity_interface>[{directory.Entities.Count}]:entities)=" + "{" + (directory.Entities.Count <= 0 ? "empty;" : SystemEntitiesListSerializer(directory.Entities, encoding, formatting, tabulationCount + 1)) + "},");
            builder.AppendLine($"{innerTabulationBuilder}(uint64:size)={directory.Size},");
            builder.AppendLine($"{innerTabulationBuilder}(string:parent_path)={directory.Parent.Path},");
            builder.AppendLine($"{innerTabulationBuilder}(string:hash)={directory.Hash};");
            builder.Append($"{outerTabulationBuilder}]");
            return builder.ToString();
        }
        #region Serialization Algorithms
        private static string FileAttributesSerializer(FileAttributes? attributes)
        {
            if (attributes is null)
                return "null";
            switch (attributes.Value)
            {
                case FileAttributes.Directory | FileAttributes.Hidden:
                    return "directory|hidden";
                case FileAttributes.Directory | FileAttributes.ReadOnly:
                    return "directory|read-only";
                case FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.ReadOnly:
                    return "directory|hidden|read-only";
                case FileAttributes.Directory:
                default:
                    return "directory";
            }
        }

        private static string SystemEntitiesListSerializer(List<ISystemEntity> entities, Encoding encoding, Formatting formatting = Formatting.None, uint tabulationCount = 0)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                innerTabulationBuilder += '\t';
                for (int i = 0; i < tabulationCount; ++i)
                {
                    innerTabulationBuilder += '\t';
                    outerTabulationBuilder += '\t';
                }
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            for (int i = 0; i < entities.Count; ++i)
            {
                if(entities[i] is File)
                    builder.AppendLine($"{innerTabulationBuilder}(file:{i})={File.SerializeToString(entities[i] as File, encoding, formatting, tabulationCount + 1)}{(i == entities.Count - 1 ? ";" : ",")}");
                else if(entities[i] is Directory)
                    builder.AppendLine($"{innerTabulationBuilder}(directory:{i})={Directory.SerializeToString(entities[i] as Directory, encoding, formatting, tabulationCount + 1)}{(i == entities.Count - 1 ? ";" : ",")}");
            }
            builder.Append($"{outerTabulationBuilder}]");
            return builder.ToString();
        }
        #endregion Serialization Algorithms
        /// <summary>
        ///   Deserializes passed string to directory.
        /// </summary>
        /// <param name="input">
        ///   String to be deserialized.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="IEntity">entity</see> for directory.
        /// </param>
        /// <param name="encoding">
        ///   Encoding used for deserialization of directory name.
        ///   Usually passed from parent <see cref="Hypermedia">hypermedia</see> where directory resides during deserialization of parent hypermedia.
        /// </param>
        public static Directory DeserializeFromString(string input, IEntity parent, Encoding encoding)
        {
            string path = null;
            string name = null;
            FileAttributes? attributes = null;
            long lastModDateTime = 0;
            DateTime? lastModifiedDateTime = null;
            List<ISystemEntity> entities = new List<ISystemEntity>();
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            input = input.Replace("\t", "");

            if (!input.StartsWith("["))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.");
            if (!input.EndsWith("]"))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.");

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');

            string entitiesList = ExtractSerializedSystemEntitiesList(input);
            int count = SystemEntitiesListCount(input);
            input = RemoveSystemEntitiesList(input);
            var stringList = input.Split('\n').ToList();

            path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());
            name = EncodingTools.DecodeString(new string(stringList[1].Skip(14).TakeWhile(x => x != ',').ToArray()), encoding);
            attributes = FileAttributesDeserializer(new string(stringList[2].Skip(34).TakeWhile(x => x != ',').ToArray()));
            lastModDateTime = long.Parse(new string(stringList[3].Skip(41).TakeWhile(x => x != ',').ToArray()));
            lastModifiedDateTime = DateTimeOffset.FromUnixTimeSeconds(lastModDateTime).UtcDateTime;
            size = ulong.Parse(new string(stringList[4].Skip(14).TakeWhile(x => x != ',').ToArray()));
            parent_path = new string(stringList[5].Skip(21).TakeWhile(x => x != ',').ToArray());
            hash = new string(stringList[6].Skip(14).TakeWhile(x => x != ';').ToArray());

            if (parent.Path != parent_path)
                throw new ArgumentException("Deserialized parent path is not the expected one");

            Directory directory = new Directory()
            {
                Path = path,
                Name = name,
                Attributes = attributes,
                LastModifiedDateTime = lastModifiedDateTime,
                Size = size,
                Parent = parent,
                Hash = hash
            };
            if (count != 0)
                entities = SystemEntitiesListDeserializer(entitiesList, directory, count, encoding);

            directory.Entities = entities;
            return directory;
        }
        #region Deserialization Algorithms
        private static FileAttributes? FileAttributesDeserializer(string input)
        {
            if (input == "null")
                return null;
            switch (input)
            {
                case "directory|hidden":
                    return FileAttributes.Directory | FileAttributes.Hidden;
                case "directory|read-only":
                    return FileAttributes.Directory | FileAttributes.ReadOnly;
                case "directory|hidden|read-only":
                    return FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.ReadOnly;
                case "directory":
                default:
                    return FileAttributes.Directory;
            }
        }

        private static List<ISystemEntity> SystemEntitiesListDeserializer(string input, Directory parent, int count, Encoding encoding)
        {
            List<ISystemEntity> entities = new List<ISystemEntity>();

            if (!input.StartsWith("["))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.");
            if (!input.EndsWith("]"))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.");

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            var stringList = SplitSystemEntitiesList(input, parent);
            if (stringList.Count != count)
                throw new Exception("Parsed string list does not match expected length");

            for(int i = 0; i < count; ++i)
            {
                string type = new string(stringList[i].Skip(1).TakeWhile(s => s != ':').ToArray());
                int index = int.Parse(new string(stringList[i].Skip(type.Length + 2).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                    throw new Exception("Possible serialization error encountered. Unexpected sequence");
                switch (type)
                {
                    case "file":
                        entities.Add(File.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, encoding));
                        break;
                    case "directory":
                        entities.Add(Directory.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent, encoding));
                        break;
                    default:
                        throw new Exception("Possible serialization error encountered. Unexpected type");
                }
            }

            if (count != entities.Count)
                throw new Exception("Serialized and deserialized collection length does not match");

            return entities;
        }

        private static List<string> SplitSystemEntitiesList(string input, Directory parent)
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

        private static string RemoveSystemEntitiesList(string input)
        {
            string redacted = null;

            int start_block_index = input.IndexOf("(list<system_entity_interface>[");
            int end_block_index = input.LastIndexOf("},");
            redacted = input.Remove(start_block_index - 1,
                    (end_block_index + 3) - start_block_index - 1);
            return redacted;
        }

        private static string ExtractSerializedSystemEntitiesList(string input)
        {
            string extracted = null;

            int start_block_index = input.IndexOf("(list<system_entity_interface>[");
            int end_block_index = input.LastIndexOf("},");

            int count = int.Parse(new string(input.Skip(start_block_index + 31).TakeWhile(s => s != ']').ToArray()));
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

        private static int SystemEntitiesListCount(string input)
        {
            int start_block_index = input.IndexOf("(list<system_entity_interface>[");
            int end_block_index = input.LastIndexOf("},");

            int count = int.Parse(new string(input.Skip(start_block_index + 31).TakeWhile(s => s != ']').ToArray()));
            return count;
        }
        #endregion Deserialization Algorithms
        #region Validation
        public static bool IsSerializedStringValid(string input, IEntity parent)
        {
            long lastModDateTime = 0;
            ulong size = 0;

            if (!input.StartsWith("["))
                return false;
            if (!input.EndsWith("]"))
                return false;

            string tmp = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');

            int count = -1;
            if (!TryParseSystemEntitiesListCount(tmp, out count))
                return false;
            string entityList = ExtractSerializedSystemEntitiesList(tmp);
            tmp = RemoveSystemEntitiesList(tmp);

            var stringList = tmp.Split('\n').ToList();
            if (stringList.Count != 7)
                return false;

            foreach (var s in stringList)
            {
                if (!s.StartsWith("("))
                    return false;
            }

            for (int i = 0; i < 6; ++i)
            {
                if (!stringList[i].EndsWith(",\r"))
                    return false;
            }
            if (!stringList[6].EndsWith(";\r"))
                return false;

            //0
            if ((new string(stringList[0].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[0].Skip(8).TakeWhile(x => x != ')').ToArray())) != "path")
                return false;
            //1
            if ((new string(stringList[1].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[1].Skip(8).TakeWhile(x => x != ')').ToArray())) != "name")
                return false;
            //2
            if ((new string(stringList[2].Skip(1).TakeWhile(x => x != ':').ToArray())) != "file_attributes_null")
                return false;

            if ((new string(stringList[2].Skip(22).TakeWhile(x => x != ')').ToArray())) != "attributes")
                return false;
            //3
            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "date_time_null")
                return false;

            if ((new string(stringList[3].Skip(16).TakeWhile(x => x != ')').ToArray())) != "last_modified_date_time")
                return false;
            //4
            if ((new string(stringList[4].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
                return false;

            if ((new string(stringList[4].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
                return false;
            //5
            if ((new string(stringList[5].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[5].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
                return false;
            //6
            if ((new string(stringList[6].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[6].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
                return false;
            //TryParse
            if (!long.TryParse(new string(stringList[3].Skip(41).TakeWhile(x => x != ',').ToArray()), out lastModDateTime))
                return false;

            if (!ulong.TryParse(new string(stringList[4].Skip(14).TakeWhile(x => x != ',').ToArray()), out size))
                return false;

            string path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());

            string parent_path = new string(stringList[5].Skip(21).TakeWhile(x => x != ',').ToArray());
            if (parent.Path != parent_path)
                return false;

            Directory directory = new Directory()
            {
                Path = path
            };
            bool isEntitiesValid = true;
            if (count > 0)
                isEntitiesValid = TrySystemEntitiesListDeserializer(entityList, directory, count);

            if (!isEntitiesValid)
                return false;

            return true;
        }

        private static bool TrySystemEntitiesListDeserializer(string input, Directory parent, int count)
        {
            List<ISystemEntity> entities = new List<ISystemEntity>();

            if (!input.StartsWith("["))
                return false;
            if (!input.EndsWith("]"))
                return false;

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            List<string> stringList = new List<string>();
            bool isEntitiesValid = TrySplitSystemEntitiesList(input, parent, out stringList);
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
                    default:
                        return false;
                }
            }

            return result;
        }

        private static bool TrySplitSystemEntitiesList(string input, Directory parent, out List<string> entities)
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

        private static bool TryParseSystemEntitiesListCount(string input, out int count)
        {
            count = -1;
            if (!input.Contains("(list<system_entity_interface>["))
                return false;
            int start_block_index = input.IndexOf("(list<system_entity_interface>[");
            if (!input.Contains("},"))
                return false;
            int end_block_index = input.IndexOf("},");

            if (!int.TryParse(new string(input.Skip(start_block_index + 31).TakeWhile(s => s != ']').ToArray()), out count))
                return false;
            return true;
        }
        #endregion Validation
    }
}
