using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kirides.Libs.Configuration
{

    /// <summary>
    /// Represents an Ini Key-Value Pair with its trivia, like comments and whitespace
    /// </summary>
    public class SectionValue
    {
        public List<string> LeadingTrivia { get; } = new(0);

        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Holds typical Ini Key-Value Pairs
    /// </summary>
    public class Section
    {
        /// <summary>
        /// The options used to create this section
        /// </summary>
        public IniOptions Options { get; }

        /// <summary>
        /// Leading Trivia, like comments and whitespace
        /// </summary>
        public List<string> LeadingTrivia { get; } = new(0);

        /// <summary>
        /// Title of this section
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// All the Entries
        /// </summary>
        public List<SectionValue> Entries { get; } = new();

        /// <summary>
        /// Returns a Key-Value pair to work with. If it doesn't exist yet, it will be created
        /// </summary>
        public SectionValue this[string key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Tries to returns a Key-Value pair to work with. If it doesn't exist, it will return false.
        /// </summary>
        public bool TryGetValue(string key, out SectionValue value)
        {
            var entry = Entries.FirstOrDefault(x => string.Equals(x.Key, key, Options.KeyComparer));
            if (entry != null)
            {
                value = entry;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Returns a Key-Value pair to work with. If it doesn't exist yet, it will be created
        /// </summary>
        public SectionValue Get(string key)
        {
            if (TryGetValue(key, out var entry))
            {
                return entry;
            }
            entry = new SectionValue
            {
                Key = key,
            };
            Entries.Add(entry);
            return entry;
        }

        public void Set(string key, SectionValue value)
        {
            var entry = Get(key);
            entry.Value = value.Value;

            entry.LeadingTrivia.Clear();
            entry.LeadingTrivia.AddRange(value.LeadingTrivia);
        }

        public void Set(string key, string value, bool clearTrivia = false)
        {
            var entry = Get(key);
            entry.Value = value;
            if (clearTrivia)
            {
                entry.LeadingTrivia.Clear();
            }
        }

        public Section(IniOptions options)
        {
            Options = options;
        }

        public void Remove(string key)
        {
            Entries.RemoveAll(e => string.Equals(e.Key, key, Options.KeyComparer));
        }
    }

    public class IniOptions
    {
        public StringComparison KeyComparer { get; init; } = StringComparison.Ordinal;
        public StringComparison SectionComparer { get; init; } = StringComparison.Ordinal;

        public bool ThrowOnInvalidLines { get; init; } = false;
        public bool KeepInvalidLines { get; init; } = true;
        public bool AllowEmptyKeys { get; init; } = true;
        public bool TrimValues { get; init; } = true;
        public Action<int, string> OnInvalidLine { get; init; } = (_, __) => { };

        public string SectionOpen { get; init; } = "[";
        public string SectionClose { get; init; } = "]";

        /// <summary>
        /// Default Comment check function is <see cref="SemicolonComment" />
        /// </summary>
        public Func<string, bool> IsCommentFunc { get; init; } = SemicolonComment;

        public static bool SemicolonComment(string s) => s.AsSpan().TrimStart().StartsWith(";");
        public static bool HashComment(string s) => s.AsSpan().TrimStart().StartsWith("#");
        public static bool SemicolonOrHashComment(string s) => s.AsSpan().TrimStart().IndexOfAny("#;") == 0;
    }

    /// <summary>
    /// Represents the ini file structure.
    /// </summary>
    public class Ini
    {
        /// <summary>
        /// Represents the global section
        /// </summary>
        public Section GlobalSection { get; }
        /// <summary>
        /// A List of named Sections
        /// </summary>
        public List<Section> Sections { get; } = new();

        /// <summary>
        /// The Options used to create this Ini file
        /// </summary>
        public IniOptions Options { get; }

        /// <summary>
        /// Any trailing trivia of the Global section
        /// </summary>
        public List<string> TrailingTrivia { get; } = new(0);

        public Ini(IniOptions options)
        {
            Options = options;
            GlobalSection = new Section(options);
        }

        /// <summary>
        /// Returns a section to work with. If it doesn't exist yet, it will be created
        /// </summary>
        public Section this[string key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Returns a section to work with. If it doesn't exist, it will return false
        /// </summary>
        public bool TryGetSection(string title, out Section section)
        {
            var entry = Sections.FirstOrDefault(x => string.Equals(x.Title, title, Options.SectionComparer));
            if (entry != null)
            {
                section = entry;
                return true;
            }
            section = null;
            return false;
        }

        /// <summary>
        /// Returns a section to work with. If it doesn't exist yet, it will be created
        /// </summary>
        public Section Get(string title)
        {
            if (TryGetSection(title, out var entry))
            {
                return entry;
            }
            entry = new Section(Options);
            Sections.Add(entry);
            return entry;
        }

        /// <summary>
        /// Updates or Creates a section with the given value
        /// </summary>
        void Set(string title, Section value)
        {
            if (TryGetSection(title, out var entry))
            {
                entry.Entries.Clear();
                entry.Entries.AddRange(value.Entries);

                entry.LeadingTrivia.Clear();
                entry.LeadingTrivia.AddRange(value.LeadingTrivia);
            }
            else
            {
                Sections.Add(value);
            }
        }

        /// <summary>
        /// Removes all sections with the given name
        /// </summary>
        public void Remove(string section)
        {
            Sections.RemoveAll(s => string.Equals(s.Title, section, Options.SectionComparer));
        }

        /// <summary>
        /// Returns string representation of the this Ini
        /// </summary>
        public override string ToString()
        {
            var sw = new StringWriter();
            this.Write(sw);
            return sw.ToString();
        }
    }

    public static class IniExtension
    {
        public static void Write(this Ini ini, TextWriter writer)
        {
            new IniWriter(ini.Options).Write(writer, ini);
        }

        public static void Read(this Ini ini, TextReader reader)
        {
            new IniReader(ini.Options).Read(reader, ini);
        }

        public static bool HasSection(this Ini ini, string title) => ini.TryGetSection(title, out _);
        public static bool HasKey(this Section section, string key) => section.TryGetValue(key, out _);

        public static string GetValueOrDefault(this Section section, string key, string defaultValue)
            => section.TryGetValue(key, out var value)
                ? value.Value
                : defaultValue;

        public static T GetValueOrDefailt<T>(this Section section, string key, T defaultValue)
        {
            if (section.TryGetValue(key, out var value))
            {
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(value.Value);
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)long.Parse(value.Value);
                }
                else if (typeof(T) == typeof(uint))
                {
                    return (T)(object)uint.Parse(value.Value);
                }
                else if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)ulong.Parse(value.Value);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)ushort.Parse(value.Value);
                }
                else if (typeof(T) == typeof(short))
                {
                    return (T)(object)short.Parse(value.Value);
                }
                else if (typeof(T) == typeof(byte))
                {
                    return (T)(object)byte.Parse(value.Value);
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    return (T)(object)sbyte.Parse(value.Value);
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(value.Value);
                }
                else if (typeof(T) == typeof(decimal))
                {
                    return (T)(object)decimal.Parse(value.Value);
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)float.Parse(value.Value);
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)double.Parse(value.Value);
                }

                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(value.Value);
                }
            }

            return defaultValue;
        }
    }

    public interface IIniReader
    {
        void Read(TextReader reader, Ini ini);
    }

    public interface IIniWriter
    {
        void Write(TextWriter writer, Ini ini);
    }

    public class IniWriter : IIniWriter
    {
        private readonly IniOptions _options;
        public IniWriter(IniOptions options)
        {
            _options = options;
        }

        public void Write(TextWriter writer, Ini ini)
        {
            WriteSection(writer, ini.GlobalSection, withTitle: false);

            foreach (var section in ini.Sections)
            {
                WriteSection(writer, section);
            }

            foreach (var trivia in ini.TrailingTrivia)
            {
                writer.WriteLine(trivia);
            }

            void WriteSection(TextWriter writer, Section section, bool withTitle = true)
            {
                foreach (var trivia in section.LeadingTrivia)
                {
                    writer.WriteLine(trivia);
                }
                if (withTitle)
                {
                    writer.Write(_options.SectionOpen);
                    writer.Write(section.Title);
                    writer.WriteLine(_options.SectionClose);
                }
                foreach (var entry in section.Entries)
                {
                    foreach (var trivia in entry.LeadingTrivia)
                    {
                        writer.WriteLine(trivia);
                    }
                    writer.Write(entry.Key);
                    writer.Write("=");
                    writer.WriteLine(entry.Value);
                }
            }
        }
    }

    public class IniReader : IIniReader
    {
        private readonly IniOptions _options;
        public IniReader(IniOptions options)
        {
            _options = options;
        }

        public void Read(TextReader reader, Ini ini)
        {
            var leadingTrivia = new List<string>();
            var trailingTrivia = new List<string>();

            var currentSection = ini.GlobalSection;
            int currentLine = 0;
            while (reader.ReadLine() is string line)
            {
                currentLine++;

                var trimmed = line.AsSpan().Trim();

                if (trimmed.StartsWith(_options.SectionOpen) && trimmed.EndsWith(_options.SectionClose))
                {
                    currentSection = new Section(_options)
                    {
                        Title = trimmed.Slice(1, trimmed.Length - 2).ToString(),
                    };

                    currentSection.LeadingTrivia.AddRange(leadingTrivia);
                    leadingTrivia.Clear();

                    ini.Sections.Add(currentSection);
                    continue;
                }

                if (_options.IsCommentFunc(line))
                {
                    leadingTrivia.Add(line);
                    continue;
                }

                var expectedEqualsStart = _options.AllowEmptyKeys ? 0 : 1;
                if (trimmed.IndexOf("=") is int idxEquals && idxEquals >= expectedEqualsStart)
                {
                    idxEquals = line.IndexOf("=");
                    var lineSpan = line.AsSpan();

                    var key = lineSpan.Slice(0, idxEquals).Trim().ToString();
                    var value = lineSpan.Slice(idxEquals + 1);
                    if (_options.TrimValues)
                    {
                        value = value.Trim();
                    }
                    var sectionValue = new SectionValue
                    {
                        Key = key,
                        Value = value.ToString(),
                    };

                    sectionValue.LeadingTrivia.AddRange(leadingTrivia);
                    leadingTrivia.Clear();

                    currentSection.Set(key, sectionValue);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line))
                {
                    leadingTrivia.Add(line);
                    continue;
                }

                // invalid line according to configuration
                if (_options.KeepInvalidLines)
                {
                    leadingTrivia.Add(line);
                }
                _options.OnInvalidLine?.Invoke(currentLine, line);
                if (_options.ThrowOnInvalidLines)
                {
                    throw new InvalidDataException($"Invalid line {currentLine}: \"{line}\"");
                }
            }

            // still something left, must be end of ini comments or smth.
            if (leadingTrivia.Count > 0)
            {
                ini.TrailingTrivia.AddRange(leadingTrivia);
            }
        }

        public Ini Read(TextReader reader)
        {
            var ini = new Ini(_options);
            Read(reader, ini);
            return ini;
        }
    }
}