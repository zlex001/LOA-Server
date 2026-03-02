using Newtonsoft.Json;

namespace Data.Design
{
    public class Dialogue : Ability
    {
        public string character;  // 角色CID，空表示旁白
        public string text;       // 对白内容，引用多语言表CID

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            character = Get<string>(dict, "character") ?? "";
            text = Get<string>(dict, "text");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Dialogue config in Agent.Instance.Content.Gets<Dialogue>())
            {
                // 获取角色名称ID（如果有角色）
                int characterNameId = 0;
                if (!string.IsNullOrEmpty(config.character))
                {
                    var life = Agent.Instance.Content.Get<Life>(l => l.cid == config.character);
                    if (life != null)
                    {
                        var nameText = Agent.Instance.Content.Get<Multilingual>(m => m.cid == life.name);
                        if (nameText != null)
                        {
                            characterNameId = nameText.id;
                        }
                    }
                }

                // 获取对白内容ID
                int textId = 0;
                if (!string.IsNullOrEmpty(config.text))
                {
                    var textEntry = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.text);
                    if (textEntry != null)
                    {
                        textId = textEntry.id;
                    }
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id},
                    {"character", characterNameId},  // 角色名称的多语言ID，0表示旁白
                    {"text", textId},                // 对白内容的多语言ID
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Dialogue.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}
