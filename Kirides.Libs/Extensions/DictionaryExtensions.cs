using System;
using System.Collections.Generic;

namespace Kirides.Libs.Extensions
{
    /// <summary>
    /// Offers thread-safe methods for <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class IDictionaryExtensions
    {
        public static void AddSafe<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                dictionary.Add(key, value);
        }
        public static bool TryGetSafe<K, V>(this IDictionary<K, V> dictionary, K key, out V value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                return dictionary.TryGetValue(key, out value);
        }
        public static void RemoveSafe<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                dictionary.Remove(key);
        }
        public static bool ContainsKeySafe<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            lock (dictionary)
                return dictionary.ContainsKey(key);
        }
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueGenerator)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            if (valueGenerator == null)
                throw new ArgumentNullException(nameof(valueGenerator));

            V value;
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out value))
                {
                    value = valueGenerator.Invoke();
                    dictionary.Add(key, value);
                }
            }
            return value;
        }

    }
}
