using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parsers
{
    #region Exceptions
    /// <summary>A Section is repeated in a file</summary>
    public class SectionExistsException : Exception
    {
        public string Section { get; private set; }
        public SectionExistsException(string section) : base() { this.Section = section; }
        public SectionExistsException(string message, string section) : base(message) { this.Section = section; }
    }
    /// <summary>A key is repeated in a section</summary>
    public class KeyExistsException : SectionExistsException
    {
        public string Key { get; private set; }
        public KeyExistsException(string section, string key) : base(section) { this.Key = key; }
        public KeyExistsException(string message, string section, string key) : base(message, section) { this.Key = key; }
    }
    public class FileHandlingException : Exception 
    { 
        public FileHandlingException(string message) : base(message){}
    }
    public class FileNotSuppliedException : Exception
    {
        public FileNotSuppliedException(string message) : base(message) { }
    }
    #endregion

    /// <summary>
    /// Used to parse .ini files and faster work on it's values.
    /// It is also possible to use the static methods for reading or writing to files.
    /// </summary>
    public class INIFile : IEquatable<INIFile>
    {
        #region Static API
        /// <summary>
        /// Check if the given INIFile object is null or not loaded yet (Created with an empty constructor
        /// without calling to <see cref="Load"/> Method.
        /// </summary>
        /// <param name="file">The INIFile object to check</param>
        /// <returns>true in case object is null or not loaded yet.</returns>
        public static bool IsNullOrEmpty(INIFile file)
        {
            return (file == null || file.sections == null);
        }
        /// <summary>
        /// Read An .ini file value of a key inside a given section
        /// </summary>
        /// <param name="filePath">The path to the .ini file</param>
        /// <param name="section">The desired section</param>
        /// <param name="key">The desired key inside the given section</param>
        /// <param name="defaultValue">A default value returned in case an error occurred. if not supplied a null is used.</param>
        /// <returns>The desired value or <paramref name="defaultValue"/></returns>
        ///<exception cref="System.ArgumentNullException">either section / key are null</exception>
        /// <exception cref="Parsers.FileHandlingException">Any exception thrown by file handling methods.</exception>
        public static string ReadValue(string filePath, string section, string key, string defaultValue = null)
        {
            string line;
            string currentSection = null;
            if (section == null || key == null) throw new ArgumentNullException("Section or key are null");
            key = key.Trim();
            section = section.Trim().TrimStart('[').TrimEnd(']');
            string[] keyValue;
            try
            {
                using (var file = new System.IO.StreamReader(filePath))
                {
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        line = line.TrimStart();
                        if (line.Length == 0) continue;
                        if (line.StartsWith(";")) continue;
                        if (line.StartsWith("[")) //section
                        {
                            line = line.TrimStart('[');
                            try { line = line.Remove(line.IndexOf(']')); }
                            catch { }
                            currentSection = line;
                            continue;
                        }
                        if (currentSection == null || currentSection != section) continue;
                        keyValue = line.Split(new char[] { '=' }, 2);
                        if (keyValue == null || keyValue.Length != 2) continue;
                        keyValue[0] = keyValue[0].Trim();
                        if (keyValue[1].Contains(';')) keyValue[1] = keyValue[1].Remove(keyValue[1].IndexOf(';'));
                        keyValue[1] = keyValue[1].Trim();
                        if (keyValue[0] == key)
                        {
                            file.Close();
                            return keyValue[1];
                        }
                    }
                    file.Close();
                }
            }
            catch(Exception e) { throw new FileHandlingException(e.Message);}
            return defaultValue;
        }
        /// <summary>
        /// Write An .ini file value of a key inside a given section
        /// </summary>
        /// <param name="filePath">The path to the .ini file</param>
        /// <param name="section">The desired section</param>
        /// <param name="key">The desired key inside the given section</param>
        /// <param name="value">The value to write.</param>
        /// <returns>true in case of success, false otherwise</returns>
        ///<exception cref="System.ArgumentNullException">either section / key are null</exception>
        /// <exception cref="Parsers.FileHandlingException">Any exception thrown by file handling methods.</exception>
        public static bool WriteValue(string filePath, string section, string key, string value)
        {
            string line, rawLine;
            string currentSection = null;
            bool updated = false;
            string tempFile = System.IO.Path.GetFileName(filePath);
            if (section == null || key == null) throw new ArgumentNullException("Section or key are null");
            key = key.Trim();
            section = section.Trim().TrimStart('[').TrimEnd(']');
            string[] keyValue;
            try
            {
                using (var sr = new System.IO.StreamReader(filePath))
                {
                    using (var sw = new System.IO.StreamWriter(System.IO.File.Create(tempFile)))
                    {
                        while (!sr.EndOfStream)
                        {
                            rawLine = sr.ReadLine();
                            if (updated) { sw.WriteLine(rawLine); continue; }
                            line = rawLine.TrimStart();
                            if (line.Length == 0) { sw.WriteLine(rawLine); continue; }
                            if (line.StartsWith(";")) { sw.WriteLine(rawLine); continue; }
                            if (line.StartsWith("[")) //section
                            {
                                line = line.TrimStart('[');
                                try { line = line.Remove(line.IndexOf(']')); }
                                catch { }
                                currentSection = line;
                                sw.WriteLine(rawLine);
                                continue;
                            }
                            if (currentSection == null || currentSection != section) { sw.WriteLine(rawLine); continue; }
                            keyValue = line.Split(new char[] { '=' }, 2);
                            if (keyValue == null || keyValue.Length != 2) { sw.WriteLine(rawLine); continue; }
                            keyValue[0] = keyValue[0].Trim();
                            if (keyValue[1].Contains(';')) keyValue[1] = keyValue[1].Remove(keyValue[1].IndexOf(';'));
                            keyValue[1] = keyValue[1].Trim();
                            if (keyValue[0] == key)
                            {
                                updated = true;
                                rawLine = rawLine.Replace(keyValue[1], value);
                            }
                            sw.WriteLine(rawLine);
                        }
                    }
                }
                if (updated) System.IO.File.Copy(tempFile, filePath, true);
                else System.IO.File.Delete(tempFile);
            }
            catch (Exception e) {throw new FileHandlingException(e.Message);}
            return updated;
        }
        /// <summary>
        /// Read all sections in the given file
        /// </summary>
        /// <param name="filePath">The path to the .ini file</param>
        /// <returns>All the file sections</returns>
        /// <exception cref="Parsers.FileHandlingException">Any exception thrown by file handling methods.</exception>
        public static string[] ReadAllSections(string filePath)
        {
            string line;
            List<string> sections = new List<string>();
            try
            {
                using (var file = new System.IO.StreamReader(filePath))
                {
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        line = line.TrimStart();
                        if (line.Length == 0) continue;
                        if (line.StartsWith(";")) continue;
                        if (line.StartsWith("[")) //section
                        {
                            line = line.TrimStart('[');
                            try { line = line.Remove(line.IndexOf(']')); }
                            catch { }
                            sections.Add(line);
                            continue;
                        }
                    }
                    file.Close();
                }
            }
            catch (Exception e) { throw new FileHandlingException(e.Message); }
            return sections.ToArray();
        }
        /// <summary>
        /// Read all keys of a given section
        /// </summary>
        /// <param name="filePath">The path to the .ini file</param>
        /// <param name="section">The desired section</param>
        /// <returns>All the given section keys in case exists, otherwise an empty array</returns>
        /// <exception cref="Parsers.FileHandlingException">Any exception thrown by file handling methods.</exception>
        public static string[] ReadAllKeys(string filePath, string section)
        {
            string line;
            bool desiredSectionFound = false;
            string currentSection = null;
            section = section.Trim().TrimStart('[').TrimEnd(']');
            List<string> keys = new List<string>();
            try
            {
                using (var file = new System.IO.StreamReader(filePath))
                {
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        line = line.TrimStart();
                        if (line.Length == 0) continue;
                        if (line.StartsWith(";")) continue;
                        if (line.StartsWith("[")) //section
                        {
                            line = line.TrimStart('[');
                            try { line = line.Remove(line.IndexOf(']')); }
                            catch { }
                            currentSection = line;
                            if (desiredSectionFound)
                            {
                                file.Close();
                                break;
                            }
                            desiredSectionFound = (currentSection == section);
                            continue;
                        }
                        if (currentSection == null || currentSection != section) continue;
                        if (!line.Contains('=')) continue;
                        line = line.Remove(line.IndexOf('='));
                        line = line.Trim();
                        keys.Add(line);
                    }
                    file.Close();
                }
            }
            catch (Exception e) { throw new FileHandlingException(e.Message); }
            return keys.ToArray();
        }
        #endregion

        #region Inner Variables
        private Dictionary<string, Dictionary<string,string>> sections;

        // for faster equal operations
        private DateTime lastWriteTime;
        #endregion
        #region Public Variables
        /// <summary>
        /// Represents this object name. 
        /// if not supplied to constructor will hold the file Path.
        /// </summary>
        public string Name {get; private set;}
        /// <summary>Returns All the sections of the ini</summary>
        public string[] Sections
        {
            get
            {
                return sections.Keys.ToArray();
            }
        }
        #endregion

        
        #region Public API
        /// <summary>
        /// Represents an .ini file.
        /// Reads the given file at creation.
        /// In case a section is repeated in the file only the First one is taken.
        /// In case a key is repeated in a section, only the first one is taken.
        /// </summary>
        /// <param name="filePath">The file to open for reading all it's content</param>
        /// <exception cref="System.ArgumentException">
        /// path is a zero-length string, contains only white space, or contains one
        /// or more invalid characters as defined by System.IO.Path.InvalidPathChars.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">path is null</exception>
        /// <exception cref="System.IO.PathTooLongException">
        /// The specified path, file name, or both exceed the system-defined maximum
        ///  length. For example, on Windows-based platforms, paths must be less than
        ///  248 characters, and file names must be less than 260 characters
        /// </exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive)</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// path specified a file that is read-only.  -or- This operation is not supported
        /// on the current platform.  -or- path specified a directory.  -or- The caller
        /// does not have the required permission
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException"/>
        /// <exception cref="System.NotSupportedException"/>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission</exception>
        /// <exception cref="Parsers.SectionExistsException">Section is repeated in the file</exception>
        /// <exception cref="Parsers.KeyExistsException">Key is repeated in a section</exception>
        public INIFile(string filePath)
        {
            string[] fileLines = System.IO.File.ReadAllLines(filePath);
            lastWriteTime = System.IO.File.GetLastWriteTime(filePath);
            Name = filePath;
            buildStructures(fileLines);
        }

        public INIFile(){}

        /// <summary>
        /// Load the given file.
        /// In case a section is repeated in the file only the First one is taken.
        /// In case a key is repeated in a section, only the first one is taken.
        /// </summary>
        /// <param name="filePath">The file to open for reading all it's content</param>
        /// <exception cref="System.ArgumentException">
        /// path is a zero-length string, contains only white space, or contains one
        /// or more invalid characters as defined by System.IO.Path.InvalidPathChars.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">path is null</exception>
        /// <exception cref="System.IO.PathTooLongException">
        /// The specified path, file name, or both exceed the system-defined maximum
        ///  length. For example, on Windows-based platforms, paths must be less than
        ///  248 characters, and file names must be less than 260 characters
        /// </exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive)</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// path specified a file that is read-only.  -or- This operation is not supported
        /// on the current platform.  -or- path specified a directory.  -or- The caller
        /// does not have the required permission
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException"/>
        /// <exception cref="System.NotSupportedException"/>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission</exception>
        /// <exception cref="Parsers.SectionExistsException">Section is repeated in the file</exception>
        /// <exception cref="Parsers.KeyExistsException">Key is repeated in a section</exception>
        public void LoadFile(string filePath)
        {
            string[] fileLines = System.IO.File.ReadAllLines(filePath);
            lastWriteTime = System.IO.File.GetLastWriteTime(filePath);
            Name = filePath;
            buildStructures(fileLines);
        }

        #region Override/Overload Methods
        public bool Equals(INIFile secondINI)
        {
            if (secondINI == null) return false;
            if (this.Name == secondINI.Name && this.lastWriteTime == secondINI.lastWriteTime) return true;
            if (this.sections == null && secondINI.sections == null) return true;
            return (this.sections.Except(secondINI.sections).Concat(secondINI.sections.Except(this.sections)).Count() == 0);
        }
        public override bool Equals(object obj)
        {
            INIFile secondINI = obj as INIFile;
            if (secondINI == null) return false;
            if (this.Name == secondINI.Name && this.lastWriteTime == secondINI.lastWriteTime) return true;
            if (this.sections == null && secondINI.sections == null) return true;
            return (this.sections.Except(secondINI.sections).Concat(secondINI.sections.Except(this.sections)).Count() == 0);
        }
        public override int GetHashCode()
        {
            if (this.sections == null) return 0;
            return (this.Name + ", Last Write: " + this.lastWriteTime).GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
        public static bool operator ==(INIFile a,INIFile b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            if (a.Name == b.Name && a.lastWriteTime == b.lastWriteTime) return true;
            if (a.sections == null && b.sections == null) return true;
            return (a.sections.Except(b.sections).Concat(b.sections.Except(a.sections)).Count() == 0);
        }
        public static bool operator !=(INIFile a, INIFile b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Get a value of a key in a section.
        /// </summary>
        /// <param name="section">The section to look in</param>
        /// <param name="key">The desired Key</param>
        /// <returns>The desired value or null In case section or key don't exists </returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public string this[string section, string key]
        {
            get
            {
                return this.ReadValue(section, key);
            }
        }
        /// <summary>
        /// Get an array of all keys in a section.
        /// In case section is not found an empty array will be returned.
        /// </summary>
        /// <param name="section">The Desired section</param>
        /// <returns>Array of the desired section keys or an empty array in case the section was not found.</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public string[] this[string section]
        {
            get { return this.ReadAllKeys(section); }
        }
        #endregion
        /// <summary>
        /// Determines whether the ini file<TKey,TValue>
        /// contains the specified Section.
        /// </summary>
        /// <param name="section">The section to look for.</param>
        /// <returns>true if the section exists, false otherwise</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual bool ContainsSection(string section)
        {
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            if (section == null) return false;
            section = section.Trim().TrimStart('[').TrimEnd(']');
            if (section.Length == 0) return false;
            return sections.ContainsKey(section);
        }
        /// <summary>
        /// Determines whether the given section contains the key.
        /// </summary>
        /// <param name="section">The Section to look in. in case doesn't exists will return false.</param>
        /// <param name="key">The key to look for.</param>
        /// <returns>true if section and key exists, false otherwise</returns>
        ///<exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual bool ContainsSectionKey(string section, string key)
        {
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            if (key == null || !ContainsSection(section)) return false;
            var keys = sections[section];
            return keys.ContainsKey(key);
        }
        
        /// <summary>
        /// get the value of the given key in the given section
        /// </summary>
        /// <param name="section">The section to look in</param>
        /// <param name="key">The desired Key</param>
        /// <returns>The desired value or null In case section or key don't exists</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual string ReadValue(string section, string key)
        {
            string value = null;
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            if (section == null || key == null) return value;
            section = section.Trim().TrimStart('[').TrimEnd(']');
            if (section.Length == 0 || !sections.ContainsKey(section)) return value;
            var keys = sections[section];
            if (!keys.ContainsKey(key)) return value;
            value = keys[key];
            return value;
        }
        /// <summary>
        /// get the value of the given key in the given section.
        /// In case section / key is not found the <paramref name="defaultValue"/> will be returned.
        /// </summary>
        /// <param name="section">The section to look in</param>
        /// <param name="key">The desired Key</param>
        /// <returns>The desired value or the <paramref name="defaultValue"/> In case section or key don't exists</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual string ReadValueOrDefault(string section, string key, string defaultValue)
        {
            string ret = null;
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            try { ret = this.ReadValue(section, key); }
            catch { }
            return (ret == null) ? defaultValue : ret;
        }
        /// <summary>
        /// Get an array of all keys in a section.
        /// In case section is not found an empty array will be returned.
        /// </summary>
        /// <param name="section">The Desired section</param>
        /// <returns>Array of the desired section keys or an empty array in case the section was not found.</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual string[] ReadAllKeys(string section)
        {
            var keys = new string[0];
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            if (section == null) return keys;
            section = section.Trim().TrimStart('[').TrimEnd(']');
            if (section.Length == 0 || !sections.ContainsKey(section)) return keys;

            try { keys = sections[section].Keys.ToArray(); }
            catch { }
            return keys;
        }
        /// <summary>
        /// Get an array of all keys and values in a section.
        /// In case section is not found an empty array will be returned.
        /// </summary>
        /// <param name="section">The Desired section</param>
        /// <returns>Array of the desired section keys and values or an empty array in case the section was not found.</returns>
        /// <exception cref="FileNotSuppliedException">The current object was not loaded with a file</exception>
        public virtual KeyValuePair<string,string>[] ReadAllKeyValuePairs(string section)
        {
            if (this.sections == null) throw new FileNotSuppliedException("No File was supplied To the current object");
            var kvps = new KeyValuePair<string, string>[0];
            if (section == null) return kvps;
            section = section.Trim().TrimStart('[').TrimEnd(']');
            if (section.Length == 0 || !sections.ContainsKey(section)) return kvps;

            try { kvps = sections[section].ToArray(); }
            catch { }
            return kvps;
        }
        #endregion
        #region Private API
        /// <summary>
        /// Build Object inner structures
        /// </summary>
        private void buildStructures(string[] fileLines)
        {
            string line;
            bool skipSectionKeys = true;
            string currentSection = null;
            sections = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> sectionKeys = null;
            string[] keyValue;
            foreach (var rawLine in fileLines)
            {
                line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith(";")) continue;
                if (line.StartsWith("[")) //section
                {
                    skipSectionKeys = false;
                    line = line.TrimStart('[').TrimEnd(']');
                    if (currentSection != null)
                    {
                        skipSectionKeys = sections.ContainsKey(currentSection);
                        if (!skipSectionKeys) sections.Add(currentSection, sectionKeys);
                    }
                    currentSection = line;
                    if (!skipSectionKeys) sectionKeys = new Dictionary<string, string>();
                    continue;
                }
                if (skipSectionKeys) continue;
                keyValue = line.Split(new char[] { '=' }, 2);
                if (keyValue == null || keyValue.Length != 2) continue; //no '=' in line
                keyValue[0] = keyValue[0].Trim();
                if (keyValue[0].Length == 0) continue; //no empty keys
                if (keyValue[1].Contains(';')) keyValue[1] = keyValue[1].Remove(keyValue[1].IndexOf(';')); //trim trailing remarks
                keyValue[1] = keyValue[1].Trim();
                if (sectionKeys.ContainsKey(keyValue[0])) continue;
                sectionKeys.Add(keyValue[0], keyValue[1]);
            }
            if (!skipSectionKeys) sections.Add(currentSection, sectionKeys);
        }
        #endregion
    }
}
