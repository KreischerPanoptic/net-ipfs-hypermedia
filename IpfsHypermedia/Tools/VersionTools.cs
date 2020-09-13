using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ipfs.Hypermedia.Serialization;
using Ipfs.Hypermedia.Serialization.Versions.ver010;

namespace Ipfs.Hypermedia.Tools
{
    internal static class SerializationVersionTools
    {
        public static List<ISerializationVersion> GetVersions()
        {
            return new List<ISerializationVersion>() { new HypermediaSerialization010() };
        }
        public static string GetVersion(string input)
        {
            DeserializationTools.CheckStringFormat(input, false);
            int index = input.LastIndexOf("(string:version)=");
            return new string(input.Skip(index + 17).TakeWhile(x => x != ',').ToArray());
        }

        public static ISerializationVersion GetSerializationVersion(string version)
        {
            if (version is null)
            {
                throw new ArgumentException("Version can not be null", nameof(version));
            }

            switch (version)
            {
                case "hypermedia/0.1.0":
                    return new HypermediaSerialization010();
                default:
                    throw new ArgumentException("Version unknown", nameof(version));
            }
        }
    }
}
