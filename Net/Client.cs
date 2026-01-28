using Logic;
using System.Net.Sockets;
using System.Net;
using Logic.Database;
using Logic.Config;

namespace Net
{
    public class Client : Basic.Manager
    {
        #region Enums
    public enum Event
    {
        Login,
        InitializeRandom,
        InitializeConfirm,
        Send,
        Receive,
        Connected,
        Disconnected,
        PlayerBound,
    }

    public enum Data
    {
        Player,
        Language,
        Device,
        AccountId,
        ConnectionId,
    }
        #endregion

    #region Fields & Properties
    public IPAddress IP { get; private set; }
    public Socket Socket { get; private set; }
    public Basic.Buffer Buffer { get; set; }
    public Logic.Player Player { get => data.Get<Logic.Player>(Data.Player); set => data.Change(Data.Player, value); }
    public Logic.Database.Device.Platforms Platform { get; set; }
    public string ConnectionId { get => data.Get<string>(Data.ConnectionId); set => data.Change(Data.ConnectionId, value); }
        public string Name
        {
            get
            {
                try
                {
                    string ipAddress = ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString();
                    string final = $"[{ipAddress}]";
                    if (Player != null)
                    {
                        final += Player.Id;
                    }
                    return final;
                }
                catch
                {
                    return "？？？";
                }
            }
        }
        #endregion

        #region Constructor & Initialization
        public Client() : base()
        {
            Buffer = new Basic.Buffer();
            monitor.Register(Event.Send, OnSend);
            monitor.Register(Event.Receive, OnReceive);


            data.before.Register(Data.Player, OnBeforePlayerChanged);
        }

    public override void Init(params object[] args)
    {
        Socket socket = (Socket)args[0];
        Socket = socket;
        IP = ((IPEndPoint)socket.RemoteEndPoint).Address;
        data.raw[Data.Language] = Logic.Text.Languages.ChineseSimplified;
        
        int port = ((IPEndPoint)socket.RemoteEndPoint).Port;
        ConnectionId = $"conn_{IP}:{port}";
        
        Utils.Debug.Log.Info("NET", $"[客户端初始化] {IP}:{port} ConnectionId={ConnectionId}");
        monitor.Fire(Event.Connected, IP, port);
    }
        #endregion

        #region Public Methods
        public void Send(Protocol.Base protocol)
        {
            Net.Tcp.Instance.Send(this, protocol);
        }
        #endregion

        #region Network Event Handlers
        private void OnReceive(params object[] args)
        {
            try
            {
                Protocol.Base operation = (Protocol.Base)args[0];
                // Utils.Debug.Log.Info("NET", $"[OnReceive] Processing {operation.GetType().Name} for {Name}");
                operation.Processed(this);
                // Utils.Debug.Log.Info("NET", $"[OnReceive] Completed {operation.GetType().Name} for {Name}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("NET", 
                    $"OnReceive EXCEPTION - Client={Name}: {ex.Message}", 
                    new { StackTrace = ex.StackTrace, ClientName = Name, Protocol = args[0]?.GetType().Name });
                
                // In development mode, rethrow to allow debugger to break
                if (Logic.Agent.Instance.IsDevelopment)
                {
                    throw;
                }
                
                // Production: do not disconnect, only log error to allow player to continue
            }
        }

        private void OnSend(params object[] args)
        {
            byte[] sendBytes = (byte[])args[0];
            // Utils.Debug.Log.Info("NET", $"[OnSend] Sending {sendBytes.Length} bytes to {Name}");
            Socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
        }

        #endregion

        #region Player Event Handlers
        private void OnBeforePlayerChanged(params object[] args)
        {
            Logic.Player o = (Logic.Player)args[0];
            Logic.Player v = (Logic.Player)args[1];
            if (o != null)
            {
                o.data.after.Unregister(Logic.Life.Data.WalkScale, OnAfterPlayerWalkScaleChanged);
                o.monitor.Unregister(Logic.Player.Event.QuitToMenu, OnPlayerQuitToMenu);
                o.monitor.Unregister(Logic.Player.Event.QuitToDesktop, OnPlayerQuitToDesktop);
                o.data.after.Unregister(Logic.Player.Data.AlipayOrder, OnAfterPlayerAlipayOrderChanged);

            }
            if (v != null)
            {
                v.data.after.Register(Logic.Life.Data.WalkScale, OnAfterPlayerWalkScaleChanged);
                v.monitor.Register(Logic.Player.Event.QuitToMenu, OnPlayerQuitToMenu);
                v.monitor.Register(Logic.Player.Event.QuitToDesktop, OnPlayerQuitToDesktop);
                v.data.after.Unregister(Logic.Player.Data.AlipayOrder, OnAfterPlayerAlipayOrderChanged);
                
                v.Language = this.Language;
                
                v.data.after.Register(Logic.Player.Data.Language, OnPlayerLanguageChanged);
                
                monitor.Fire(Event.PlayerBound, this, v);
            }
        }

        private void OnAfterPlayerAlipayOrderChanged(params object[] args)
        {
            string body = (string)args[0];
            Send(new Protocol.AlipayOrder(body));
        }

        private void OnPlayerQuitToDesktop(params object[] args)
        {
            Send(new Protocol.QuitToDesktop());
        }

        private void OnPlayerQuitToMenu(params object[] args)
        {
            Destroy();
        }

        private void OnAfterPlayerWalkScaleChanged(params object[] args)
        {
            double v = (double)args[0];
            Send(new Protocol.Data(Protocol.Data.Type.WalkScale, (int)v));
        }

        private void OnPlayerLanguageChanged(params object[] args)
        {
            Text.Languages newLanguage = (Text.Languages)args[0];
            this.Language = newLanguage;
            Send(new Protocol.LanguageChanged(newLanguage));
        }

        #endregion
        public Text.Languages Language { get => data.Get<Text.Languages>(Data.Language); set => data.Change(Data.Language, value); }

    

    }
}
