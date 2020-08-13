using Ipfs.Hypermedia.Cryptography;
using Ipfs.Hypermedia.Tools;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Representation of File in both IPFS distributed file system and OS file system.
    /// </summary>
    public sealed class File : IEntity, ISystemEntity
    {
        /// <summary>
        ///   Path for this file in IPFS distributed file system.
        /// </summary>
        /// <remarks>
        ///   Path ONLY for this file.
        /// </remarks>
        public string Path { get; set; }
        private string _name;
        /// <summary>
        ///   Human readable name of file.
        /// </summary>
        /// <remarks>
        ///   It is name of file without extension.
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
                        throw new ArgumentException("File name can not have more than 255 symbols");
                    }
                    _name = value;
                }
                else
                {
                    throw new ArgumentException("File name can not be null. If file do not have name - use empty string");
                }
            }
        }
        private string _extension;
        /// <summary>
        ///   Extension of the file for identification by OS.
        /// </summary>
        /// <remarks>
        ///   It contains ONLY extension.
        /// </remarks>
        public string Extension 
        { 
            get { return _extension; } 
            set
            {
                if (value != null)
                {
                    if (value.Length > 255)
                    {
                        throw new ArgumentException("File extension can not have more than 255 symbols");
                    }
                    _extension = value;
                }
                else
                {
                    throw new ArgumentException("File extension can not be null. If file do not have extension - use empty string");
                }
            }
        }
        private FileAttributes? _attributes;
        /// <summary>
        ///   Attributes of file.
        /// </summary>
        /// <remarks>
        ///   Made nullable to be compatible with raw ipfs files which doesn't have such information.
        ///   For file appliable attributes is Normal, Hidden, Read-Only and Hidden|Read-Only to be compatible with all major platforms.
        /// </remarks>
        public FileAttributes? Attributes 
        { 
            get { return _attributes; }
            set
            {
                if (!value.HasValue)
                {
                    _attributes = value;
                }
                else
                {
                    switch(value.Value)
                    {
                        case FileAttributes.Normal:
                        case FileAttributes.Hidden:
                        case FileAttributes.ReadOnly:
                        case FileAttributes.Hidden | FileAttributes.ReadOnly:
                            _attributes = value;
                            break;
                        default:
                            throw new ArgumentException("File attributes can be only Normal, Hidden, Read-Only and Hidden|Read-Only");

                    }
                }
            } 
        }
        private DateTime? _lastModifiedDateTime;
        /// <summary>
        ///   Last modification date and time for file in UTC.
        /// </summary>
        /// <remarks>
        ///   Serializes to total seconds passed from Unix Epoch.
        ///   Made nullable to be compatible with raw ipfs files which doesn't have such information.
        /// </remarks>
        public DateTime? LastModifiedDateTime 
        {
            get { return _lastModifiedDateTime; }
            set
            {
                if (!value.HasValue)
                {
                    _lastModifiedDateTime = value;
                }
                else
                {
                    _lastModifiedDateTime = value.Value.ToUniversalTime();
                }
            } 
        }
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
        public string Hash { get; private set; }
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
            get { return _parent; }
            set
            {
                if (value != null)
                {
                    if (value is File)
                    {
                        throw new ArgumentException("File can't be parent of the file!");
                    }
                    _parent = value;
                }
                else
                {
                    throw new ArgumentException("Parent must be set for file!");
                }
            }
        }
        private const string _startOfBlockListDeclaration = "(list<block>[";
        private const string _endOfBlockListDeclaration = "},";
        /// <summary>
        ///   Creates and set hash for file instance.
        /// </summary>
        public void SetHash()
        {
            SetHash(null);
        }
        /// <summary>
        ///   Creates and set hash for file instance.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of file if needed in byte array.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        /// <exception cref="Exception"/>
        public void SetHash(byte[] content)
        {
            if (Hash is null)
            {
                KeccakManaged keccak = new KeccakManaged(512);
                if (this.IsSingleBlock)
                {
                    if (content == null)
                    {
                        throw new ArgumentException("This file is single block and must be provided with byte array of content", "content");
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
                    List<string> blockHashes = new List<string>();
                    foreach (var b in Blocks)
                    {
                        blockHashes.Add(b.Hash);
                    }
                    List<byte> buffer = new List<byte>();
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
                throw new AccessViolationException("Hash can only be set once");
            }
        }
        /// <summary>
        ///   Creates and set hash for file instance, and returns it as string.
        /// </summary>
        public string GetHash()
        {
            return GetHash(null);
        }
        /// <summary>
        ///   Creates and set hash for file instance, and returns it as string.
        /// </summary>
        /// <param name="content">
        ///   Raw bytes of file if needed in byte array.
        ///   Only needed for <see cref="File.IsSingleBlock">Single Blocked files</see>.
        /// </param>
        /// <remarks>
        ///   It would be better if hash was created during the upload,
        ///   but it impossible, due to nature of this library and purposes.
        ///   So, the best approach is to upload a file to IPFS, retrieve it and set content manually.
        /// </remarks>
        public string GetHash(byte[] content)
        {
            if (Hash is null)
            {
                SetHash(content);
            }
            return Hash;
        }
        /// <summary>
        ///   Serializes passed file to string using passed encoding.
        /// </summary>
        /// <param name="file">
        ///   File to be serialized.
        /// </param>
        /// <param name="encoding">
        ///   Encoding used for serialization of file name.
        ///   Usually passed from parent <see cref="Hypermedia">hypermedia</see> where file resides.
        /// </param>
        public static string SerializeToString(File file, Encoding encoding)
        {
            return SerializeToString(file, encoding, Formatting.None, 0);
        }
        /// <summary>
        ///   Serializes passed file to string using passed encoding.
        /// </summary>
        /// <param name="file">
        ///   File to be serialized.
        /// </param>
        /// <param name="encoding">
        ///   Encoding used for serialization of file name.
        ///   Usually passed from parent <see cref="Hypermedia">hypermedia</see> where file resides.
        /// </param>
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <param name="tabulationCount">
        ///   Internal argument for count of tabulations.
        /// </param>
        public static string SerializeToString(File file, Encoding encoding, Formatting formatting, uint tabulationsCount)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                SerializationTools.InitTabulations(out outerTabulationBuilder, out innerTabulationBuilder, tabulationsCount);
            }
            StringBuilder builder = new StringBuilder();
            SerializationTools.InitStartBaseSystemEntitySerializationStrings(ref builder, file, encoding, outerTabulationBuilder, innerTabulationBuilder);
            builder.AppendLine($"{innerTabulationBuilder}(string:extension)={file.Extension},");
            builder.AppendLine($"{innerTabulationBuilder}(file_attributes_null:attributes)={FileAttributesSerializer(file.Attributes)},");
            builder.AppendLine($"{innerTabulationBuilder}(date_time_null:last_modified_date_time)={(file.LastModifiedDateTime is null?"null":((DateTimeOffset)file.LastModifiedDateTime.Value).ToUnixTimeSeconds().ToString())},");
            builder.AppendLine($"{innerTabulationBuilder}{_startOfBlockListDeclaration}{file.Blocks.Count}]:blocks)="+"{"+(file.IsSingleBlock ? "empty;" : (formatting == Formatting.Indented ? BlocksListSerializer(file.Blocks, formatting, tabulationsCount + 1) : BlocksListSerializer(file.Blocks)))+$"{_endOfBlockListDeclaration}");
            builder.AppendLine($"{innerTabulationBuilder}(boolean:is_single_block)={(file.IsSingleBlock ? "true" : "false")},");
            SerializationTools.InitEndBaseSerializationStrings(ref builder, file, outerTabulationBuilder, innerTabulationBuilder);
            return builder.ToString();
        }
        #region Serialization Algorithms
        private static string FileAttributesSerializer(FileAttributes? attributes)
        {
            if (attributes is null)
            {
                return "null";
            }
            switch (attributes.Value)
            {
                case FileAttributes.Hidden:
                    return "hidden";
                case FileAttributes.ReadOnly:
                    return "read-only";
                case FileAttributes.Hidden | FileAttributes.ReadOnly:
                    return "hidden|read-only";
                case FileAttributes.Normal:
                default:
                    return "normal";
            }
        }
        private static string BlocksListSerializer(List<Block> blocks)
        {
            return BlocksListSerializer(blocks, Formatting.None, 0);
        }
        private static string BlocksListSerializer(List<Block> blocks, Formatting formatting, uint tabulationsCount)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if (formatting == Formatting.Indented)
            {
                SerializationTools.InitTabulations(out outerTabulationBuilder, out innerTabulationBuilder, tabulationsCount);
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            for (int i = 0; i < blocks.Count; ++i)
            {
                builder.AppendLine($"{innerTabulationBuilder}(block:{i})={(formatting == Formatting.Indented ? Block.SerializeToString(blocks[i], formatting, tabulationsCount + 1) : Block.SerializeToString(blocks[i]))}{(i == blocks.Count-1 ? ";" : ",")}");
            }
            builder.Append($"{outerTabulationBuilder}]");
            return builder.ToString();
        }
        #endregion Serialization Algorithms
        /// <summary>
        ///   Deserializes passed string to file.
        /// </summary>
        /// <param name="input">
        ///   String to be deserialized.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="IEntity">entity</see> for file.
        /// </param>
        /// <param name="encoding">
        ///   Encoding used for deserialization of file name.
        ///   Usually passed from parent <see cref="Hypermedia">hypermedia</see> where file resides during deserialization of parent hypermedia.
        /// </param>
        public static File DeserializeFromString(string input, IEntity parent, Encoding encoding)
        {
            string path = null;
            string name = null;
            string extension = null;
            FileAttributes? attributes = null;
            long lastModDateTime = 0;
            DateTime? lastModifiedDateTime = null;
            List<Block> blocks = new List<Block>();
            bool isSingleBlock = false;
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            DeserializationTools.CheckStringFormat(input, false);

            string blockList;
            int count;
            List<string> stringList;
            DeserializationTools.SplitStringForSystemEntity(input, _startOfBlockListDeclaration, _endOfBlockListDeclaration, 13, out count, out blockList, out stringList, true);

            DeserializationTools.ParseStartBaseSystemEntitySerializationString(stringList, encoding, out path, out name);
            extension = new string(stringList[2].Skip(19).TakeWhile(x => x != ',').ToArray());
            attributes = FileAttributesDeserializer(new string(stringList[3].Skip(34).TakeWhile(x => x != ',').ToArray()));
            lastModDateTime = long.Parse(new string(stringList[4].Skip(41).TakeWhile(x => x != ',').ToArray()));
            lastModifiedDateTime = DateTimeOffset.FromUnixTimeSeconds(lastModDateTime).UtcDateTime;
            isSingleBlock = (new string(stringList[5].Skip(26).TakeWhile(x => x != ',').ToArray()) == "true") ? true : false;
            DeserializationTools.ParseEndBaseSerializationString(stringList, 6, out size, out parent_path, out hash);

            DeserializationTools.CheckParent(parent, parent_path, false);

            File file = new File
            {
                Path = path,
                Name = name,
                Extension = extension,
                Attributes = attributes,
                LastModifiedDateTime = lastModifiedDateTime,
                IsSingleBlock = isSingleBlock,
                Size = size,
                Parent = parent,
                Hash = hash
            };
            if (count != 0)
            {
                blocks = BlocksListDeserializer(blockList, file, count);
            }

            file.Blocks = blocks;
            return file;
        }
        #region Deserialization Algorithms
        private static FileAttributes? FileAttributesDeserializer(string input)
        {
            if (input == "null")
            {
                return null;
            }
            switch (input)
            {
                case "directory":
                    return FileAttributes.Directory;
                case "hidden":
                    return FileAttributes.Hidden;
                case "read-only":
                    return FileAttributes.ReadOnly;
                case "directory|hidden":
                    return FileAttributes.Directory | FileAttributes.Hidden;
                case "directory|read-only":
                    return FileAttributes.Directory | FileAttributes.ReadOnly;
                case "directory|hidden|read-only":
                    return FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.ReadOnly;
                case "hidden|read-only":
                    return FileAttributes.Hidden | FileAttributes.ReadOnly;
                case "normal":
                default:
                    return FileAttributes.Normal;
            }
        }

        private static List<Block> BlocksListDeserializer(string input, File parent, int count)
        {
            List<Block> blocks = new List<Block>();

            DeserializationTools.CheckStringFormat(input, false);

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            var stringList = input.Split(new string[] { "],", "];" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (stringList.Count != count)
            {
                throw new ArgumentException("Parsed string list does not match expected collection length", "count");
            }
            for(int i = 0; i < stringList.Count; ++i)
            {
                stringList[i] = stringList[i].TrimStart('\r').TrimStart('\n');
                StringBuilder builder = new StringBuilder(stringList[i]);
                builder.Append(']');
                stringList[i] = builder.ToString();
            }
            for(int i = 0; i < count; ++i)
            {
                int index = int.Parse(new string(stringList[i].Skip(7).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                {
                    throw new ArgumentException("Possible serialization error encountered. Unexpected sequence", "input");
                }
                blocks.Add(Block.DeserializeFromString(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent));
            }

            if (count != blocks.Count)
            {
                throw new ArgumentException("Serialized and deserialized collection length does not match", "count");
            }

            return blocks;
        }
        #endregion Deserialization Algorithms
        #region Validation
        public static bool IsSerializedStringValid(string input, IEntity parent)
        {
            long lastModDateTime = 0;
            ulong size = 0;

            if (!DeserializationTools.CheckStringFormat(input, true))
            {
                return false;
            }

            int count;
            string blockList;
            List<string> stringList;
            
            if(!DeserializationTools.SplitStringForSystemEntity(input, _startOfBlockListDeclaration, _endOfBlockListDeclaration, 13, out count, out blockList, out stringList, true))
            {
                return false;
            }

            if (stringList.Count != 9)
            {
                return false;
            }

            if (!DeserializationTools.ValidateStartOfStrings(stringList))
            {
                return false;
            }

            if (!DeserializationTools.ValidateEndOfStrings(stringList, 8))
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

            if ((new string(stringList[2].Skip(8).TakeWhile(x => x != ')').ToArray())) != "extension")
            {
                return false;
            }
            //3
            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "file_attributes_null")
            {
                return false;
            }

            if ((new string(stringList[3].Skip(22).TakeWhile(x => x != ')').ToArray())) != "attributes")
            {
                return false;
            }
            //4
            if ((new string(stringList[4].Skip(1).TakeWhile(x => x != ':').ToArray())) != "date_time_null")
            {
                return false;
            }

            if ((new string(stringList[4].Skip(16).TakeWhile(x => x != ')').ToArray())) != "last_modified_date_time")
            {
                return false;
            }
            //5
            if ((new string(stringList[5].Skip(1).TakeWhile(x => x != ':').ToArray())) != "boolean")
            {
                return false;
            }

            if ((new string(stringList[5].Skip(9).TakeWhile(x => x != ')').ToArray())) != "is_single_block")
            {
                return false;
            }
            //6
            if ((new string(stringList[6].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
            {
                return false;
            }

            if ((new string(stringList[6].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
            {
                return false;
            }
            //7
            if ((new string(stringList[7].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[7].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
            {
                return false;
            }
            //8
            if ((new string(stringList[8].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[8].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
            {
                return false;
            }
            //TryParse
            if (!long.TryParse(new string(stringList[4].Skip(41).TakeWhile(x => x != ',').ToArray()), out lastModDateTime))
            {
                return false;
            }

            if (!(new string(stringList[5].Skip(26).TakeWhile(x => x != ',').ToArray()) == "true" || new string(stringList[5].Skip(26).TakeWhile(x => x != ',').ToArray()) == "false"))
            {
                return false;
            }

            if (!ulong.TryParse(new string(stringList[6].Skip(14).TakeWhile(x => x != ',').ToArray()), out size))
            {
                return false;
            }

            string path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());

            string parent_path = new string(stringList[7].Skip(21).TakeWhile(x => x != ',').ToArray());
            if (!DeserializationTools.CheckParent(parent, parent_path, true))
            {
                return false;
            }

            File file = new File
            {
                Path = path
            };
            bool isBlocksValid = true;
            if (count > 0)
            {
                isBlocksValid = TryBlocksListDeserializer(blockList, file, count);
            }

            if (!isBlocksValid)
            {
                return false;
            }

            return true;
        }

        private static bool TryBlocksListDeserializer(string input, File parent, int count)
        {
            if (!DeserializationTools.CheckStringFormat(input, true))
            {
                return false;
            }

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n').TrimEnd('\r');
            var stringList = input.Split(new string[] { "],", "];" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (stringList.Count != count)
            {
                return false;
            }
            for (int i = 0; i < stringList.Count; ++i)
            {
                stringList[i] = stringList[i].TrimStart('\r').TrimStart('\n');
                StringBuilder builder = new StringBuilder(stringList[i]);
                builder.Append(']');
                stringList[i] = builder.ToString();
            }
            bool result = true;
            for (int i = 0; i < count; ++i)
            {
                int index = int.Parse(new string(stringList[i].Skip(7).TakeWhile(s => s != ')').ToArray()));
                if (index != i)
                {
                    return false;
                }
                if (!Block.IsSerializedStringValid(new string(stringList[i].SkipWhile(s => s != '[').ToArray()), parent))
                {
                    result = false;
                }
            }

            return result;
        }
        #endregion Validation
    }
}
