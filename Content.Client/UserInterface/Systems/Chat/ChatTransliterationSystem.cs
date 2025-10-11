using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Client.UserInterface.Systems.Chat
{
    public static class ChatTransliterationSystem
    {
        private static readonly Dictionary<string, string> RuToEn = new Dictionary<string, string>(){ //code quality people, you can сосать мой огромный хуй))))
            {"а","a"},
            {"б","b"},
            {"в","v"},
            {"г","g"},
            {"д","d"},
            {"е","ye"},
            {"ё","yo"},
            {"ж","zh"},
            {"з","z"},
            {"и","i"},
            {"й","y"},
            {"к","k"},
            {"л","l"},
            {"м","m"},
            {"н","n"},
            {"о","o"},
            {"п","p"},
            {"р","r"},
            {"с","s"},
            {"т","t"},
            {"у","u"},
            {"ф","f"},
            {"х","kh"},
            {"ц","c"},
            {"ч","ch"},
            {"ш","sh"},
            {"щ","shch"},
            {"ъ","''"},
            {"ы","iy"},
            {"ь","'"},
            {"э","e"},
            {"ю","yu"},
            {"я","ya"}
        };
        private static readonly Dictionary<string, string> EnToRu = new Dictionary<string, string>(){
            {"ye","е"}, //first in the foreach loop are the complex two letter transliterations
            {"yo","ё"},
            {"zh","ж"},
            {"''","ъ"},
            {"kh","х"},
            {"shch","щ"},
            {"ch","ч"},
            {"sh","ш"},
            {"yu","ю"},
            {"ya","я"},
            {"'","ь"},
            {"a","а"},
            {"b","б"},
            {"c","ц"},
            {"d","д"},
            {"e","е"},
            {"f","ф"},
            {"g","г"},
            {"h","г"},
            {"i","и"},
            {"j","й"},
            {"k","к"},
            {"l","л"},
            {"m","м"},
            {"n","н"},
            {"o","о"},
            {"q","ку"},
            {"p","п"},
            {"r","р"},
            {"s","с"},
            {"t","т"},
            {"u","у"},
            {"v","в"},
            {"w","в"},
            {"x","кс"},
            {"y","ы"},
            {"z","з"},
        };
        private static string Transliterate(string message, Dictionary<string, string> dictionary)
        {
            foreach (var (key, value) in dictionary)
            {
                var pattern = Regex.Escape(key);
                message = Regex.Replace(message, pattern, match =>
                {
                    var replacement = value;

                    if (!match.Value.Any(char.IsLower) && (match.Length > 1 || replacement.Length == 1))
                    {
                        replacement = replacement.ToUpperInvariant();
                    }
                    else if (match.Length >= 1 && replacement.Length >= 1 && char.IsUpper(match.Value[0]))
                    {
                        replacement = replacement[0].ToString().ToUpper() + replacement[1..];
                    }

                    return replacement;
                }, RegexOptions.IgnoreCase);
            }

            return message;
        }

        public static string TransliterateRussianToEnglish(string message)
        {
            return Transliterate(message, RuToEn);
        }

        public static string TransliterateEnglishToRussian(string message)
        {
            return Transliterate(message, EnToRu);
        }
    }
}
