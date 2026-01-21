using System;
using System.Collections.Generic;

namespace Utils
{
    public static class Tag
    {
        // =========================
        // �������ߺ���
        // =========================
        private static bool StartsWithPrefix(string tag, string prefix, string sep = ":")
        {
            int plen = prefix.Length;
            return tag.Length > plen && tag.StartsWith(prefix) && tag[plen] == sep[0];
        }

        private static string ExtractValue(string tag, string prefix)
        {
            int plen = prefix.Length + 1; // ���� ":"
            return tag.Length > plen ? tag.Substring(plen) : string.Empty;
        }

        // =========================
        // ��ѯ����
        // =========================
        public static bool HasPrefix(this IReadOnlyList<string> tags, string prefix)
        {
            if (tags == null || tags.Count == 0) return false;
            for (int i = 0; i < tags.Count; i++)
            {
                if (StartsWithPrefix(tags[i], prefix)) return true;
            }
            return false;
        }

        public static bool Has(this IReadOnlyList<string> tags, string tag)
        {
            if (tags == null || tags.Count == 0) return false;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == tag) return true;
            }
            return false;
        }

        // =========================
        // ȡֵ����
        // =========================
        public static string GetValue(this IReadOnlyList<string> tags, string prefix)
        {
            if (tags == null || tags.Count == 0) return null;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (StartsWithPrefix(tag, prefix))
                    return ExtractValue(tag, prefix);
            }
            return null;
        }

        public static List<string> GetValues(this IReadOnlyList<string> tags, string prefix)
        {
            List<string> result = new List<string>();
            if (tags == null || tags.Count == 0) return result;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (StartsWithPrefix(tag, prefix))
                {
                    result.Add(ExtractValue(tag, prefix));
                }
            }
            return result;
        }

        // =========================
        // ����/���ຯ��
        // =========================
        public static List<string> GetIndividuals(this IReadOnlyList<string> tags)
        {
            List<string> result = new List<string>();
            if (tags == null || tags.Count == 0) return result;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!tag.Contains(":"))
                    result.Add(tag);
            }
            return result;
        }

        public static List<string> GetComposites(this IReadOnlyList<string> tags, string prefix = null)
        {
            List<string> result = new List<string>();
            if (tags == null || tags.Count == 0) return result;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (tag.Contains(":"))
                {
                    if (prefix == null || StartsWithPrefix(tag, prefix))
                        result.Add(tag);
                }
            }
            return result;
        }

        // =========================
        // ����ṹ����
        // =========================
        public static Dictionary<string, int> ParseSlotTags(this IReadOnlyList<string> tags)
        {
            var slots = new Dictionary<string, int>();
            if (tags == null || tags.Count == 0) return slots;

            const string prefix = "Slot";
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!StartsWithPrefix(tag, prefix)) continue;

                string value = ExtractValue(tag, prefix);
                int starIndex = value.IndexOf('*');
                if (starIndex > 0 && starIndex < value.Length - 1)
                {
                    string name = value.Substring(0, starIndex);
                    string countStr = value.Substring(starIndex + 1);
                    if (int.TryParse(countStr, out int count))
                        slots[name] = count;
                }
            }
            return slots;
        }

        // =========================
        // �ַ���ת��ǩ����
        // =========================
        public static HashSet<string> ParseTagsFromString(string tagsString)
        {
            var result = new HashSet<string>();
            if (string.IsNullOrEmpty(tagsString)) return result;

            var parts = tagsString.Split(';');
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                    result.Add(trimmed);
            }

            return result;
        }

        private static bool IsTagSeparator(string tagsString, int commaIndex)
        {
            int nextNonSpace = commaIndex + 1;
            while (nextNonSpace < tagsString.Length && tagsString[nextNonSpace] == ' ')
            {
                nextNonSpace++;
            }

            if (nextNonSpace >= tagsString.Length) return true;

            int colonIndex = tagsString.IndexOf(':', nextNonSpace);
            if (colonIndex == -1) return true;

            int nextCommaIndex = tagsString.IndexOf(',', nextNonSpace);
            if (nextCommaIndex != -1 && nextCommaIndex < colonIndex) return true;

            string possibleTagName = tagsString.Substring(nextNonSpace, colonIndex - nextNonSpace);
            return IsEnglishLettersOnly(possibleTagName);
        }

        private static bool IsEnglishLettersOnly(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            foreach (char c in str)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                    return false;
            }
            return true;
        }

        // =========================
        // ����ǩ��ȡ���ⲿ��ֱ�ӵ��ã�
        // =========================
        public static string Extract(this string tag)
        {
            if (string.IsNullOrEmpty(tag)) return tag;
            int idx = tag.IndexOf(':');
            return idx >= 0 && idx < tag.Length - 1 ? tag.Substring(idx + 1) : tag;
        }
    }
}
