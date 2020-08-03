using Ipfs.Hypermedia.Cryptography;

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
        ///   Key of block in IPFS distributed file system.
        /// </summary>
        public string Key { get; set; }
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
                    throw new ArgumentException("Parent must be set for block!");
                else
                    _parent = value;
            } 
        }
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
        /// <summary>
        ///   Serializes passed block to string.
        /// </summary>
        /// <param name="block">
        ///   Block to be serialized.
        /// </param>
        public static string SerializeToString(Block block)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            builder.AppendLine($"(string:key)={block.Key},");
            builder.AppendLine($"(uint64:size)={block.Size},");
            builder.AppendLine($"(string:parent_path)={block.Parent.Path},");
            builder.AppendLine($"(string:hash)={block.Hash};");
            builder.Append("]");
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
            string key = null;
            ulong size = 0;
            string parent_path = null;
            string hash = null;

            if (!input.StartsWith("["))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.");
            if(!input.EndsWith("]"))
                throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.");

            input = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');
            var stringList = input.Split('\n').ToList();

            key = new string(stringList[0].Skip(13).TakeWhile(x => x != ',').ToArray());
            size = ulong.Parse(new string(stringList[1].Skip(14).TakeWhile(x => x != ',').ToArray()));
            parent_path = new string(stringList[2].Skip(21).TakeWhile(x => x != ',').ToArray());
            hash = new string(stringList[3].Skip(14).TakeWhile(x => x != ';').ToArray());

            if (parent.Path != parent_path)
                throw new ArgumentException("Deserialized parent path is not the expected one");

            return new Block() { Key = key, Size = size, Parent = parent, Hash = hash };
        }
        #region Validation
        public static bool IsSerializedStringValid(string input, File parent)
        {
            if (!input.StartsWith("["))
                return false;
            if (!input.EndsWith("]"))
                return false;

            string tmp = input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');
            var stringList = tmp.Split('\n').ToList();
            if (stringList.Count != 4)
                return false;

            foreach (var s in stringList)
            {
                if (!s.StartsWith("("))
                    return false;
            }

            for(int i = 0; i < 3; ++i)
            {
                if (!stringList[i].EndsWith(",\r"))
                    return false;
            }
            if (!stringList[3].EndsWith(";\r"))
                return false;

            if ((new string(stringList[0].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[0].Skip(8).TakeWhile(x => x != ')').ToArray())) != "key")
                return false;

            if ((new string(stringList[1].Skip(1).TakeWhile(x => x != ':').ToArray())) != "uint64")
                return false;

            if ((new string(stringList[1].Skip(8).TakeWhile(x => x != ')').ToArray())) != "size")
                return false;

            if ((new string(stringList[2].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[2].Skip(8).TakeWhile(x => x != ')').ToArray())) != "parent_path")
                return false;

            if ((new string(stringList[3].Skip(1).TakeWhile(x => x != ':').ToArray())) != "string")
                return false;

            if ((new string(stringList[3].Skip(8).TakeWhile(x => x != ')').ToArray())) != "hash")
                return false;

            string parent_path = new string(stringList[2].Skip(21).TakeWhile(x => x != ',').ToArray());

            if (parent.Path != parent_path)
                return false;

            return true;
        }
        #endregion Validation
    }
}
