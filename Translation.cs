using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using GTA;

namespace MapEditor
{
    public static class Translation
    {
        private static List<TranslatedString> _stringList;

        public static void Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TranslationFile));
            _stringList = ((TranslationFile)serializer.Deserialize(System.IO.File.OpenRead(path))).Translations;
        }

        public static void Test()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(TranslationFile));
            TranslationFile f = new TranslationFile();
            f.Translations = new List<TranslatedString>
            {
                new TranslatedString() { Original = "Enter/Exit Map Editor", Translations = new List<TranslatedPair>
                {
                    new TranslatedPair() {Language = Language.Spanish, String = "Enter/Exit Map Editor"},
                    new TranslatedPair() {Language = Language.American, String = "Salir/Entrar del Editor"},
                }},
                new TranslatedString() { Original = "New Map", Translations = new List<TranslatedPair>
                {
                    new TranslatedPair() {Language = Language.Spanish, String = "New Map"},
                    new TranslatedPair() {Language = Language.American, String = "Mapa Nuevo"},
                }},

            };
            serializer.Serialize(System.IO.File.OpenWrite(@"C:\Users\Guad\Desktop\ts.xml"), f);
        }

        public static string Translate(string original)
        {
            if (_stringList == null) return original;
            Language currentLanguage = Game.Language;
            if (_stringList.All(ts => ts.Original != original)) return original;
            var ourTs = _stringList.First(ts => ts.Original == original).Translations;
            return ourTs.FirstOrDefault(pair => pair.Language == currentLanguage)?.String.Replace("~n~", "\n") ?? original;
        }
    }

    public class TranslationFile
    {
        public List<TranslatedString> Translations { get; set; }
    }

    public class TranslatedString
    {
        public string Original { get; set; }
        public List<TranslatedPair> Translations { get; set; }
    }

    public class TranslatedPair
    {
        public Language Language { get; set; }
        public string String { get; set; }
    }
}