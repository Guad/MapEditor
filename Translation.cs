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
            using (var stream = System.IO.File.OpenRead(path))
                _stringList = ((TranslationFile)serializer.Deserialize(stream)).Translations;
        }

        public static void Save(string path)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(TranslationFile));
            using (var stream = System.IO.File.OpenWrite(path))
                serializer.Serialize(stream, new TranslationFile() { Translations = new List<TranslatedString>(_stringList) });
        }

        public static string Translate(string original)
        {
            if (_stringList == null) return original;
            Language currentLanguage = Game.Language;
            if (_stringList.All(ts => ts.Original != original))
            {
                _stringList.Add(new TranslatedString()
                {
                    Original = original,
                    Translations = new List<TranslatedPair>()
                });
                Save("scripts\\MapEditor_Translation.xml");
                return original;
            }
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