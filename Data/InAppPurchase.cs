using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Data
{
    public class ReceiptData
    {
        public string key;

        public ReceiptData(string str)
        {
            key = str;
        }
    }

    public static class PurchaseVerification
    {
        private static async Task<string> HttpPost(string postDataStr)
        {
            string url = "https://buy.itunes.apple.com/verifyReceipt";

            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(postDataStr, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                string retString = await response.Content.ReadAsStringAsync();
                return retString;
            }
        }


        private static async Task<string> PostWebRequest(string postUrl, string paramData, Encoding dataEncode = null)
        {
            string ret = string.Empty;

            // ׼����������
            byte[] byteArray = (dataEncode ?? Encoding.UTF8).GetBytes(paramData);

            using (HttpClient client = new HttpClient())
            {
                // ������������
                using (var content = new ByteArrayContent(byteArray))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    content.Headers.ContentLength = byteArray.Length;

                    // ���� POST ����
                    HttpResponseMessage response = await client.PostAsync(postUrl, content);

                    // ȷ������ɹ�
                    response.EnsureSuccessStatusCode();

                    // ��ȡ��Ӧ����
                    ret = await response.Content.ReadAsStringAsync();
                }
            }

            return ret;
        }


        public static string ToJSON(this object o)
        {
            if (o == null)
            {
                return null;
            }
            string str = JsonConvert.SerializeObject(o);
            return str.Replace("key", "receipt-data");
        }

        public static T FromJSON<T>(this string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(input);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static async Task<string> Veryfy(string payload)
        {
            string data = ToJSON(new ReceiptData(payload));
            try
            {
                return await HttpPost(data);
            }
            catch (Exception)
            {
                return "";
            }
        }

    }
    public class Receipt
    {
        public string receipt_type { get; set; }
        public int adam_id { get; set; }
        public int app_item_id { get; set; }
        public string bundle_id { get; set; }
        public string application_version { get; set; }
        public long download_id { get; set; }
        public int version_external_identifier { get; set; }
        public string receipt_creation_date { get; set; }
        public string receipt_creation_date_ms { get; set; }
        public string receipt_creation_date_pst { get; set; }
        public string request_date { get; set; }
        public string request_date_ms { get; set; }
        public string request_date_pst { get; set; }
        public string original_purchase_date { get; set; }
        public string original_purchase_date_ms { get; set; }
        public string original_purchase_date_pst { get; set; }
        public string original_application_version { get; set; }
        public In_App[] in_app { get; set; }
    }

    public class In_App
    {
        public string quantity { get; set; }
        public string product_id { get; set; }
        public string transaction_id { get; set; }
        public string original_transaction_id { get; set; }
        public string purchase_date { get; set; }
        public string purchase_date_ms { get; set; }
        public string purchase_date_pst { get; set; }
        public string original_purchase_date { get; set; }
        public string original_purchase_date_ms { get; set; }
        public string original_purchase_date_pst { get; set; }
        public string is_trial_period { get; set; }
    }

    public class AppStoreVerify
    {
        public Receipt receipt { get; set; }
        public int status { get; set; }
        public string environment { get; set; }
    }

    public class UnityReceipt
    {
        public string Store;
        public string TranscationID;
        public string Payload;
    }
}

