using System;
using System.Collections.Generic;
using System.Text;

using Ipfs.Hypermedia.Tools;

namespace Ipfs.Hypermedia.Serialization
{
    internal interface ISerializationVersion
    {
        string SerializeToString(Hypermedia hypermedia);
        string SerializeToString(Hypermedia hypermedia, Formatting formatting, uint tabulationsCount);
        Hypermedia DeserializeFromString(string input);
        Hypermedia DeserializeFromString(string input, Hypermedia parent);
        bool IsSerializedStringValid(string input, Hypermedia parent = null);
    }
}
