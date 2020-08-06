using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ipfs.Hypermedia.Tools
{
    internal static class DeserializationTools
    {
        public static bool CheckStringFormat(string input, bool isValidationMode)
        {
            if (!input.StartsWith("["))
            {
                if (!isValidationMode)
                {
                    throw new ArgumentException("Bad formatting in serialized string detected. Expected [ in start.", "input");
                }
                else
                {
                    return false;
                }
            }
            if (!input.EndsWith("]"))
            {
                if (!isValidationMode)
                {
                    throw new ArgumentException("Bad formatting in serialized string detected. Expected ] in end.", "input");
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static string PrepareString(string input)
        {
            return input.TrimStart('[').TrimEnd(']').TrimStart('\r').TrimEnd('\n').TrimStart('\n');
        }

        public static List<string> SplitStringForBlock(string input)
        {
            input = PrepareString(input);
            return input.Split('\n').ToList();
        }

        public static bool SplitStringForSystemEntity(string input, string startString, string endString, int skipCount, out int count, out string entitesList, out List<string> stringList, bool isFile)
        {
            input = PrepareString(input);
            entitesList = null;
            stringList = null;
            string output = null;
            if (!EntitiesListCount(input, startString, skipCount, out count))
            {
                return false;
            }
            entitesList = ExtractEntitiesList(input, startString, endString, skipCount, isFile);
            output = RemoveEntitiesList(input, startString, endString, isFile);

            stringList = output.Split('\n').ToList();
            return true;
        }

        public static bool SplitStringForHypermedia(string input, string startString, string endString, int skipCount, out int count, out string entitesList, out List<string> stringList, bool isValidationMode)
        {
            input = PrepareString(input);
            entitesList = null;
            stringList = null;
            string output = null;
            if (!EntitiesListCount(input, startString, skipCount, out count))
            {
                return false;
            }
            if (count <= 0)
            {
                if (!isValidationMode)
                {
                    throw new ArgumentException("Possible serialization error encountered. Hypermedia entities list can not be empty", "input");
                }
                else
                {
                    return false;
                }
            }
            entitesList = ExtractEntitiesList(input, startString, endString, skipCount, false);
            output = RemoveEntitiesList(input, startString, endString, false);

            stringList = output.Split('\n').ToList();
            return true;
        }

        public static void ParseEndBaseSerializationString(List<string> stringList, int start_index, out ulong size, out string parent_path, out string hash)
        {
            size = ulong.Parse(new string(stringList[start_index].Skip(14).TakeWhile(x => x != ',').ToArray()));
            parent_path = new string(stringList[start_index+1].Skip(21).TakeWhile(x => x != ',').ToArray());
            hash = new string(stringList[start_index+2].Skip(14).TakeWhile(x => x != ';').ToArray());
        }

        public static void ParseStartBaseSystemEntitySerializationString(List<string> stringList, Encoding encoding, out string path, out string name)
        {
            path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());
            name = EncodingTools.DecodeString(new string(stringList[1].Skip(14).TakeWhile(x => x != ',').ToArray()), encoding);
        }

        public static void ParseStartBaseHypermediaSerializationString(List<string> stringList, out string path, out Encoding encoding, out string name)
        {
            path = new string(stringList[0].Skip(14).TakeWhile(x => x != ',').ToArray());
            try
            {
                encoding = Encoding.GetEncoding(new string(stringList[3].Skip(20).TakeWhile(x => x != ',').ToArray()));
            }
            catch
            {
                encoding = Encoding.GetEncoding("utf-8");
            }
            name = EncodingTools.DecodeString(new string(stringList[1].Skip(14).TakeWhile(x => x != ',').ToArray()), encoding);
        }

        public static bool EntitiesListCount(string input, string startString, int skipCount, out int count)
        {
            count = -1;
            if (!input.Contains(startString))
            {
                return false;
            }
            int start_block_index = input.IndexOf(startString);
            if (!input.Contains("},"))
                return false;

            if (!int.TryParse(new string(input.Skip(start_block_index + skipCount).TakeWhile(s => s != ']').ToArray()), out count))
            {
                return false;
            }
            return true;
        }

        private static string ExtractEntitiesList(string input, string startString, string endString, int skipCount, bool isFile)
        {
            string extracted = null;

            int start_block_index = input.IndexOf(startString);
            int end_block_index = (isFile ? input.IndexOf(endString) : input.LastIndexOf(endString));

            int count = int.Parse(new string(input.Skip(start_block_index + skipCount).TakeWhile(s => s != ']').ToArray()));
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

        private static string RemoveEntitiesList(string input, string startString, string endString, bool isFile)
        {
            string redacted = null;

            int start_block_index = input.IndexOf(startString);
            int end_block_index = (isFile ? input.IndexOf(endString) : input.LastIndexOf(endString));
            redacted = input.Remove(start_block_index - 1,
                    (end_block_index + 3) - start_block_index - 1);
            return redacted;
        }

        public static bool CheckParent(IEntity parent, string parent_path, bool isValidationMode)
        {
            if (parent.Path != parent_path)
            {
                if (!isValidationMode)
                {
                    throw new ArgumentException("Deserialized parent path is not the expected one", "parent");
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidateStartOfStrings(List<string> stringList)
        {
            foreach (var s in stringList)
            {
                if (!s.StartsWith("("))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidateEndOfStrings(List<string> stringList, int borderIndex)
        {
            for (int i = 0; i < borderIndex; ++i)
            {
                if (!stringList[i].EndsWith(",\r"))
                {
                    return false;
                }
            }
            if (!stringList[borderIndex].EndsWith(";\r"))
            {
                return false;
            }
            return true;
        }
    }
}
