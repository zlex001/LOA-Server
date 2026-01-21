using Net;
using Logic;
using Net.Protocol;

namespace Domain
{
    public class Business
    {
        #region Singleton

        private static Business instance;
        public static Business Instance { get { if (instance == null) { instance = new Business(); } return instance; } }

        #endregion

        private bool Open { get; set; }




    }
}
