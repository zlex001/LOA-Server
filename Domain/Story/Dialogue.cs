using Logic;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Story
{
    public static class DialogueSender
    {
        public static bool Can(Plot plot)
        {
            return plot.Config.dialogues.Length > 0;
        }

        public static void Do(Player player, Plot plot)
        {
            var lines = new List<Net.Protocol.Story.Line>();
            
            foreach (var dialogueId in plot.Config.dialogues)
            {
                var dialogueConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Dialogue>(d => d.Id == dialogueId);
                if (dialogueConfig == null) continue;

                var line = new Net.Protocol.Story.Line
                {
                    // 发言者：0表示旁白，返回空字符串；否则获取发言者名称的多语言文本
                    character = dialogueConfig.character == 0 
                        ? "" 
                        : Text.Agent.Instance.Get(dialogueConfig.character, player),
                    // 对白内容
                    words = Text.Agent.Instance.Get(dialogueConfig.text, player)
                };
                lines.Add(line);
            }

            Net.Tcp.Instance.Send(player, new Net.Protocol.Story(lines));
        }
    }
}
