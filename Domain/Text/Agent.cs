using Logic;
using Net;
using static Logic.Text;

namespace Domain.Text
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {

        }

        public int Id(Logic.Text.Labels label, bool random = false)
        {
            if (Logic.Text.Instance.Label.TryGetValue(label, out var ids) && ids.Length > 0)
            {
                return random ? Utils.Random.GetElement(ids) : ids[0];
            }
            return 0;
        }

        public bool TryGet(int id, Logic.Text.Languages lang, out string result)
        {
            if (Logic.Text.Instance.Multilingual.TryGetValue(id, out var map) && map.TryGetValue(lang, out result))
                return true;

            result = string.Empty;
            return false;
        }

        public string Get(int id, Logic.Text.Languages lang)
        {
            if (TryGet(id, lang, out var result))
                return result;

            if (TryGet(0, lang, out result))
                return result;

            if (TryGet(id, Languages.ChineseSimplified, out result))
                return result;

            return string.Empty;
        }

        public string Get(int id, Player player)
        {
            return Get(id, player.Language);
        }

        public string Get(int id, Client client)
        {
            return Get(id, client.Language);
        }

        public string Get(Logic.Text.Labels label, Logic.Text.Languages lang)
        {
            int id = Id(label, random: false);
            return Get(id, lang);
        }

        public string Get(Logic.Text.Labels label, Logic.Text.Languages lang, bool random)
        {
            int id = Id(label, random);
            return Get(id, lang);
        }

        public string Get(Logic.Text.Labels label, Player player)
        {
            return Get(label, player.Language, false);
        }

        public string Get(Logic.Text.Labels label, Player player, bool random)
        {
            return Get(label, player.Language, random);
        }

        public string Get(Logic.Text.Labels label, Client client)
        {
            return Get(label, client.Language, false);
        }

        public string Get(Logic.Text.Labels label, Client client, bool random)
        {
            return Get(label, client.Language, random);
        }

        public string Get<TEnum>(TEnum enumValue, Logic.Text.Languages lang) where TEnum : Enum
        {
            return Get(Convert.ToInt32(enumValue), lang);
        }

        public string Get<TEnum>(TEnum enumValue, Player player) where TEnum : Enum
        {
            return Get(Convert.ToInt32(enumValue), player.Language);
        }

        public string Get<TEnum>(TEnum enumValue, Client client) where TEnum : Enum
        {
            return Get(Convert.ToInt32(enumValue), client.Language);
        }

        public string GetDynamic(Logic.Text.Labels label, Logic.Text.Languages lang, params (string key, string value)[] replacements)
        {
            string text = Get(label, lang);
            foreach (var (key, value) in replacements)
            {
                text = text.Replace($"{{{key}}}", value);
            }
            return text;
        }

        public string GetDynamic(Logic.Text.Labels label, Player player, params (string key, string value)[] replacements)
        {
            return GetDynamic(label, player.Language, replacements);
        }

        public string GetDynamic(int id, Logic.Text.Languages lang, params (string key, string value)[] replacements)
        {
            string text = Get(id, lang);
            foreach (var (key, value) in replacements)
            {
                text = text.Replace($"{{{key}}}", value);
            }
            return text;
        }
        
        public Dictionary<string, string> GetUITexts(Logic.Text.Languages language)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in Logic.Text.Instance.CidToId)
            {
                if (!pair.Key.Contains('.'))
                    continue;
                var text = Get(pair.Value, language);
                if (!string.IsNullOrEmpty(text))
                    result[pair.Key] = text;
            }
            return result;
        }

        /// <summary>
        /// Get translation by cid (content id string)
        /// </summary>
        public string GetByCid(string cid, Logic.Text.Languages lang)
        {
            if (string.IsNullOrEmpty(cid))
                return string.Empty;
                
            if (Logic.Text.Instance.CidToId.TryGetValue(cid, out int id))
            {
                return Get(id, lang);
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Get translation by cid for a player
        /// </summary>
        public string GetByCid(string cid, Player player)
        {
            return GetByCid(cid, player.Language);
        }
    }
}
