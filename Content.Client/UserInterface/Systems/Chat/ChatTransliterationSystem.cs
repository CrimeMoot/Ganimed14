using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Client.UserInterface.Systems.Chat
{
    public class ChatTransliterationSystem
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
        private static readonly Dictionary<string, string> EnToRu = new Dictionary<string, string>(){ //thankfully even for letters like the standard a the russian alphabet has its own utc anal)))ogs which save us from an infinite recursive loop, praise prigozhin inshallah
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
        public static string TransliterateRussianToEnglish(string message)
        {
            foreach (KeyValuePair<string, string> ele in RuToEn)
            {
                var RussianLetter = ele.Key;
                var EnglishLetter = ele.Value;
                // Using the same system as ReplacementAccentSystem.cs, performance hit irrelevant due to this running clientside.
                var maskMessage = message;
                // RT wont let me use regex.count because MUH SANDBOX VIOLATION, so i instead use the even MORE malware-capable regex.matches :)
                for (int i = Regex.Matches(message, $"{RussianLetter}", RegexOptions.IgnoreCase).Count; i > 0; i--)
                {
                    // fetch the match again as the character indices may have changed
                    Match match = Regex.Match(maskMessage, $"{RussianLetter}", RegexOptions.IgnoreCase);
                    var TempReplacement = EnglishLetter;

                    // Intelligently replace capitalization
                    // two cases where we will do so:
                    // - the string is all upper case (just uppercase the replacement too)
                    // - the first letter of the word is capitalized (common, just uppercase the first letter too)
                    // any other cases are not really useful or not viable, since the match & replacement can be different
                    // lengths

                    // second expression here is weird--its specifically for single-word capitalization for I or A
                    // dwarf expands I -> Ah, without that it would transform I -> AH
                    // so that second case will only fully-uppercase if the replacement length is also 1
                    if (!match.Value.Any(char.IsLower) && (match.Length > 1 || TempReplacement.Length == 1))
                    {
                        TempReplacement = TempReplacement.ToUpperInvariant();
                    }
                    else if (match.Length >= 1 && TempReplacement.Length >= 1 && char.IsUpper(match.Value[0]))
                    {
                        TempReplacement = TempReplacement[0].ToString().ToUpper() + TempReplacement[1..];
                    }

                    // In-place replace the match with the transformed capitalization replacement
                    message = message.Remove(match.Index, match.Length).Insert(match.Index, TempReplacement);
                    var mask = new string('_', TempReplacement.Length);
                    maskMessage = maskMessage.Remove(match.Index, match.Length).Insert(match.Index, mask);
                }
            }

            return message;
        }
        public static string TransliterateEnglishToRussian(string message)
        {
            foreach (KeyValuePair<string, string> ele in EnToRu)
            {
                var EnglishLetter = ele.Key;
                var RussianLetter = ele.Value;
                // Using the same system as ReplacementAccentSystem.cs, performance hit irrelevant due to this running clientside.
                var maskMessage = message;
                // RT wont let me use regex.count because MUH SANDBOX VIOLATION, so i instead use the even MORE malware-capable regex.matches :)
                for (int i = Regex.Matches(message, $"{EnglishLetter}", RegexOptions.IgnoreCase).Count; i > 0; i--)
                {
                    // fetch the match again as the character indices may have changed
                    Match match = Regex.Match(maskMessage, $"{EnglishLetter}", RegexOptions.IgnoreCase);
                    var TempReplacement = RussianLetter;

                    // Intelligently replace capitalization
                    // two cases where we will do so:
                    // - the string is all upper case (just uppercase the replacement too)
                    // - the first letter of the word is capitalized (common, just uppercase the first letter too)
                    // any other cases are not really useful or not viable, since the match & replacement can be different
                    // lengths

                    // second expression here is weird--its specifically for single-word capitalization for I or A
                    // dwarf expands I -> Ah, without that it would transform I -> AH
                    // so that second case will only fully-uppercase if the replacement length is also 1
                    if (!match.Value.Any(char.IsLower) && (match.Length > 1 || TempReplacement.Length == 1))
                    {
                        TempReplacement = TempReplacement.ToUpperInvariant();
                    }
                    else if (match.Length >= 1 && TempReplacement.Length >= 1 && char.IsUpper(match.Value[0]))
                    {
                        TempReplacement = TempReplacement[0].ToString().ToUpper() + TempReplacement[1..];
                    }

                    // In-place replace the match with the transformed capitalization replacement
                    message = message.Remove(match.Index, match.Length).Insert(match.Index, TempReplacement);
                    var mask = new string('_', TempReplacement.Length);
                    maskMessage = maskMessage.Remove(match.Index, match.Length).Insert(match.Index, mask);
                }
            }
            return message;
        }
    }
}
