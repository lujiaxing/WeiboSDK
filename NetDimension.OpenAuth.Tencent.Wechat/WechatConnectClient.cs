using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetDimension.OpenAuth.Tencent.Wechat
{
	public class WechatConnectClient : OpenAuthenticationClientBase
	{
        

		const string AUTH_URL = "https://open.weixin.qq.com/connect/qrconnect";
		const string TOKEN_URL = "https://api.weixin.qq.com/sns/oauth2/access_token";
		const string API_URL = "https://api.weixin.qq.com/sns/{0}";


		protected override string BaseApiUrl
		{
			get { return API_URL; }
		}

		protected override string AuthorizationCodeUrl
		{
			get { return AUTH_URL; }
		}

		protected override string AccessTokenUrl
		{
			get { return TOKEN_URL; }
		}

		public string OpenId
		{
			get;
			set;
		}

        public override string UserID
        {
            get
            {
                return OpenId;
            }
        }

        public WechatConnectClient(string clientId, string clientSecret, string callbackUrl, string accessToken = null, string openId = null) :
			base(clientId, clientSecret, callbackUrl, accessToken)
		{
			OpenId = openId;
			ClientName = "WechatConnect";
		}

		public override string GetAuthorizationUrl()
		{
			var ub = new UriBuilder(AuthorizationCodeUrl);

			ub.Query = string.Format("appid={0}&response_type=code&redirect_uri={1}&state={2}&scope=snsapi_login", ClientId, Uri.EscapeDataString(CallbackUrl), Uri.EscapeDataString(State));
                
			return ub.ToString();

		}

		public override void GetAccessTokenByCode(string code)
		{

			var response = HttpPost(TOKEN_URL, new
			{
				grant_type = "authorization_code",
                appid = ClientId,
                secret = ClientSecret,
				code = code
			});


			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return;


			var result = response.Content.ReadAsStringAsync().Result;

			var accessToken = string.Empty;

            Dictionary<String, String> reply = JsonConvert.DeserializeObject<Dictionary<String, String>>(result);

            if (reply.ContainsKey("errcode"))
                throw new Exception(reply["errcode"]);


            if (reply.ContainsKey("access_token"))
            {
                AccessToken = reply["access_token"];
                isAccessTokenSet = true;
            }
            else
            {
                throw new Exception("unacceptable reply from wechat: " + result);
            }

            if (reply.ContainsKey("openid"))
                OpenId = reply["openid"];
            
		}

		public override Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> parameters)
		{
			if (IsAuthorized)
			{
				if (parameters == null)
					parameters = new Dictionary<string, object>();

				if (!parameters.ContainsKey("access_token"))
				{
					parameters["access_token"] = AccessToken;
				}
				if (!parameters.ContainsKey("oauth_consumer_key"))
				{
					parameters["oauth_consumer_key"] = ClientId;
				}

				if (!parameters.ContainsKey("openid"))
				{
					parameters["openid"] = OpenId;
				}

			}



			return base.HttpGetAsync(api, parameters);
		}

		public override Task<HttpResponseMessage> HttpPostAsync(string api, Dictionary<string, object> parameters)
		{
			if (IsAuthorized)
			{
				if (parameters == null)
					parameters = new Dictionary<string, object>();

				if (!parameters.ContainsKey("access_token"))
				{
					parameters["access_token"] = AccessToken;
				}
				if (!parameters.ContainsKey("oauth_consumer_key"))
				{
					parameters["oauth_consumer_key"] = ClientId;
				}

				if (!parameters.ContainsKey("openid"))
				{
					parameters["openid"] = OpenId;
				}
			}

			return base.HttpPostAsync(api, parameters);
		}

        public override UserBasicInfo GetUserBasicInfomation(String accessToken, String userOpenId)
        {
            String requestUrl = String.Format(API_URL, "userinfo");

            var reply = HttpPost(requestUrl, new {
                access_token = accessToken, openid = userOpenId });

            if (reply.IsSuccessStatusCode)
            {
                var result = reply.Content.ReadAsStringAsync().Result;

                Dictionary<String, String> rep = JsonConvert.DeserializeObject<Dictionary<String, String>>(result);

                UserBasicInfo userInfo = new UserBasicInfo();

                if (rep.ContainsKey("openid"))
                    userInfo.OpenID = rep["openid"];

                if (rep.ContainsKey("nickname"))
                    userInfo.Name = rep["nickname"];

                if (rep.ContainsKey("sex"))
                    userInfo.Gender = rep["sex"] == "1" ? "男" : "女";

                if (rep.ContainsKey("headimgurl"))
                    userInfo.Avatar = rep["headimgurl"];

                return userInfo;
            }
            else
            {
                throw new Exception("failed to get user's basic infomation. code = " + reply.StatusCode);
            }
        }
    }
}
