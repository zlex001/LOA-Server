using Logic;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Quest
{
    public static class DialogueSender
    {
        public static bool Can(Logic.Quest quest)
        {
            return quest.Config.dialogues.Length > 0;
        }

        public static void Do(Player player, Logic.Quest quest)
        {
            var lines = new List<Net.Protocol.Story.Line>();
            
            foreach (var dialogueId in quest.Config.dialogues)
            {
                var dialogueConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Dialogue>(d => d.Id == dialogueId);
                if (dialogueConfig == null) continue;

                var line = new Net.Protocol.Story.Line
                {
                    // Speaker: 0 means narrator, return empty string; otherwise get speaker name multilingual text
                    character = dialogueConfig.character == 0 
                        ? "" 
                        : Text.Agent.Instance.Get(dialogueConfig.character, player),
                    // Dialogue content
                    words = Text.Agent.Instance.Get(dialogueConfig.text, player)
                };
                lines.Add(line);
            }

            Net.Tcp.Instance.Send(player, new Net.Protocol.Story(lines));
        }
    }
}
