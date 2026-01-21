using Newtonsoft.Json;
using Utils;

namespace Logic
{
    #region OptionHelper
    public static class OptionHelper
    {
        #region Fields
        private static readonly Dictionary<Enum, Enum> _buttonKeyMap = BuildButtonKeyMap();
        #endregion

        #region Private Methods
        private static Dictionary<Enum, Enum> BuildButtonKeyMap()
        {
            var map = new Dictionary<Enum, Enum>();
            // PanelClick 枚举已删除，直接使用 Option.Types 作为按钮键
            foreach (Enum type in Enum.GetValues(typeof(Option.Types)))
            {
                map[type] = type; // 直接使用 Types 本身
            }
            return map;
        }
        #endregion

        #region Public Methods
        public static Enum GetButtonKey(Enum type)
        {
            return _buttonKeyMap.TryGetValue(type, out var key) ? key : type;
        }

        public static Option.Item BuildButton(Enum type, string text)
        {
            var item = new Option.Item(Option.Item.Type.Button, text);
            item.data["Action"] = type.ToString();
            return item;
        }






        #endregion
    }
    #endregion
}
