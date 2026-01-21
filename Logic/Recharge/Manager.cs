using Aop.Api.Request;
using Aop.Api;
using Aop.Api.Response;
using Logic;
using System.Net;
using System.Text;
using System.Web;
using Aop.Api.Util;

namespace Logic.Recharge
{
    public partial class Manager : Basic.Ability
    {

        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }

    }
}
