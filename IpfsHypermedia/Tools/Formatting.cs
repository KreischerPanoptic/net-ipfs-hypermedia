using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia.Tools
{
    /// <summary>
    ///   Enum for formatting mode of serialized string.
    /// </summary>
    public enum Formatting
    {
        /// <summary>
        ///   Serialized string will not contain tabulations.
        /// </summary>
        None = 0,
        /// <summary>
        ///   Serialized string will contain tabulations.
        /// </summary>
        Indented = 1
    }
}
