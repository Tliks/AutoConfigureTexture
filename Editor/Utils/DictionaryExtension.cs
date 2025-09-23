namespace com.aoyon.AutoConfigureTexture;

internal static class DictionaryExtensions
{
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue addValue)
    {
        bool canAdd = !dict.ContainsKey(key);

        if (canAdd)
            dict.Add(key, addValue);

        return canAdd;
    }

    public static bool TryAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        bool canAdd = !dict.ContainsKey(key);
        if (canAdd)
            dict.Add(key, new TValue());
        return canAdd;
    }

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> addValueFactory)
    {
        bool canAdd = !dict.ContainsKey(key);

        if (canAdd)
            dict.Add(key, addValueFactory(key));

        return canAdd;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue addValue)
    {
        dict.TryAdd(key, addValue);
        return dict[key];
    }

    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        dict.TryAddNew(key);
        return dict[key];
    }
    
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory)
    {
        dict.TryAdd(key, valueFactory);
        return dict[key];
    }
}