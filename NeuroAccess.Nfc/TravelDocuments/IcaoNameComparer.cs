using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NeuroAccess.Nfc.TravelDocuments
{
    public static class IcaoNameComparer
    {
        private const int MaxVariants = 4096;
        private const char Separator = ' ';

        private sealed class VariantState
        {
            public VariantState(string text, bool atComponentStart, string lastEmission)
            {
                this.Text = text;
                this.AtComponentStart = atComponentStart;
                this.LastEmission = lastEmission;
            }

            public string Text { get; }
            public bool AtComponentStart { get; }
            public string LastEmission { get; }
        }

        private static readonly Dictionary<char, string[]> LatinMappings = CreateLatinMappings();
        private static readonly Dictionary<char, string[]> CyrillicMappings = CreateCyrillicMappings();
        private static readonly Dictionary<char, string[]> ArabicMappings = CreateArabicMappings();
        private static readonly HashSet<char> ArabicIgnoredChars = CreateArabicIgnoredChars();
        private static readonly HashSet<char> ArabicShaddaChars = new HashSet<char>() { '\u0651' };
        private static readonly HashSet<char> SeparatorChars = new HashSet<char>()
        {
            '<', '-', '\'', '’', '.', ',', '/', '\\', '_', '·'
        };

        internal static bool AreNamesSimilar(string s1, string s2)
        {
            if (s1 is null || s2 is null)
                return string.Compare(s1, s2, true) == 0;

            if (string.Compare(s1, s2, true) == 0)
                return true;

            HashSet<string> v1 = GenerateVariants(s1);
            HashSet<string> v2 = GenerateVariants(s2);

            if (Intersects(v1, v2))
                return true;

            HashSet<string> c1 = RemoveSeparators(v1);
            HashSet<string> c2 = RemoveSeparators(v2);

            return Intersects(c1, c2);
        }

        private static bool Intersects(HashSet<string> s1, HashSet<string> s2)
        {
            if (s1.Count > s2.Count)
            {
                HashSet<string> t = s1;
                s1 = s2;
                s2 = t;
            }

            foreach (string s in s1)
            {
                if (s2.Contains(s))
                    return true;
            }

            return false;
        }

        private static HashSet<string> RemoveSeparators(HashSet<string> source)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);

            foreach (string s in source)
            {
                if (s.IndexOf(Separator) < 0)
                {
                    result.Add(s);
                    continue;
                }

                StringBuilder sb = new StringBuilder(s.Length);
                foreach (char ch in s)
                {
                    if (ch != Separator)
                        sb.Append(ch);
                }

                result.Add(sb.ToString());
            }

            return result;
        }

        private static HashSet<string> GenerateVariants(string input)
        {
            input = input.Normalize(NormalizationForm.FormC);

            List<VariantState> states = new List<VariantState>()
            {
                new VariantState(string.Empty, true, null)
            };

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                List<VariantState> next = new List<VariantState>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (VariantState state in states)
                    ApplyChar(input, i, ch, state, next, seen);

                states = next;
                if (states.Count == 0)
                    break;
            }

            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            foreach (VariantState state in states)
            {
                string normalized = TrimAndCollapseSeparators(state.Text);
                if (!string.IsNullOrEmpty(normalized))
                    result.Add(normalized);
            }

            if (result.Count == 0)
                result.Add(string.Empty);

            return result;
        }

        private static void ApplyChar(string input, int index, char ch, VariantState state, List<VariantState> next, HashSet<string> seen)
        {
            if (ArabicShaddaChars.Contains(ch))
            {
                if (!string.IsNullOrEmpty(state.LastEmission))
                    AddState(next, seen, state.Text + state.LastEmission, false, state.LastEmission);
                else
                    AddState(next, seen, state.Text, state.AtComponentStart, state.LastEmission);
                return;
            }

            if (IsIgnorableUnicode(ch))
            {
                AddState(next, seen, state.Text, state.AtComponentStart, state.LastEmission);
                return;
            }

            if (IsSeparator(ch))
            {
                string text = AppendSeparator(state.Text);
                AddState(next, seen, text, true, null);
                return;
            }

            if (ArabicIgnoredChars.Contains(ch))
            {
                AddState(next, seen, state.Text, state.AtComponentStart, state.LastEmission);
                return;
            }

            string[] mapped = GetMappedValues(ch, state.AtComponentStart, IsArabicTehMarbutaAtEndOfComponent(input, index, ch));
            if (!(mapped is null))
            {
                foreach (string value in mapped)
                    AddState(next, seen, state.Text + value, false, value);
                return;
            }

            if (TryGetAsciiFallback(ch, out string fallback))
            {
                AddState(next, seen, state.Text + fallback, false, fallback);
                return;
            }

            if (char.IsLetterOrDigit(ch))
            {
                string upper = ch.ToString().ToUpperInvariant();
                AddState(next, seen, state.Text + upper, false, upper);
                return;
            }

            AddState(next, seen, state.Text, state.AtComponentStart, state.LastEmission);
        }

        private static bool TryGetAsciiFallback(char ch, out string fallback)
        {
            fallback = null;

            if (char.IsLetterOrDigit(ch))
            {
                string s = ch.ToString();
                string stripped = ShuftiProClient.RemoveDiacritics(s).Normalize(NormalizationForm.FormC).ToUpperInvariant();
                if (!string.IsNullOrEmpty(stripped) && IsAsciiLettersDigitsOrX(stripped))
                {
                    fallback = stripped;
                    return true;
                }

                string upper = s.ToUpperInvariant();
                if (IsAsciiLettersDigitsOrX(upper))
                {
                    fallback = upper;
                    return true;
                }
            }

            return false;
        }

        private static bool IsAsciiLettersDigitsOrX(string s)
        {
            foreach (char ch in s)
            {
                if (!(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9'))
                    return false;
            }

            return true;
        }

        private static string[] GetMappedValues(char ch, bool atComponentStart, bool isArabicTehMarbutaEnd)
        {
            if (ch == '\u0629')
                return isArabicTehMarbutaEnd ? new string[] { "XAH" } : new string[] { "XTA" };

            if (ArabicMappings.TryGetValue(ch, out string[] arabic))
                return arabic;

            if (LatinMappings.TryGetValue(ch, out string[] latin))
                return latin;

            char upper = char.ToUpperInvariant(ch);

            if (upper == '\u0404')
                return atComponentStart ? new string[] { "IE", "YE" } : new string[] { "IE" };
            if (upper == '\u0407')
                return atComponentStart ? new string[] { "I", "YI" } : new string[] { "I" };
            if (upper == '\u0419')
                return atComponentStart ? new string[] { "I", "Y" } : new string[] { "I" };
            if (upper == '\u042E')
                return atComponentStart ? new string[] { "IU", "YU" } : new string[] { "IU" };
            if (upper == '\u042F')
                return atComponentStart ? new string[] { "IA", "YA" } : new string[] { "IA" };

            if (CyrillicMappings.TryGetValue(upper, out string[] cyr))
                return cyr;

            return null;
        }

        private static bool IsArabicTehMarbutaAtEndOfComponent(string input, int index, char ch)
        {
            if (ch != '\u0629')
                return false;

            for (int i = index + 1; i < input.Length; i++)
            {
                char c = input[i];

                if (IsIgnorableUnicode(c) || ArabicIgnoredChars.Contains(c) || ArabicShaddaChars.Contains(c))
                    continue;

                if (IsSeparator(c))
                    return true;

                return false;
            }

            return true;
        }

        private static bool IsIgnorableUnicode(char ch)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
            return category == UnicodeCategory.NonSpacingMark
                || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark
                || category == UnicodeCategory.Format;
        }

        private static bool IsSeparator(char ch)
        {
            return char.IsWhiteSpace(ch) || SeparatorChars.Contains(ch);
        }

        private static string AppendSeparator(string text)
        {
            if (string.IsNullOrEmpty(text) || text[text.Length - 1] == Separator)
                return text;

            return text + Separator;
        }

        private static string TrimAndCollapseSeparators(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            StringBuilder sb = new StringBuilder(text.Length);
            bool lastWasSeparator = true;

            foreach (char ch in text)
            {
                if (ch == Separator)
                {
                    if (!lastWasSeparator)
                    {
                        sb.Append(Separator);
                        lastWasSeparator = true;
                    }
                }
                else
                {
                    sb.Append(ch);
                    lastWasSeparator = false;
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == Separator)
                sb.Length--;

            return sb.ToString();
        }

        private static void AddState(List<VariantState> next, HashSet<string> seen, string text, bool atComponentStart, string lastEmission)
        {
            string key = text + "\u001F" + (atComponentStart ? "1" : "0") + "\u001F" + (lastEmission ?? string.Empty);
            if (!seen.Add(key))
                return;

            if (next.Count >= MaxVariants)
                return;

            next.Add(new VariantState(text, atComponentStart, lastEmission));
        }

        private static Dictionary<char, string[]> CreateLatinMappings()
        {
            Dictionary<char, string[]> d = new Dictionary<char, string[]>();

            void Add(char ch, params string[] values)
            {
                d[ch] = values;
                char lower = char.ToLowerInvariant(ch);
                d[lower] = values;
            }

            Add('\u00C0', "A");
            Add('\u00C1', "A");
            Add('\u00C2', "A");
            Add('\u00C3', "A");
            Add('\u00C4', "AE", "A");
            Add('\u00C5', "AA", "A");
            Add('\u00C6', "AE");
            Add('\u00C7', "C");
            Add('\u00C8', "E");
            Add('\u00C9', "E");
            Add('\u00CA', "E");
            Add('\u00CB', "E");
            Add('\u00CC', "I");
            Add('\u00CD', "I");
            Add('\u00CE', "I");
            Add('\u00CF', "I");
            Add('\u00D0', "D");
            Add('\u00D1', "N", "NXX");
            Add('\u00D2', "O");
            Add('\u00D3', "O");
            Add('\u00D4', "O");
            Add('\u00D5', "O");
            Add('\u00D6', "OE", "O");
            Add('\u00D8', "OE");
            Add('\u00D9', "U");
            Add('\u00DA', "U");
            Add('\u00DB', "U");
            Add('\u00DC', "UE", "UXX", "U");
            Add('\u00DD', "Y");
            Add('\u00DE', "TH");
            Add('\u0100', "A");
            Add('\u0102', "A");
            Add('\u0104', "A");
            Add('\u0106', "C");
            Add('\u0108', "C");
            Add('\u010A', "C");
            Add('\u010C', "C");
            Add('\u010E', "D");
            Add('\u0110', "D");
            Add('\u0112', "E");
            Add('\u0114', "E");
            Add('\u0116', "E");
            Add('\u0118', "E");
            Add('\u011A', "E");
            Add('\u011C', "G");
            Add('\u011E', "G");
            Add('\u0120', "G");
            Add('\u0122', "G");
            Add('\u0124', "H");
            Add('\u0126', "H");
            Add('\u0128', "I");
            Add('\u012A', "I");
            Add('\u012C', "I");
            Add('\u012E', "I");
            Add('\u0130', "I");
            Add('\u0131', "I");
            Add('\u0132', "IJ");
            Add('\u0134', "J");
            Add('\u0136', "K");
            Add('\u0139', "L");
            Add('\u013B', "L");
            Add('\u013D', "L");
            Add('\u013F', "L");
            Add('\u0141', "L");
            Add('\u0143', "N");
            Add('\u0145', "N");
            Add('\u0147', "N");
            Add('\u014A', "N");
            Add('\u014C', "O");
            Add('\u014E', "O");
            Add('\u0150', "O");
            Add('\u0152', "OE");
            Add('\u0154', "R");
            Add('\u0156', "R");
            Add('\u0158', "R");
            Add('\u015A', "S");
            Add('\u015C', "S");
            Add('\u015E', "S");
            Add('\u0160', "S");
            Add('\u0162', "T");
            Add('\u0164', "T");
            Add('\u0166', "T");
            Add('\u0168', "U");
            Add('\u016A', "U");
            Add('\u016C', "U");
            Add('\u016E', "U");
            Add('\u0170', "U");
            Add('\u0172', "U");
            Add('\u0174', "W");
            Add('\u0176', "Y");
            Add('\u0178', "Y");
            Add('\u0179', "Z");
            Add('\u017B', "Z");
            Add('\u017D', "Z");
            Add('\u1E9E', "SS");
            d['\u00DF'] = new string[] { "SS" };

            return d;
        }

        private static Dictionary<char, string[]> CreateCyrillicMappings()
        {
            Dictionary<char, string[]> d = new Dictionary<char, string[]>();

            void Add(char ch, params string[] values)
            {
                d[ch] = values;
                char lower = char.ToLowerInvariant(ch);
                d[lower] = values;
            }

            Add('\u0401', "E", "IO");
            Add('\u0402', "D", "DJ");
            Add('\u040B', "D");
            Add('\u0405', "DZ");
            Add('\u0406', "I");
            Add('\u0408', "J");
            Add('\u0409', "LJ");
            Add('\u040A', "NJ");
            Add('\u040C', "K", "KJ");
            Add('\u040E', "U");
            Add('\u040F', "DZ", "DJ");
            Add('\u0410', "A");
            Add('\u0411', "B");
            Add('\u0412', "V");
            Add('\u0413', "G", "H");
            Add('\u0414', "D");
            Add('\u0415', "E");
            Add('\u0416', "ZH", "Z");
            Add('\u0417', "Z");
            Add('\u0418', "I", "Y");
            Add('\u041A', "K");
            Add('\u041B', "L");
            Add('\u041C', "M");
            Add('\u041D', "N");
            Add('\u041E', "O");
            Add('\u041F', "P");
            Add('\u0420', "R");
            Add('\u0421', "S");
            Add('\u0422', "T");
            Add('\u0423', "U");
            Add('\u0424', "F");
            Add('\u0425', "KH", "H");
            Add('\u0426', "TS", "C");
            Add('\u0427', "CH", "C");
            Add('\u0428', "SH", "S");
            Add('\u0429', "SHCH", "SHT");
            Add('\u042A', "IE");
            Add('\u042B', "Y");
            Add('\u042D', "E");
            Add('\u046A', "U");
            Add('\u0474', "Y");
            Add('\u0490', "G");
            Add('\u0492', "G", "GJ");
            Add('\u04BA', "C");

            return d;
        }

        private static Dictionary<char, string[]> CreateArabicMappings()
        {
            return new Dictionary<char, string[]>()
            {
                ['\u0621'] = new string[] { "XE" },
                ['\u0622'] = new string[] { "XAA" },
                ['\u0623'] = new string[] { "XAE" },
                ['\u0624'] = new string[] { "U" },
                ['\u0625'] = new string[] { "I" },
                ['\u0626'] = new string[] { "XI" },
                ['\u0627'] = new string[] { "A" },
                ['\u0628'] = new string[] { "B" },
                ['\u062A'] = new string[] { "T" },
                ['\u062B'] = new string[] { "XTH" },
                ['\u062C'] = new string[] { "J" },
                ['\u062D'] = new string[] { "XH" },
                ['\u062E'] = new string[] { "XKH" },
                ['\u062F'] = new string[] { "D" },
                ['\u0630'] = new string[] { "XDH" },
                ['\u0631'] = new string[] { "R" },
                ['\u0632'] = new string[] { "Z" },
                ['\u0633'] = new string[] { "S" },
                ['\u0634'] = new string[] { "XSH" },
                ['\u0635'] = new string[] { "XSS" },
                ['\u0636'] = new string[] { "XDZ" },
                ['\u0637'] = new string[] { "XTT" },
                ['\u0638'] = new string[] { "XZZ" },
                ['\u0639'] = new string[] { "E" },
                ['\u063A'] = new string[] { "G" },
                ['\u0641'] = new string[] { "F" },
                ['\u0642'] = new string[] { "Q" },
                ['\u0643'] = new string[] { "K" },
                ['\u0644'] = new string[] { "L" },
                ['\u0645'] = new string[] { "M" },
                ['\u0646'] = new string[] { "N" },
                ['\u0647'] = new string[] { "H" },
                ['\u0648'] = new string[] { "W" },
                ['\u0649'] = new string[] { "XAY" },
                ['\u064A'] = new string[] { "Y" },
                ['\u0671'] = new string[] { "XXA" },
                ['\u0679'] = new string[] { "XXT" },
                ['\u067C'] = new string[] { "XRT" },
                ['\u067E'] = new string[] { "P" },
                ['\u0681'] = new string[] { "XKE" },
                ['\u0685'] = new string[] { "XXH" },
                ['\u0686'] = new string[] { "XC" },
                ['\u0688'] = new string[] { "XXD" },
                ['\u0689'] = new string[] { "XDR" },
                ['\u0691'] = new string[] { "XXR" },
                ['\u0693'] = new string[] { "XRR" },
                ['\u0696'] = new string[] { "XRX" },
                ['\u0698'] = new string[] { "XJ" },
                ['\u069A'] = new string[] { "XXS" },
                ['\u06A9'] = new string[] { "XKK" },
                ['\u06AB'] = new string[] { "XXK" },
                ['\u06AD'] = new string[] { "XNG" },
                ['\u06AF'] = new string[] { "XGG" },
                ['\u06BA'] = new string[] { "XNN" },
                ['\u06BC'] = new string[] { "XXN" },
                ['\u06BE'] = new string[] { "XDO" },
                ['\u06C0'] = new string[] { "XYH" },
                ['\u06C1'] = new string[] { "XXG" },
                ['\u06C2'] = new string[] { "XGE" },
                ['\u06C3'] = new string[] { "XTG" },
                ['\u06CC'] = new string[] { "XYA" },
                ['\u06CD'] = new string[] { "XXY" },
                ['\u06D0'] = new string[] { "Y" },
                ['\u06D2'] = new string[] { "XYB" },
                ['\u06D3'] = new string[] { "XBE" }
            };
        }

        private static HashSet<char> CreateArabicIgnoredChars()
        {
            return new HashSet<char>()
            {
                '\u0640',
                '\u064B',
                '\u064C',
                '\u064D',
                '\u064E',
                '\u064F',
                '\u0650',
                '\u0652',
                '\u0670',
                '\u069C',
                '\u06A2',
                '\u06A7',
                '\u06A8'
            };
        }
    }
}
