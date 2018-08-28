using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kirides.Libs.Configuration
{
    /// <summary>
    /// Default implementation of the IIniConfig interface
    /// </summary>
    public class IniConfig : IIniConfig
    {
        const string DEFAULT_SECTION = "General";

        /// <summary>
        /// Returns an IIniConfig from a given file.
        /// </summary>
        /// <param name="filePath">Full or relative path to the Ini-file</param>
        /// <returns>Implementation of IIniConfig</returns>
        public static IIniConfig From(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            return new IniConfig().LoadStream(File.OpenRead(filePath));
        }

        /// <summary>
        /// Returns an IIniConfig from a given file.
        /// </summary>
        /// <param name="filePath">Full or relative path to the Ini-file</param>
        /// <returns>Implementation of IIniConfig</returns>
        public static IIniConfig From(Stream stream)
        {
            return new IniConfig().LoadStream(stream);
        }

        /// <summary>
        /// Returns an IIniConfig from a given Ini-string.
        /// </summary>
        /// <param name="ini">string containing the Ini</param>
        /// <returns>Implementation of IIniConfig</returns>
        public static IIniConfig Parse(string ini)
        {
            return new IniConfig().LoadString(ini);
        }

        private IList<IIniSection> _ini;

        IniConfig()
        {
            _ini = new List<IIniSection>();
        }

        public IIniConfig LoadStream(Stream stream)
        {
            lock (_ini)
            {
                _ini.Clear();
                using (StreamReader sr = new StreamReader(stream))
                {
                    string actSection = DEFAULT_SECTION;
                    string actLine = null;
                    while ((actLine = sr.ReadLine()) != null)
                    {
                        actSection = HandleLine(actLine, actSection);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Creates an IIniConfig from an Ini-String
        /// </summary>
        /// <param name="ini">The Ini-string</param>
        /// <returns>Implementation of IIniConfig</returns>
        IIniConfig LoadString(string ini)
        {
            lock (_ini)
            {
                _ini.Clear();
                using (var sr = new StringReader(ini))
                {
                    string actSection = DEFAULT_SECTION;
                    while (sr.ReadLine() is var actLine && actLine != null)
                    {
                        actSection = HandleLine(actLine, actSection);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Parses a line, adds either a section or a Key-Value-Pair to the Ini
        /// </summary>
        /// <param name="actLine"></param>
        /// <param name="actSection"></param>
        /// <returns>The section that is now being worked with</returns>
        private string HandleLine(string actLine, string actSection)
        {
            actLine = actLine.Trim();
            if (actLine.StartsWith("#", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(actLine))
                return actSection;

            if (actLine.StartsWith("[", StringComparison.OrdinalIgnoreCase) && actLine.EndsWith("]", StringComparison.OrdinalIgnoreCase))
            {
                actSection = actLine.Substring(1, actLine.Length - 2);
                AddSection(actSection);
            }
            else if (actLine.Contains('='))
            {
                int indexOfEqSign = actLine.IndexOf('=');
                string key = actLine.Substring(0, indexOfEqSign);
                string value = actLine.Substring(indexOfEqSign + 1);

                var section = GetOrAddSection(actSection, AddSection);
                section.AddOrReplace(key, value);
            }
            return actSection;
        }

        /// <summary>
        /// Adds (if not exists) an IIniSection with the given Name and returns it.
        /// <para>
        /// If a section already exists, it returns null.
        /// </para>
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        public IIniSection AddSection(string section)
        {
            lock (_ini)
                if (!_ini.Any(se => string.Equals(se.Name, section, StringComparison.OrdinalIgnoreCase)))
                {
                    var iniSection = new IniSection(section);
                    _ini.Add(iniSection);
                    return iniSection;
                }
            return null;
        }

        /// <summary>
        /// Returns an Ini-section if it exists, or generates a new one using the provided generateSection method.
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <param name="generateSection">Function to generate a new section if it does not exist. Parameter is the Section</param>
        /// <returns>Implementation of IIniSection</returns>
        public IIniSection GetOrAddSection(string section, Func<string, IIniSection> generateSection)
        {
            IIniSection sec = GetSection(section);
            if (sec == null)
            {
                sec = generateSection(section);
                lock (_ini)
                    _ini.Add(sec);
            }
            return sec;
        }

        /// <summary>
        /// Returns the requested Ini-section if it exists, else it creates it using <see cref="AddSection(string)"/> method.
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        public IIniSection GetOrAddSection(string section)
            => GetOrAddSection(section, AddSection);

        /// <summary>
        /// Returns the requested Ini-section if it exists, else it creates it using <see cref="AddSection(string)"/> method.
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        public IIniSection this[string section]
            => GetOrAddSection(section, AddSection);

        /// <summary>
        /// Returns the requested Ini-section if it exists
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        public IIniSection GetSection(string section)
        {
            lock (_ini)
                return _ini.FirstOrDefault(x => string.Equals(x.Name, section, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Saves the current Ini to the specified file
        /// </summary>
        /// <param name="fileName">Full or relative path to the file</param>
        public void SaveTo(string fileName)
        {
            using (var fs = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                byte[] buffer;
                for (int i = 0; i < _ini.Count; i++)
                {
                    buffer = Encoding.UTF8.GetBytes($"[{_ini[i].Name}]{Environment.NewLine}");
                    fs.Write(buffer, 0, buffer.Length);
                    for (int j = 0; j < _ini[i].KeyValuePairs.Count; j++)
                    {
                        var pair = _ini[i].KeyValuePairs.ElementAt(j);

                        buffer = Encoding.UTF8.GetBytes($"{pair.Key}={pair.Value ?? ""}{Environment.NewLine}");
                        fs.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the current Ini to the specified <paramref name="stream"/> using <see cref="Encoding.UTF8"/> as encoding.
        /// <para/>
        /// Uses <see cref="Environment.NewLine"/> for line endings.
        /// </summary>
        public void SaveTo(Stream stream)
        {
            byte[] buffer;
            for (int i = 0; i < _ini.Count; i++)
            {
                buffer = Encoding.UTF8.GetBytes($"[{_ini[i].Name}]{Environment.NewLine}");
                stream.Write(buffer, 0, buffer.Length);
                for (int j = 0; j < _ini[i].KeyValuePairs.Count; j++)
                {
                    var pair = _ini[i].KeyValuePairs.ElementAt(j);

                    buffer = Encoding.UTF8.GetBytes($"{pair.Key}={pair.Value ?? ""}{Environment.NewLine}");
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Removes the given section and all of its keys from the Ini
        /// </summary>
        /// <param name="section">Name of the section</param>
        public void RemoveSection(string section)
        {
            lock (_ini)
            {
                var iniSection = _ini.FirstOrDefault(se => string.Equals(se.Name, section, StringComparison.OrdinalIgnoreCase));
                if (iniSection != null)
                    _ini.Remove(iniSection);
            }
        }

        /// <summary>
        /// Represents a section of an Ini-file
        /// </summary>
        private class IniSection : IIniSection
        {
            /// <summary>
            /// Name of this Section
            /// </summary>
            public string Name { get; }

            public IDictionary<string, object> KeyValuePairs { get; }

            internal IniSection(string name)
            {
                this.Name = name;
                this.KeyValuePairs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Returns the Value of a given Key in the requested Type.
            /// </summary>
            /// <typeparam name="T">Type of the Value</typeparam>
            /// <param name="key">Name of the key</param>
            /// <exception cref="KeyNotFoundException">Thrown when the Key was not found</exception>
            /// <exception cref="FormatException">Thrown when the the Value could not be converted to the requested Type</exception>
            /// <exception cref="InvalidCastException">Thrown when the the Value could not be converted to the requested Type</exception>
            /// <exception cref="OverflowException">Thrown when a numeric Value was requested but did not fit into the Type (ex.: long.MaxValue -> int)</exception>
            /// <returns>Value with the requested Type</returns>
            public T GetValue<T>(string key)
            {
                T result = default(T);
                lock (KeyValuePairs)
                {
                    result = (T)Convert.ChangeType(KeyValuePairs[key], typeof(T));
                }
                return result;
            }
            /// <summary>
            /// For details see:
            /// <see cref="GetValue{T}(string)"/>
            /// </summary>
            /// <param name="key">Name of the key</param>
            /// <returns>Value as string</returns>
            public object GetValue(string key)
                => GetValue<string>(key);

            /// <summary>
            /// For details see:
            /// <see cref="GetValue(string)"/>
            /// </summary>
            /// <param name="key">Name of the key</param>
            /// <returns>Value as string</returns>
            public object this[string key]
            {
                get => GetValue(key);
                set => AddOrReplace(key, value);
            }

            /// <summary>
            /// Adds or replaces a key to/from the Ini
            /// </summary>
            /// <param name="key">The key to be added/replaced</param>
            /// <param name="value">the value of the key</param>
            public void AddOrReplace(string key, object value)
            {
                lock (KeyValuePairs)
                    KeyValuePairs[key] = value;
            }

            /// <summary>
            /// Adds or replaces a key to/from the Ini
            /// </summary>
            /// <param name="key">The key to be added/replaced</param>
            /// <param name="value">(optional)the value of the key</param>
            public void AddOrReplaceKey(string key, object value = null)
            {
                AddOrReplace(key, value);
            }

            /// <summary>
            /// Removes the given key from the Ini. (if it exists)
            /// </summary>
            /// <param name="key">The key</param>
            public void RemoveKey(string key)
            {
                lock (KeyValuePairs)
                    if (KeyValuePairs.ContainsKey(key))
                        KeyValuePairs.Remove(key);
            }
        }
    }

    /// <summary>
    /// Represents a basic Ini-file with sections and keys with values.
    /// </summary>
    public interface IIniConfig
    {
        /// <summary>
        /// Returns an IIniSection, discovered by its name
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection</returns>
        IIniSection this[string section] { get; }
        /// <summary>
        /// Returns the requested Ini-section if it exists
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        IIniSection GetSection(string section);
        /// <summary>
        /// Returns the requested Ini-section if it exists, else it creates it using <see cref="AddSection(string)"/> method.
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection or null</returns>
        IIniSection GetOrAddSection(string section);
        /// <summary>
        /// Returns an Ini-section if it exists, or generates a new one using the provided generateSection method.
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <param name="generateSection">Function to generate a new section if it does not exist</param>
        /// <returns>Implementation of IIniSection</returns>
        IIniSection GetOrAddSection(string section, Func<string, IIniSection> generateSection);
        /// <summary>
        /// Removes the given section and all of its keys from the Ini
        /// </summary>
        /// <param name="section">Name of the section</param>
        void RemoveSection(string section);
        /// <summary>
        /// Adds (if not exists) an IIniSection with the given Name
        /// </summary>
        /// <param name="section">Name of the section</param>
        /// <returns>Implementation of IIniSection</returns>
        IIniSection AddSection(string section);
        /// <summary>
        /// Saves the current Ini to the specified file
        /// </summary>
        /// <param name="fileName">Full or relative path to the file</param>
        void SaveTo(string fileName);
        /// <summary>
        /// Writes the current Ini to the specified <paramref name="stream"/> using <see cref="Encoding.UTF8"/> as encoding.
        /// </summary>
        void SaveTo(Stream stream);
    }

    /// <summary>
    /// Represents a section of an Ini-file
    /// </summary>
    public interface IIniSection
    {
        /// <summary>
        /// Name of this Section
        /// </summary>
        string Name { get; }
        IDictionary<string, object> KeyValuePairs { get; }
        /// <summary>
        /// Returns the Value of a given Key in the requested Type.
        /// </summary>
        /// <typeparam name="T">Type of the Value</typeparam>
        /// <param name="key">Name of the key</param>
        /// <exception cref="KeyNotFoundException">Thrown when the Key was not found</exception>
        /// <exception cref="FormatException">Thrown when the the Value could not be converted to the requested Type</exception>
        /// <exception cref="InvalidCastException">Thrown when the the Value could not be converted to the requested Type</exception>
        /// <exception cref="OverflowException">Thrown when a numeric Value was requested but did not fit into the Type (ex.: long.MaxValue -> int)</exception>
        /// <returns>Value with the requested Type</returns>
        T GetValue<T>(string key);
        /// <summary>
        /// For details see:
        /// <see cref="GetValue{T}(string)"/>
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <returns>Value as string</returns>
        object GetValue(string key);
        /// <summary>
        /// For details see:
        /// <see cref="GetValue(string)"/>
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <returns>Value as string</returns>
        object this[string key] { get; set; }
        /// <summary>
        /// Adds or replaces a key to/from the Ini
        /// </summary>
        /// <param name="key">The key to be added/replaced</param>
        /// <param name="value">the value of the key</param>
        void AddOrReplace(string key, object value);
        /// <summary>
        /// Adds or replaces a key to/from the Ini
        /// </summary>
        /// <param name="key">The key to be added/replaced</param>
        /// <param name="value">(optional)the value of the key</param>
        void AddOrReplaceKey(string key, object value = null);
        /// <summary>
        /// Removes the given key from the Ini. (if it exists)
        /// </summary>
        /// <param name="key">The key</param>
        void RemoveKey(string key);
    }
}