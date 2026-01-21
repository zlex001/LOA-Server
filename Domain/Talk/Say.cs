using Logic;

namespace Domain.Talk
{
    public static class Say
    {
        public static void Do(Life speaker, Logic.Text.Labels textKey)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<Logic.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Domain.Text.Decorate.Life(speaker, player);
                    Domain.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{Domain.Text.Agent.Instance.Get(textKey, player)}" });
                }
            }
        }

        public static void Do(Life speaker, int textId)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<Logic.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Domain.Text.Decorate.Life(speaker, player);
                    Domain.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{Domain.Text.Agent.Instance.Get(textId, player)}" });
                }
            }
        }

        public static void Do(Life speaker, Logic.Text.Labels textKey, params (string key, object value)[] parameters)
        {
            if (speaker?.Map != null)
            {
                var players = speaker.Map.Content.Gets<Logic.Player>();
                foreach (var player in players)
                {
                    var decoratedSpeaker = Domain.Text.Decorate.Life(speaker, player);
                    var content = Domain.Text.Agent.Instance.Get(textKey, player, random: true);
                    
                    // 处理参数替换
                    if (parameters != null && parameters.Length > 0)
                    {
                        var placeholderList = new List<string>();
                        foreach (var (key, value) in parameters)
                        {
                            string resolved = value switch
                            {
                                Logic.Item item => Domain.Text.Decorate.Item(item, player),
                                Logic.Life life => Domain.Text.Decorate.Life(life, player),
                                _ => value?.ToString() ?? string.Empty
                            };
                            placeholderList.Add(key);
                            placeholderList.Add(resolved);
                        }
                        content = Utils.Text.Format(content, placeholderList.ToArray());
                    }
                    
                    Domain.Broadcast.Instance.System(player, new object[] { $"{decoratedSpeaker}：{content}" });
                }
            }
        }
    }
}
