using Logic;
using System.Linq;
using System.Text;
using Utils;

namespace Domain
{
    public class Broadcast
    {
        public class ColoredText
        {
            public int TextId { get; }
            public Utils.Text.Colors Color { get; }

            public ColoredText(int textId, Utils.Text.Colors color)
            {
                TextId = textId;
                Color = color;
            }
        }

        private static Broadcast instance;
        public static Broadcast Instance { get { if (instance == null) { instance = new Broadcast(); } return instance; } }
        
        private static string DecorateConfigItem(Logic.Config.Item configItem, Logic.Player player)
        {
            if (configItem == null || player == null) return "";
            
            var color = Domain.Text.Decorate.GetItemValueColor(configItem.value);
            var name = Domain.Text.Agent.Instance.Get(configItem.Name, player);
            
            return Utils.Text.Color(color, $"【{name}】");
        }
        private void Do(Player player, Logic.Channel channel, object[] segments, params (string, object)[] placeholders)
        {
            var sb = new StringBuilder();

            foreach (object segment in segments)
            {
                string text = segment switch
                {
                    ColoredText colored => Utils.Text.Color(colored.Color, Domain.Text.Agent.Instance.Get(colored.TextId, player)),
                    int id => Domain.Text.Agent.Instance.Get(id, player),
                    string raw => raw,
                    _ => segment?.ToString() ?? string.Empty
                };



                sb.Append(text);
            }
            string merged = sb.ToString();
            if (placeholders != null && placeholders.Length > 0)
            {
                var list = new List<string>();
                var placeholderDict = placeholders.ToDictionary(p => p.Item1, p => p.Item2);
                
                foreach (var (key, value) in placeholders)
                {
                    string resolved = value switch
                    {
                        Item item when key == "item" && placeholderDict.ContainsKey("count") 
                            => Domain.Text.Decorate.Item(item, player, 1),
                        Item item when key == "item" 
                            => Domain.Text.Decorate.Item(item, player),
                        Item item 
                            => Domain.Text.Decorate.Item(item, player, 1),
                        Logic.Config.Item configItem => DecorateConfigItem(configItem, player),
                        Logic.Skill skill => Domain.Text.Decorate.Skill(skill, player),
                        Logic.Movement movement => Domain.Text.Decorate.Movement(movement, player),
                        Part part => Domain.Text.Decorate.Part(part, player),
                        Life life => Domain.Text.Decorate.Life(life, player),
                        Logic.Map map => Domain.Text.Decorate.Map(map, player),
                        int id => Domain.Text.Agent.Instance.Get(id, player),
                        Logic.Text.Raw raw => raw.Value,
                        string str => str,
                        _ => value?.ToString() ?? string.Empty
                    };

                    list.Add(key);
                    list.Add(resolved);
                }

                merged = Utils.Text.Format(merged, list.ToArray());
            }
            Utils.Text.Colors color = (Utils.Text.Colors)Enum.Parse(typeof(Utils.Text.Colors), $"Channel{channel}");
            string title = Utils.Text.Color(color, $"〔{Domain.Text.Agent.Instance.Get((int)channel, player)}〕");
            string coloredContent = Utils.Text.Color(color, merged);
            string message = $"{title}{coloredContent}";
            
            Net.Tcp.Instance.Send(player, new Net.Protocol.Information(channel, message));
        }
        public void System(Player player, object[] segments, params (string, object)[] placeholders)
        {
            if (player != null)
            {
                Do(player, Channel.System, segments, placeholders);
            }
        }
        public void Local(Character character, object[] segments, params (string, object)[] placeholders)
        {
            var players = character.Map?.Content.Gets<Player>() ?? new List<Player>();
            foreach (Player player in players)
            {
                Do(player, Channel.Local, segments, placeholders);
            }
        }
        public void Battle(Character character, object[] segments, params (string, object)[] placeholders)
        {
            // Battle channel removed, use Local instead
            Local(character, segments, placeholders);
        }
        public void All(object[] segments, params (string, object)[] placeholders)
        {
            var players =Logic.Agent.Instance.Content.Gets<Player>();
            
            foreach (Player player in players)
            {
                Do(player, Channel.All, segments, placeholders);
            }
        }
        public void Rumor(object[] segments, params (string, object)[] placeholders)
        {
            var players = Manager.Instance.Content.Gets<Player>();
            foreach (Player player in players)
            {
                Do(player, Channel.Rumor, segments, placeholders);
            }
        }
        public void Automation(Player player, object[] segments, params (string, object)[] placeholders)
        {
            if (player != null)
            {
                Do(player, Channel.Automation, segments, placeholders);
            }
        }

    }
}
