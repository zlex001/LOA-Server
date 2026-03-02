namespace Data.Config
{
    public class Dialogue : Ability
    {
        public int character;  // 角色名称的多语言ID，0表示旁白
        public int text;       // 对白内容的多语言ID

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            character = Get<int>(dict, "character");
            text = Get<int>(dict, "text");
        }
    }
}
