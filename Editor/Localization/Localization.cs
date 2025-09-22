using System.Globalization;
using System.IO;

namespace com.aoyon.AutoConfigureTexture
{
    internal partial class L10n
    {
        private const string PREFERENCE_KEY = "com.aoyon.AutoConfigureTexture.lang";

        public static string language;
        public static LocalizationAsset localizationAsset;
        private static string[] languages;
        private static string[] languageNames;
        private static readonly Dictionary<string, GUIContent> guicontents = new();
        private static string localizationFolder => AssetDatabase.GUIDToAssetPath("08da4be78bd777d44a816cf4e2232999");

        internal static void Load()
        {
            guicontents.Clear();
            language ??= EditorPrefs.GetString(PREFERENCE_KEY, "en-US");
            var path = localizationFolder + "/" + language + ".po";
            if(File.Exists(path)) localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path);

            if(!localizationAsset) localizationAsset = new LocalizationAsset();
        }

        internal static string[] GetLanguages()
        {
            return languages ??= Directory.GetFiles(localizationFolder).Where(f => f.EndsWith(".po")).Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
        }

        internal static string[] GetLanguageNames()
        {
            return languageNames ??= languages.Select(l => {
                if(l == "zh-Hans") return "简体中文";
                if(l == "zh-Hant") return "繁體中文";
                return new CultureInfo(l).NativeName;
            }).ToArray();
        }

        internal static string L(string key)
        {
            if(!localizationAsset) Load();
            return localizationAsset.GetLocalizedString(key);
        }

        private static GUIContent G(string key) => G(key, null, "");
        private static GUIContent G(string[] key) => key.Length == 2 ? G(key[0], null, key[1]) : G(key[0], null, null);
        internal static GUIContent G(string key, string tooltip) => G(key, null, tooltip); // From EditorToolboxSettings
        private static GUIContent G(string key, Texture image) => G(key, image, "");
        internal static GUIContent G(SerializedProperty property) => G(property.name, $"{property.name}.tooltip");

        private static GUIContent G(string key, Texture image, string tooltip)
        {
            if(!localizationAsset) Load();
            if(guicontents.TryGetValue(key, out var content)) return content;
            return guicontents[key] = new GUIContent(L(key), image, L(tooltip));
        }

        internal static void SelectLanguageGUI()
        {
            var langs = GetLanguages();
            var names = GetLanguageNames();
            EditorGUI.BeginChangeCheck();
            var ind = EditorGUILayout.Popup("Language", Array.IndexOf(langs, language), names);
            if(EditorGUI.EndChangeCheck())
            {
                language = langs[ind];
                EditorPrefs.SetString(PREFERENCE_KEY, language);
                Load();
            }
        }
    }

}