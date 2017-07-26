using System;
using System.Collections.Generic;

namespace Kirides.Libs.Extensions
{
    /// <summary>
    /// Offers thread-safe methods for <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class IDictionaryExtensions
    {
        public static void AddSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                dictionary.Add(key, value);
        }
        public static bool TryGetSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                return dictionary.TryGetValue(key, out value);
        }
        public static void RemoveSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                dictionary.Remove(key);
        }
        public static bool ContainsKeySafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                return dictionary.ContainsKey(key);
        }
        public static TValue GetOrAddSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey,TValue> valueGenerator)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            if (valueGenerator == null)
                throw new ArgumentNullException(nameof(valueGenerator));

            TValue value;
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out value))
                {
                    value = valueGenerator.Invoke(key);
                    dictionary.Add(key, value);
                }
            }
            return value;
        }

    }
}
