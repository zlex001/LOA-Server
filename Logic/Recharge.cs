using Aop.Api.Request;
using Aop.Api.Response;
using Data;
using Data.Database;
using Net;
using System.Net;
using System.Text;
using System.Web;

namespace Logic
{
    public class Recharge
    {
        #region Instance Management
        private static Recharge instance;
        public static Recharge Instance { get { if (instance == null) { instance = new Recharge(); } return instance; } }


        #endregion
        public void Init()
        {
            Net.Http.Instance.RegisterRoute(8882, "/api/alipay/notify", Net.Http.Event.Alipay, OnAlipay);
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Player), OnRemovePlayer);
            global::Data.Recharge.Manager.Instance.Content.Add.Register(typeof(global::Data.Recharge.Order), OnManagerAddOrder);
        }
        private void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Register(global::Data.Player.Event.Purchase, OnPlayerPurchase);
            player.monitor.Register(global::Data.Player.Event.AlipayPurchase, OnAlipay);
            player.monitor.Register(global::Data.Player.Event.CardPurchase, OnCard);
            player.monitor.Register(global::Data.Player.Event.GemChanged, OnPlayerGemChanged);
        }
        private void OnRemovePlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Unregister(global::Data.Player.Event.Purchase, OnPlayerPurchase);
            player.monitor.Unregister(global::Data.Player.Event.AlipayPurchase, OnAlipay);
            player.monitor.Unregister(global::Data.Player.Event.CardPurchase, OnCard);
            player.monitor.Unregister(global::Data.Player.Event.GemChanged, OnPlayerGemChanged);
        }
        private void OnPlayerPurchase(params object[] args)
        {
        
        }
        private void OnAlipay(params object[] args)
        {
            var context = (HttpListenerContext)args[0];

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                    string requestBody = await reader.ReadToEndAsync();

                    Dictionary<string, string> notifyData = HttpUtility.ParseQueryString(requestBody)
                        .AllKeys.ToDictionary(k => k, k => HttpUtility.ParseQueryString(requestBody)[k]);

                    if (notifyData.Count == 0)
                    {
                        await Net.Http.Instance.SendError(context.Response, "回调数据为空", 400);
                        return;
                    }

                    if (!VerifyAlipaySign(notifyData))
                    {
                        await Net.Http.Instance.SendError(context.Response, "验签失败", 403);
                        return;
                    }

                    string tradeStatus = notifyData.GetValueOrDefault("trade_status", "");
                    string outTradeNo = notifyData.GetValueOrDefault("out_trade_no", "");
                    string alipayTradeNo = notifyData.GetValueOrDefault("trade_no", "");
                    string totalAmount = notifyData.GetValueOrDefault("total_amount", "");

                    if (tradeStatus == "TRADE_SUCCESS" || tradeStatus == "TRADE_FINISHED")
                    {

                        global::Data.Recharge.Order order = global::Data.Recharge.Manager.Instance.Content.Get<global::Data.Recharge.Order>(o => o.OutTradeNo == outTradeNo);
                        if (global::Data.Agent.Instance.Content.Has<global::Data.Player>(p => p.Id == order.Player))
                        {
                            global::Data.Agent.Instance.Content.Get<global::Data.Player>(p => p.Id == order.Player).Gem += order.Amount;
                        }

                        await Net.Http.Instance.SendJson(context.Response, new { Success = true });
                    }
                    else
                    {
                        await Net.Http.Instance.SendError(context.Response, "订单状态非成功", 400);
                    }
                }
                catch (Exception ex)
                {
                    await Net.Http.Instance.SendError(context.Response, "服务器异常", 500);
                }
            });
        }

        private bool VerifyAlipaySign(Dictionary<string, string> notifyData)
        {
            const string charset = "utf-8";
            const string signType = "RSA2";
            string alipayPublicKey = global::Data.Config.Recharge.AlipayPublicKey;
            bool keyFromFile = false;

            bool ok = Aop.Api.Util.AlipaySignature.RSACheckV1(
                notifyData, alipayPublicKey, charset, signType, keyFromFile);

            return ok;
        }


        private void OnManagerAddOrder(params object[] args)
        {
            var order = (global::Data.Recharge.Order)args[0];
            var request = new AlipayTradeAppPayRequest
            {
                BizContent = order.BizContent
            };
            request.SetNotifyUrl(global::Data.Config.Recharge.AlipayNotifyUrl);

            var response = global::Data.Config.Recharge.AlipayAopClient.SdkExecute(request);

            if (global::Data.Agent.Instance.Content.Has<global::Data.Player>(p => p.Id == order.Player))
            {
                global::Data.Agent.Instance.Content.Get<global::Data.Player>(p => p.Id == order.Player).AlipayOrder = response.Body;
            }
        }


        private void OnCard(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            Card card = global::Data.Database.Agent.Instance.Content.Get<Card>(c => c.Cid == (string)args[1] && c.utilized == default);
            if (card != null)
            {
                player.Gem += card.value;
                card.utilized = DateTime.Now;
                global::Data.Database.Agent.Instance.Update(global::Data.Config.MySQL.ConnectionString, card);
            }
            else
            {
                Broadcast.Instance.System(player, 
                    [Text.Agent.Instance.Id(global::Data.Text.Labels.CardNotExist)]);
            }
        }

        private void OnPlayerGemChanged(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int o = (int)args[1];
            int v = (int)args[2];
            int d = v - o;
            
            if (d > 0)
            {
                Broadcast.Instance.System(player, 
                    [Text.Agent.Instance.Id(global::Data.Text.Labels.GemObtained)],
                    ("amount", d.ToString()));
            }
        }
        
        public void CreateAlipayOrder(global::Data.Player player, int amount = 1)
        {
            global::Data.Recharge.Manager.Instance.Create<global::Data.Recharge.Order>(player.Id, "RMB", amount);
        }












    }
}

