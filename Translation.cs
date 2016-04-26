using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GTA;

namespace MapEditor
{
    public static class Translation
    {
        public static List<TranslationRoot> Translations;
        public static string CurrentTranslation { get; set; }

        private static TranslationRoot _currenTranslationFile;
        private static List<StringPair> _stringList;

        public static void Load(string folder, string translation)
        {
            Translations = new List<TranslationRoot>();
            XmlSerializer serializer = new XmlSerializer(typeof (TranslationRoot));

            foreach (var path in Directory.GetFiles(folder, "*.xml"))
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(path))
                    {
                        var trans = (TranslationRoot) serializer.Deserialize(stream);
                        if (trans == null) throw new NullReferenceException();
                        trans.SetPath(path);
                        Translations.Add(trans);
                    }
                }
                catch (Exception) { }
            }

            SetLanguage(translation);
        }

        public static void SetLanguage(string newLanguage)
        {
            if (newLanguage == "Auto")
            {
                CurrentTranslation = Game.Language.ToString();
                _currenTranslationFile = Translations.FirstOrDefault(t => t.Language == newLanguage);
            }
            else
            {
                CurrentTranslation = newLanguage;
                _currenTranslationFile = Translations.FirstOrDefault(t => t.Language == newLanguage);
            }
        }

        public static void Save()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof (TranslationRoot));
            foreach (var trans in Translations)
            {
                using (var stream = File.OpenWrite(trans.GetPath()))
                    serializer.Serialize(stream, trans);
            }
        }

        public static string Translate(string original)
        {
            if (_currenTranslationFile == null) return original;
            
            if (_currenTranslationFile.Translations.All(ts => ts.Original != original))
            {
                _currenTranslationFile.Translations.Add(new StringPair()
                {
                    Original = original,
                    Translation = original,
                });
                Save();
                return original;
            }

            return _currenTranslationFile.Translations.First(ts => ts.Original == original).Translation.Replace("~n~", "\n");
        }
    }

    public class TranslationRoot
    {
        private string _path;

        internal void SetPath(string path)
        {
            _path = path;
        }

        internal string GetPath()
        {
            return _path;
        }

        public string Language { get; set; }
        public string Translator { get; set; }

        public List<StringPair> Translations { get; set; }
    }

    public class StringPair
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
}