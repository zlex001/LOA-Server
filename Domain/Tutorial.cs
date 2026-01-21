using Logic;
using Utils;

namespace Domain
{
    public class Tutorial
    {
        #region Singleton

        private static Tutorial instance;
        public static Tutorial Instance { get { if (instance == null) { instance = new Tutorial(); } return instance; } }

        #endregion

        private Logic.Config.Item MasterGuidanceItem;
        #region Initialization

        public void Init()
        {
            MasterGuidanceItem = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Item>(c => c.Id == -1);
        }

        #endregion


    }
}
