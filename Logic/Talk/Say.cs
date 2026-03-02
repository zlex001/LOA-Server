using Data;

namespace Logic.Talk
{
    public static class Say
    {
        public static void Do(Life speaker, global::Data.Text.Labels textKey)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<global::Data.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Logic.Text.Decorate.Life(speaker, player);
                    Logic.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{Logic.Text.Agent.Instance.Get(textKey, player)}" });
                }
            }
        }

        public static void Do(Life speaker, int textId)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<global::Data.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Logic.Text.Decorate.Life(speaker, player);
                    Logic.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{Logic.Text.Agent.Instance.Get(textId, player)}" });
                }
            }
        }

        public static void Do(Life speaker, global::Data.Text.Labels textKey, params (string key, object value)[] parameters)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<global::Data.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Logic.Text.Decorate.Life(speaker, player);
                    var content = Logic.Text.Agent.Instance.Get(textKey, player, random: true);
                    
                    // 处理参数替换
                    if (parameters != null && parameters.Length > 0)
                    {
                        var placeholderList = new List<string>();
                        foreach (var (key, value) in parameters)
                        {
                            string resolved = value switch
                            {
                                global::Data.Item item => Logic.Text.Decorate.Item(item, player),
                                global::Data.Life life => Logic.Text.Decorate.Life(life, player),
                                _ => value?.ToString() ?? string.Empty
                            };
                            placeholderList.Add(key);
                            placeholderList.Add(resolved);
                        }
                        content = Utils.Text.Format(content, placeholderList.ToArray());
                    }
                    
                    Logic.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{content}" });
                }
            }
        }
    }
}
