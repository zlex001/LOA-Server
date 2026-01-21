using Aop.Api.Request;
using Aop.Api.Response;
using Logic;
using Logic.Database;
using Net;
using System.Net;
using System.Text;
using System.Web;

namespace Domain
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
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Player), OnRemovePlayer);
            Logic.Recharge.Manager.Instance.Content.Add.Register(typeof(Logic.Recharge.Order), OnManagerAddOrder);
        }
        private void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.monitor.Register(Logic.Player.Event.Purchase, OnPlayerPurchase);
            player.monitor.Register(Logic.Player.Event.AlipayPurchase, OnAlipay);
            player.monitor.Register(Logic.Player.Event.CardPurchase, OnCard);
            player.monitor.Register(Logic.Player.Event.GemChanged, OnPlayerGemChanged);
        }
        private void OnRemovePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.monitor.Unregister(Logic.Player.Event.Purchase, OnPlayerPurchase);
            player.monitor.Unregister(Logic.Player.Event.AlipayPurchase, OnAlipay);
            player.monitor.Unregister(Logic.Player.Event.CardPurchase, OnCard);
            player.monitor.Unregister(Logic.Player.Event.GemChanged, OnPlayerGemChanged);
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

                        Logic.Recharge.Order order = Logic.Recharge.Manager.Instance.Content.Get<Logic.Recharge.Order>(o => o.OutTradeNo == outTradeNo);
                        if (Logic.Agent.Instance.Content.Has<Logic.Player>(p => p.Id == order.Player))
                        {
                            Logic.Agent.Instance.Content.Get<Logic.Player>(p => p.Id == order.Player).Gem += order.Amount;
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
            string alipayPublicKey = Logic.Config.Recharge.AlipayPublicKey;
            bool keyFromFile = false;

            bool ok = Aop.Api.Util.AlipaySignature.RSACheckV1(
                notifyData, alipayPublicKey, charset, signType, keyFromFile);

            return ok;
        }


        private void OnManagerAddOrder(params object[] args)
        {
            var order = (Logic.Recharge.Order)args[0];
            var request = new AlipayTradeAppPayRequest
            {
                BizContent = order.BizContent
            };
            request.SetNotifyUrl(Logic.Config.Recharge.AlipayNotifyUrl);

            var response = Logic.Config.Recharge.AlipayAopClient.SdkExecute(request);

            if (Logic.Agent.Instance.Content.Has<Logic.Player>(p => p.Id == order.Player))
            {
                Logic.Agent.Instance.Content.Get<Logic.Player>(p => p.Id == order.Player).AlipayOrder = response.Body;
            }
        }


        private void OnCard(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            Card card = Logic.Database.Agent.Instance.Content.Get<Card>(c => c.Cid == (string)args[1] && c.utilized == default);
            if (card != null)
            {
                player.Gem += card.value;
                card.utilized = DateTime.Now;
                Logic.Database.Agent.Instance.Update(Logic.Config.MySQL.ConnectionString, card);
            }
            else
            {
                Broadcast.Instance.System(player, 
                    [Text.Agent.Instance.Id(Logic.Text.Labels.CardNotExist)]);
            }
        }

        private void OnPlayerGemChanged(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            int o = (int)args[1];
            int v = (int)args[2];
            int d = v - o;
            
            if (d > 0)
            {
                Broadcast.Instance.System(player, 
                    [Text.Agent.Instance.Id(Logic.Text.Labels.GemObtained)],
                    ("amount", d.ToString()));
            }
        }
        
        public void CreateAlipayOrder(Logic.Player player, int amount = 1)
        {
            Logic.Recharge.Manager.Instance.Create<Logic.Recharge.Order>(player.Id, "RMB", amount);
        }












    }
}

