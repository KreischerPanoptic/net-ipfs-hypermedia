using Ipfs.Hypermedia.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia.Versions.ver010
{
    public sealed class Hypermedia010 : Hypermedia
    {
        /// <summary>
        ///   Information about version of hypermedia metadata.
        /// </summary>
        /// <remarks>
        ///   It used in clients for choosing correct algorithms of parsing a hypermedia.
        ///   Current default - hypermedia/0.1.0
        /// </remarks>
        public override string Version { get; internal set; } = "hypermedia/0.1.0";
        public override string GetHash()
        {
            return GetHash(null);
        }

        public override string GetHash(byte[] content)
        {
            if (Hash is null)
            {
                SetHash();
            }
            return Hash;
        }

        public override void SetHash()
        {
            SetHash(null);
        }

        public override void SetHash(byte[] content)
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
                throw new AccessViolationException("Hash can only be set once");
            }
        }

        public override void SetTopic()
        {
            if (Hash is null)
            {
                throw new FieldAccessException("Hash must be created for hypermedia before topic address creation");
            }
            if (!(Topic is null))
            {
                throw new AccessViolationException("Topic can only be set once");
            }
            Topic = $"{Path}_{Hash}";
        }
    }
}
