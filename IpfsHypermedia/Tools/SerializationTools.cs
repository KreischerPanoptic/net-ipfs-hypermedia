using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia.Tools
{
    internal static class SerializationTools
    {
        public static void InitTabulations(out string outerTabulationBuilder, out string innerTabulationBuilder, uint tabulationsCount)
        {
            outerTabulationBuilder = string.Empty;
            innerTabulationBuilder = "\t";
            for (int i = 0; i < tabulationsCount; ++i)
            {
                outerTabulationBuilder += '\t';
                innerTabulationBuilder += '\t';
            }
        }

        public static void InitEndBaseSerializationStrings(ref StringBuilder builder, Block block, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            builder.AppendLine($"{innerTabulationBuilder}(uint64:size)={block.Size},");
            builder.AppendLine($"{innerTabulationBuilder}(string:parent_path)={block.Parent.Path},");
            builder.AppendLine($"{innerTabulationBuilder}(string:hash)={block.Hash};");
            builder.Append($"{outerTabulationBuilder}]");
        }

        public static void InitEndBaseSerializationStrings(ref StringBuilder builder, IEntity entity, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            builder.AppendLine($"{innerTabulationBuilder}(uint64:size)={entity.Size},");
            builder.AppendLine($"{innerTabulationBuilder}(string:parent_path)={entity.Parent.Path},");
            builder.AppendLine($"{innerTabulationBuilder}(string:hash)={entity.Hash};");
            builder.Append($"{outerTabulationBuilder}]");
        }

        public static void InitEndBaseSerializationStrings(ref StringBuilder builder, Hypermedia hypermedia, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            builder.AppendLine($"{innerTabulationBuilder}(uint64:size)={hypermedia.Size},");
            builder.AppendLine($"{innerTabulationBuilder}(string:parent_path)={(hypermedia.Parent is null ? "null" : hypermedia.Parent.Path)},");
            builder.AppendLine($"{innerTabulationBuilder}(string:hash)={hypermedia.Hash};");
            builder.Append($"{outerTabulationBuilder}]");
        }
        public static void InitStartBaseSerializationStrings(ref StringBuilder builder, IEntity entity, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            builder.AppendLine("[");
            builder.AppendLine($"{innerTabulationBuilder}(string:path)={entity.Path},");
        }
        public static void InitStartBaseSystemEntitySerializationStrings(ref StringBuilder builder, IEntity entity, Encoding encoding, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            InitStartBaseSerializationStrings(ref builder, entity, outerTabulationBuilder, innerTabulationBuilder);
            builder.AppendLine($"{innerTabulationBuilder}(string:name)={EncodingTools.EncodeString(entity.Name, encoding)},");
        }

        public static void InitStartBaseHypermediaSerializationStrings(ref StringBuilder builder, Hypermedia hypermedia, string outerTabulationBuilder, string innerTabulationBuilder)
        {
            InitStartBaseSerializationStrings(ref builder, hypermedia, outerTabulationBuilder, innerTabulationBuilder);
            builder.AppendLine($"{innerTabulationBuilder}(string:name)={EncodingTools.EncodeString(hypermedia.Name, hypermedia.Encoding is null ? Encoding.UTF8 : hypermedia.Encoding)},");
        }
    }
}
