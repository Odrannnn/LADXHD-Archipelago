using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace LADXHD_Launcher
{
    internal static class Extensions
    {
        public static bool TestPath(this string InputPath, bool IsDirectory = false)
        {
            // If the value is null or empty then return false.
            if (string.IsNullOrWhiteSpace(InputPath))
                return false;

            // Test the path for file or directory.
            try
            {
                // Attempt to pull attributes from the file/folder.
                var attributes = File.GetAttributes(InputPath);
                bool isDir = (attributes & FileAttributes.Directory) != 0;

                // If parameter is set only return true if it's a directory.
                if (IsDirectory)
                    return isDir;

                // If it's a file or directory without the paramemter return true.
                return true;
            }
            // Catch all known exception types.
            catch (Exception x) 
            {
                // Catches types where directory does not eixst.
                if (x is DirectoryNotFoundException || 
                    x is FileNotFoundException || 
                    x is ArgumentException || 
                    x is NotSupportedException)
                    return false;

                // Directory exists but is inaccessible.
                else if (x is UnauthorizedAccessException)
                    return true;

                // Exception is unknown.
                return false;
            }
        }

        public static string CreatePath(this string InputPath, bool NoReturn = false)
        {
            // If the path is empty then it does not exist.
            if (InputPath == "" || InputPath == null)
                return "";

            // Check to see if the path does not exist.
            if (!InputPath.TestPath(false))
                Directory.CreateDirectory(InputPath);

            // Return the path that was created unless NoReturn flag is set.
            if (!NoReturn)
                return InputPath;
            return "";
        }

        public static void RemovePath(this string inputPath)
        {
            // If the path is empty then it does not exist.
            if (string.IsNullOrEmpty(inputPath))
                return;

            // Don't even enter the loop if nothing is there.
            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
                return;

            // Set up a loop for retries if a file is still locked when trying to delete it.
            for (int i = 0; i < 10; i++)
            {
                // Attempt to delete the file or folder.
                try
                {
                    // Get whether it's a file or a folder and run the proper delete command.
                    var attributes = File.GetAttributes(inputPath);

                    // If it's a directory (folder).
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        // Clear "read-only" on directories recursively.
                        foreach (var file in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                            File.SetAttributes(file, FileAttributes.Normal);
                        foreach (var dir in Directory.GetDirectories(inputPath, "*", SearchOption.AllDirectories))
                            File.SetAttributes(dir, FileAttributes.Normal);
                       
                        // Clear potential "read-only" and delete the directory.
                        File.SetAttributes(inputPath, FileAttributes.Normal);
                        Directory.Delete(inputPath, true);
                    }
                    // If it's simply a file.
                    else
                    {
                        // Clear potential "read-only" and delete the file.
                        File.SetAttributes(inputPath, FileAttributes.Normal);
                        File.Delete(inputPath);
                    }
                    return;
                }
                // Catch exceptions and try to delete again.
                catch (Exception x) 
                {
                    // Catch types where directory does not exist.
                    if (x is DirectoryNotFoundException || x is FileNotFoundException)
                        return;

                    // Catch types were a delete retry takes place.
                    else if (x is IOException || x is UnauthorizedAccessException)
                        Thread.Sleep(200);
                }
            }
        }

        public static void MovePath(this string Source, string Destination, bool Overwrite = false)
        {
            // If the values are null or empty then return false.
            if (string.IsNullOrWhiteSpace(Source) || string.IsNullOrWhiteSpace(Destination))
                return;

            // Bail early if source doesn't exist.
            if (!File.Exists(Source) && !Directory.Exists(Source))
                return;

            // Get whether it's a file or a folder and run the proper rename command.
            var attributes = File.GetAttributes(Source);

            // If it's a directory (folder).
            if ((attributes & FileAttributes.Directory) != 0)
            {
                // If the destination exists and we want to overwrite the contents.
                if (Directory.Exists(Destination))
                {
                    if (!Overwrite) return;
                    Destination.RemovePath();
                }
                // Move the new name to the destination.
                Directory.Move(Source, Destination);
            }
            // Move the file to the new destination.
            else
            {
                // If the destination exists and we want to overwrite the contents.
                if (File.Exists(Destination))
                {
                    if (!Overwrite) return;
                    Destination.RemovePath();
                }
                File.Move(Source, Destination);
            }
        }

        public static void RenamePath(this string Source, string Destination, bool Overwrite = false)
        {
            // Anything I write here would be identical to move so just call that.
            Source.MovePath(Destination, Overwrite);
        }

        public static void CopyPath(this string SourcePath, string DestinationPath, bool Overwrite)
        {
            // If the path is empty then it does not exist.
            if (SourcePath == null || SourcePath == "")
                return;

            // The path exists so let's try to copy it.
            if (SourcePath.TestPath())
            {
                // The destination already exists so either remove it or exit.
                if (DestinationPath.TestPath())
                    if (Overwrite)
                        DestinationPath.RemovePath();
                    else
                        return;

                // If a folder, copy the folder, subfolders, and files to the new destination.
                if (File.GetAttributes(SourcePath) == FileAttributes.Directory)
                {
                    foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                        Directory.CreateDirectory(Path.Combine(DestinationPath, Path.GetRelativePath(SourcePath, dirPath)));
                    foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
                        File.Copy(newPath, Path.Combine(DestinationPath, Path.GetRelativePath(SourcePath, newPath)), true);
                }
                // Copying a file is a much simpler process.
                else
                    File.Copy(SourcePath, DestinationPath);
            }
        }

        public static bool IsPathEmpty(this string SourcePath)
        {
            if (File.GetAttributes(SourcePath) == FileAttributes.Directory)
            {
                // If it doesn't exist then treat it as "empty".
                if (!SourcePath.TestPath())
                    return true;

                return !Directory.EnumerateFileSystemEntries(SourcePath).Any();
            }
            // If it's a file then just return false since the file exists.
            return false;
        }

        public static List<string> GetFiles(this string Path, string SearchPatterns = "*.*", bool Recurse = false)
        {
            // Split the search patterns using the commas into a list.
            string[] findPatterns = SearchPatterns.Split(',');

            // Search all sub-folders if recurse is enabled.
            SearchOption searchOption = SearchOption.TopDirectoryOnly;
            if (Recurse) searchOption = SearchOption.AllDirectories;

            // Grab the files within the folder (and maybe sub-folders).
            return (findPatterns.AsParallel()
                .SelectMany(searchPattern => Directory.EnumerateFiles(Path, searchPattern, searchOption)
                .Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System))))
                .EnumToList();
        }

        public static List<string> GetFolders(this string Path, string SearchPattern = "*", bool Recurse = false)
        {
            // Search all sub-folders if recurse is enabled.
            SearchOption SearchOption = SearchOption.TopDirectoryOnly;
            if (Recurse) SearchOption = SearchOption.AllDirectories;

            // Get the folder and potentially all subfolders.
            return (Directory.GetDirectories(Path, SearchPattern, SearchOption)).EnumToList();
        }

        public static int BoolToInt(this bool Boolean)
        {
            return Boolean ? 1 : 0;
        }

        public static bool IntToBool(this int Int)
        {
            return Convert.ToBoolean(Int);
        }

        public static void ClearPath(this string InputPath)
        {
            // If the path is empty then it does not exist.
            if (InputPath == "" || InputPath == null)
                return;

            // If the path exists remove all files and folders but do not remove the InputPath.
            if (InputPath.TestPath())
            {
                foreach (string LoopFile in InputPath.GetFiles(Recurse: true))
                    LoopFile.RemovePath();
                foreach (string LoopFile in InputPath.GetFolders(Recurse: true))
                    LoopFile.RemovePath();
            }
        }

        public static string Extend(this string InputString, int Length)
        {
            // Check the number of characters against the desired amount.
            if (InputString.Length < Length)
            {
                // Loop until the desired number of characters is added.
                int AddLength = Length - InputString.Length;
                for (int i = 1; i <= AddLength; i++)
                    InputString += " ";
            }
            // Return the modified string.
            return InputString;
        }

        public static string[] StrSplit(this string InputString, string SplitOn)
        {
            // Splits a string by the input string.
            return InputString.Split(new string[] { SplitOn }, StringSplitOptions.None);
        }

        public static string FormatNewLines(this string InputString)
        {
            // Split the string into a character array to pick out new lines.
            char[] CharArray = InputString.ToCharArray();
            string NewString = "";

            // Replace newline characters with ones that can use "Format".
            for (int i = 0; i < CharArray.Length; i++)
            {
                if (CharArray[i] == '\n')
                    NewString += "{0}";
                else
                    NewString += CharArray[i].ToString();
            }
            // Return the string where all '\n' were replaced with '{0}'.
            return NewString;
        }

        public static string RemoveIllegalCharacters(this string Value)
        {
            // Create an array that contains all illegal characters.
            string[] IllegalArray = { "<", ">", ":", "\"", "'", "/", "\\", "|", "?", "*" };

            // Loop through the array of illegal characters
            foreach (string IllegalChar in IllegalArray)
            {
                // Check the value against the current character and replace if exists.
                Value = Value.Replace(IllegalChar,"");
            }
            // Return the modified string.
            return Value;
        }

        public static string CalculateHash(this string filePath, string hashType)
        {
            // If the file doesn't exist then exit early.
            if (!filePath.TestPath()) return "";

            // Determine the algorithm and cast to the base class.
            using HashAlgorithm? algorithm = hashType.ToUpper() switch
            {
                "MD5"    => MD5.Create(),
                "SHA256" => SHA256.Create(),
                "SHA512" => SHA512.Create(),
                _        => null
            };
            // If it wasn't specified then return.
            if (algorithm == null) return "";

            // Use a Stream to avoid loading the whole file into RAM.
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = algorithm.ComputeHash(stream);

            // Convert to hex string.
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        public static List<string> EnumToList(this IEnumerable<string> EnumArray)
        {
            // Convert "IEnumerable" to "List<string>".
            List<string> StringList = new List<string> { };
            foreach (string Item in EnumArray)
                StringList.Add(Item);
            return StringList;
        } 

        public static List<string> ArrayToList(this string[] StringArray)
        {
            // Convert "string[]" to "List<string>".
            List<string> StringList = new List<string> { };
            for (int i = 0; i < StringArray.Length; i++)
                StringList.Add(StringArray[i]);
            return StringList;
        }

        public static string[] ListToArray(this List<string> StringList)
        {
            // Convert "List<string>" to "string[]".
            string[] StringArray = new string[StringList.Count];
            for (int i = 0; i < StringList.Count; i++)
                StringArray[i] = StringList[i];
            return StringArray;
        }

        public static List<string> ReadLinesToList(this string TextFile)
        {
            // Get a text file as "List<string>" instead of "string[]".
            return File.ReadAllLines(TextFile).ArrayToList();
        }

        public static void Move<T>(this List<T> GenericList, int OldIndex, int NewIndex)
        {
            // Move the position of an item in a generic "List<T>".
            T ListItem = GenericList[OldIndex];
            GenericList.RemoveAt(OldIndex);
            GenericList.Insert(NewIndex, ListItem);
        }

        public static T[] Reverse<T>(this T[] OldArray)
        {
            // Create a new array to hold the reversed order.
            T[] NewArray = new T[OldArray.Length];

            // Loop through the old array in reverse and build the new array ascending.
            int Index = 0;
            for (int i = OldArray.Length - 1; i >= 0; i--)
            {
                NewArray[Index] = OldArray[i];
                Index++;
            }
            // Return the reversed array.
            return NewArray;
        }

        public static T[] RemoveAt<T>(this T[] OldArray, int Index)
        {
            // Create a new array with one less index.
            T[] NewArray = new T[OldArray.Length - 1];

            // If the index is beyond the first position copy the array up to that position.
            if (Index > 0)
                Array.Copy(OldArray, 0, NewArray, 0, Index);

            // When we reached the position of the index to remove copy everything beyond that index.
            if (Index < OldArray.Length - 1)
                Array.Copy(OldArray, Index + 1, NewArray, Index, OldArray.Length - Index - 1);

            // Return the new array with the data removed.
            return NewArray;
        }

        public static T[] Insert<T>(this T[] OldArray, int Index, dynamic Data)
        {
            // Create a new array with one more index.
            T[] NewArray = new T[OldArray.Length + 1];

            // If the index is beyond the first position copy the array up to that position.
            if (Index > 0)
                Array.Copy(OldArray, 0, NewArray, 0, Index);

            // Copy the data into the current position.
            NewArray[Index] = Data;

            // If the index falls within the upper bounds copy the rest of the data into the new array.
            if (Index < OldArray.Length - 1)
                Array.Copy(OldArray, Index, NewArray, Index + 1, OldArray.Length - Index);

            // Return the new array with the data added.
            return NewArray;
        }
    }
}