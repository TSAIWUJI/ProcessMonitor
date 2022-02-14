using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using HttpClientTest.JsonFile;

namespace HttpClientTest
{
    public class SendHttpRequest
    {
        private readonly HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://10.17.10.32") };
        private string _token = "";
        public SendHttpRequest()
        {
            try
            {
                LoginJson postData = new LoginJson() { username = "admin", password = "admin" };
                string json = JsonConvert.SerializeObject(postData);
                TokenJson tokenResult = GetToken(_client, json);
                _token = tokenResult.id_token;
                Console.WriteLine("");
            }
            catch(Exception ex)
            {
                
            }
            // 準備寫入的 data 轉為 json
            
        }
        /// <summary>
        /// 傳入 HttpClient & 要 post 的 Json 轉 String
        /// </summary>
        /// <param name="client"></param>
        /// <param name="postJson"></param>
        /// <returns>TokenJson</returns>
        private TokenJson GetToken(HttpClient client, string postJson)
        {
            HttpContent contentPost = new StringContent(postJson, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync("api/authenticate", contentPost).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            TokenJson convertJson = JsonConvert.DeserializeObject<TokenJson>(result);
            return convertJson;
        }

        public void SendInfoEvent(string ip = "", string note = "", bool gisNotify = false, string priority = "低")
        {
            HttpClient client = _client;
            client.DefaultRequestHeaders.Add("authorization", "Bearer "+ _token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            SendInfoEventJson sendJson = new SendInfoEventJson()
            {
                gisNotify = gisNotify,
                latitude = 25.081203,
                longitude = 121.234315,
                note = note,
                priority = priority,
                reportUser = "IVAD Monitor Process",
                target = ip,
                time = DateTime.Now,
                type = "測試類型",
                notifyEndTime = DateTime.Now.AddDays(1).ToString("o"),

            };
            string json = JsonConvert.SerializeObject(sendJson);
            HttpContent contentPost = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync("api/extend/event-notifications", contentPost).GetAwaiter().GetResult();

            var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine(result);
        }
    }
}
