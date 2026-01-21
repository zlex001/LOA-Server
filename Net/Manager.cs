using System.Net;
using System.Net.Sockets;
using Aop.Api.Domain;
using Newtonsoft.Json;
using Utils;

namespace Net
{

    public class Manager : Basic.Manager
    {
        public enum Event
        {
            OptionButton,
            OptionConfirm
        }

        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }

        public override void Init(params object[] args)
        {
            Utils.Debug.Log.Info("NET", "[Manager.Init] Starting network initialization...");
            Http.Instance.Init();
            Utils.Debug.Log.Info("NET", "[Manager.Init] HTTP initialized");
            Tcp.Instance.Init();
            Utils.Debug.Log.Info("NET", "[Manager.Init] TCP initialized");
            Utils.Debug.Log.Info("NET", "[Manager.Init] Network initialization complete");
        }
    }
}
