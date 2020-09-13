using Ipfs.Hypermedia.Cryptography;
using Ipfs.Hypermedia.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Permissions;
using System.Text;

namespace Ipfs.Hypermedia
{
    /// <summary>
    ///   Implements the IPFS block with necessary information for block retrieval
    ///   and correct processing.
    /// </summary>
    public sealed class Block
    {
        /// <summary>
        ///   Path for this block in IPFS distributed file system.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        ///   Size of block in bytes.
        /// </summary>
        public ulong Size { get; set; }
        private File _parent;
        /// <summary>
        ///   Link to parent <see cref="File">File<see/> from which this block is taken.
        /// </summary>
        /// <remarks>
        ///   Return type is File which can be confusing,
        ///   but it should be that way. The use of <see cref="ISystemEntity">ISystemEntity<see/> or <see cref="IEntity">IEntity<see/>
        ///   is unwelcome due to possible confusion with <see cref="Directory">Directory<see/> or <see cref="Hypermedia">Hypermedia<see/> types.
        /// </remarks>
        public File Parent 
        { 
            get { return _parent; }
            set
            {
                if (value is null)
                {
                    throw new ArgumentException("Parent must be set for block!");
                }
                else
                {
                    _parent = value;
                }
            } 
        }
        /// <summary>
        ///   Hash of block for verification purposes.
        /// </summary>
        public string Hash { get; private set; }
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
                throw new AccessViolationException("Hash can only be set once");
            }
        }
        /// <summary>
        ///   Serializes passed block to string.
        /// </summary>
        /// <param name="block">
        ///   Block to be serialized.
        /// </param>
        public static string SerializeToString(Block block)
        {
            return SerializeToString(block, Formatting.None, 0);
        }
        /// <summary>
        ///   Serializes passed block to string.
        /// </summary>
        /// <param name="block">
        ///   Block to be serialized.
        /// </param>
        /// <param name="formatting">
        ///   <see cref="Formatting">Formatting</see> options for serialization.
        /// </param>
        /// <param name="tabulationCount">
        ///   Internal argument for count of tabulations.
        /// </param>
        public static string SerializeToString(Block block, Formatting formatting, uint tabulationsCount)
        {
            string outerTabulationBuilder = string.Empty;
            string innerTabulationBuilder = string.Empty;
            if(formatting == Formatting.Indented)
            {
                SerializationTools.InitTabulations(out outerTabulationBuilder, out innerTabulationBuilder, tabulationsCount);
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            builder.AppendLine($"{innerTabulationBuilder}(string:path)={block.Path},");
            SerializationTools.InitEndBaseSerializationStrings(ref builder, block, outerTabulationBuilder, innerTabulationBuilder);
            return builder.ToString();
        }
        /// <summary>
        ///   Deserializes passed string to block.
        /// </summary>
        /// <param name="input">
        ///   String to be deserialized.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="File">file</see> for block.
        /// </param>
        public static Block DeserializeFromString(string input, File parent)
        {
            string path = null;
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            DeserializationTools.CheckStringFormat(input, false);

            var stringList = DeserializationTools.SplitStringForBlock(input);

            path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());
            DeserializationTools.ParseEndBaseSerializationString(stringList, 1, out size, out parent_path, out hash);

            DeserializationTools.CheckParent(parent, parent_path, false);

            return new Block { Path = path, Size = size, Parent = parent, Hash = hash };
        }
        #region Validation
        public static bool IsSerializedStringValid(string input, File parent)
        {
            if(!DeserializationTools.CheckStringFormat(input, true))
            {
                return false;
            }

            var stringList = DeserializationTools.SplitStringForBlock(input);
            if (stringList.Count != 4)
            {
                return false;
            }

            if (!DeserializationTools.ValidateStartOfStrings(stringList))
            {
                return false;
            }

            if (!DeserializationTools.ValidateEndOfStrings(stringList, 3))
            {
                return false;
            }

            if ((new string(stringList[0].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[0].Skip(8).TakeWhile(x => x != ')').ToArray())) != "path")
            {
                return false;
            }

            if ((new string(stringList[1].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
            {
                return false;
            }

            if ((new string(stringList[1].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
            {
                return false;
            }

            if ((new string(stringList[2].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[2].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
            {
                return false;
            }

            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
            {
                return false;
            }

            if ((new string(stringList[3].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
            {
                return false;
            }

            string parent_path = new string(stringList[2].Skip(21).TakeWhile(x => x != ',').ToArray());
            if (!DeserializationTools.CheckParent(parent, parent_path, true))
            {
                return false;
            }

            return true;
        }
        #endregion Validation
    }
}
