using Data;
using Net;
using static global::Data.Text;

namespace Logic.Text
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {

        }

        public int Id(global::Data.Text.Labels label, bool random = false)
        {
            if (global::Data.Text.Instance.Label.TryGetValue(label, out var ids) && ids.Length > 0)
            {
                return random ? Utils.Random.GetElement(ids) : ids[0];
            }
            return 0;
        }

        public bool TryGet(int id, global::Data.Text.Languages lang, out string result)
        {
            if (global::Data.Text.Instance.Multilingual.TryGetValue(id, out var map) && map.TryGetValue(lang, out result))
                return true;

            result = string.Empty;
            return false;
        }

        public string Get(int id, global::Data.Text.Languages lang)
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

        public string Get(global::Data.Text.Labels label, global::Data.Text.Languages lang)
        {
            int id = Id(label, random: false);
            return Get(id, lang);
        }

        public string Get(global::Data.Text.Labels label, global::Data.Text.Languages lang, bool random)
        {
            int id = Id(label, random);
            return Get(id, lang);
        }

        public string Get(global::Data.Text.Labels label, Player player)
        {
            return Get(label, player.Language, false);
        }

        public string Get(global::Data.Text.Labels label, Player player, bool random)
        {
            return Get(label, player.Language, random);
        }

        public string Get(global::Data.Text.Labels label, Client client)
        {
            return Get(label, client.Language, false);
        }

        public string Get(global::Data.Text.Labels label, Client client, bool random)
        {
            return Get(label, client.Language, random);
        }

        public string Get<TEnum>(TEnum enumValue, global::Data.Text.Languages lang) where TEnum : Enum
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

        public string GetDynamic(global::Data.Text.Labels label, global::Data.Text.Languages lang, params (string key, string value)[] replacements)
        {
            string text = Get(label, lang);
            foreach (var (key, value) in replacements)
            {
                text = text.Replace($"{{{key}}}", value);
            }
            return text;
        }

        public string GetDynamic(global::Data.Text.Labels label, Player player, params (string key, string value)[] replacements)
        {
            return GetDynamic(label, player.Language, replacements);
        }

        public string GetDynamic(int id, global::Data.Text.Languages lang, params (string key, string value)[] replacements)
        {
            string text = Get(id, lang);
            foreach (var (key, value) in replacements)
            {
                text = text.Replace($"{{{key}}}", value);
            }
            return text;
        }
        
        public Dictionary<string, string> GetUITexts(global::Data.Text.Languages language)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in global::Data.Text.Instance.CidToId)
            {
                if (!pair.Key.Contains('.'))
                    continue;
                var text = Get(pair.Value, language);
                if (!string.IsNullOrEmpty(text))
                    result[pair.Key] = text;
            }
            return result;
        }

        private const string StartSettingsCidPrefix = "StartSettings.";

        /// <summary>
        /// Get settings panel UI texts for client DataManager.StartSettingsTexts (key: accounts, general, ...; value: localized string).
        /// </summary>
        public Dictionary<string, string> GetStartSettingsTexts(global::Data.Text.Languages language)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in global::Data.Text.Instance.CidToId)
            {
                if (pair.Key == null || !pair.Key.StartsWith(StartSettingsCidPrefix))
                    continue;
                var key = pair.Key.Substring(StartSettingsCidPrefix.Length);
                var text = Get(pair.Value, language);
                if (!string.IsNullOrEmpty(text))
                    result[key] = text;
            }
            return result;
        }

        private const string GatewayCidPrefix = "Gateway.";

        private static readonly (string key, global::Data.Text.Labels label)[] GatewayLabelMappings = new[]
        {
            ("title", global::Data.Text.Labels.StartTitle),
            ("tip", global::Data.Text.Labels.StartLogin),
            ("footer", global::Data.Text.Labels.StartFooter),
            ("accountIdPlaceholder", global::Data.Text.Labels.AccountIdPlaceholder),
            ("accountPasswordPlaceholder", global::Data.Text.Labels.AccountPasswordPlaceholder),
            ("accountNotePlaceholder", global::Data.Text.Labels.AccountNotePlaceholder),
            ("errorAccountEmpty", global::Data.Text.Labels.ErrorAccountEmpty),
            ("errorAccountFormat", global::Data.Text.Labels.ErrorAccountFormat),
            ("errorPasswordEmpty", global::Data.Text.Labels.ErrorPasswordEmpty),
            ("errorPasswordFormat", global::Data.Text.Labels.ErrorPasswordFormat),
            ("loginPasswordError", global::Data.Text.Labels.LoginPasswordError),
            ("loginAppVersionUnfit", global::Data.Text.Labels.LoginAppVersionUnfit),
            ("loginUnsafeAccount", global::Data.Text.Labels.LoginUnsafeAccount),
        };

        public Dictionary<string, string> GetGatewayTexts(global::Data.Text.Languages language)
        {
            var texts = new Dictionary<string, string>();

            foreach (var (key, label) in GatewayLabelMappings)
            {
                var text = Get(label, language);
                if (!string.IsNullOrEmpty(text))
                    texts[key] = text;
            }

            foreach (var pair in global::Data.Text.Instance.CidToId)
            {
                if (pair.Key == null) continue;

                if (pair.Key.StartsWith(StartSettingsCidPrefix))
                {
                    var key = pair.Key.Substring(StartSettingsCidPrefix.Length);
                    var text = Get(pair.Value, language);
                    if (!string.IsNullOrEmpty(text))
                        texts[key] = text;
                }
                else if (pair.Key.StartsWith(GatewayCidPrefix))
                {
                    var key = pair.Key.Substring(GatewayCidPrefix.Length);
                    var text = Get(pair.Value, language);
                    if (!string.IsNullOrEmpty(text))
                        texts[key] = text;
                }
            }

            return texts;
        }

        /// <summary>
        /// Get translation by cid (content id string)
        /// </summary>
        public string GetByCid(string cid, global::Data.Text.Languages lang)
        {
            if (string.IsNullOrEmpty(cid))
                return string.Empty;
                
            if (global::Data.Text.Instance.CidToId.TryGetValue(cid, out int id))
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
