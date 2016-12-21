using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetDimension.OpenAuth.Tencent.QQ
{
	public class QQConnectClient : OpenAuthenticationClientBase
	{
		const string AUTH_URL = "https://graph.qq.com/oauth2.0/authorize";
		const string TOKEN_URL = "https://graph.qq.com/oauth2.0/token";
		const string API_URL = "https://graph.qq.com/{0}?access_token={1}&oauth_consumer_key={2}&openid={3}";
		const string OPEN_API_URL = "https://graph.qq.com/oauth2.0/me";


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

        public QQConnectClient(string clientId, string clientSecret, string callbackUrl, string accessToken = null, string openId = null) :
			base(clientId, clientSecret, callbackUrl, accessToken)
		{
			OpenId = openId;
			ClientName = "QQConnect";
		}

		public override string GetAuthorizationUrl()
		{
			var ub = new UriBuilder(AuthorizationCodeUrl);

			ub.Query = string.Format("response_type=code&client_id={0}&redirect_uri={1}&state=QQ&scope=get_user_info,add_t,add_pic_t", ClientId, Uri.EscapeDataString(CallbackUrl));

			return ub.ToString();

		}

		public override void GetAccessTokenByCode(string code)
		{

			var response = HttpPost(TOKEN_URL, new
			{
				grant_type = "authorization_code",
				client_id = ClientId,
				client_secret = ClientSecret,
				code = code,
				redirect_uri = CallbackUrl
			});


			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return;


			var result = response.Content.ReadAsStringAsync().Result;

			var accessToken = string.Empty;

			var pattern = @"access_token=(([\d|a-zA-Z]*))";

			if (Regex.IsMatch(result, pattern))
			{
				accessToken = Regex.Match(result, pattern).Groups[1].Value;
			}

			response = HttpGet(OPEN_API_URL, new Dictionary<string, object>
			{
				{"access_token" , accessToken}
			});

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return;

			result = response.Content.ReadAsStringAsync().Result;

			pattern = @"\""openid\"":\""([\d|a-zA-Z]+)\""";

			if (!Regex.IsMatch(result, pattern))
			{
				return;
			}



			AccessToken = accessToken;
			OpenId = Regex.Match(result, pattern).Groups[1].Value;

			isAccessTokenSet = true;

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


        /// <summary>
        /// 获取用户基本信息
        /// </summary>
        /// <param name="accessToken">ACCESS TOKEN</param>
        /// <param name="userid">用户OPENID</param>
        /// <returns></returns>
        public override UserBasicInfo GetUserBasicInfomation(String accessToken, String userid)
        {
            String requestUrl = String.Format(API_URL, "get_user_info", accessToken, ClientId, userid);

            var reply = HttpPost(requestUrl, new{});

            if (reply.IsSuccessStatusCode)
            {
                var result = reply.Content.ReadAsStringAsync().Result;

                Dictionary<String, String> rep = JsonConvert.DeserializeObject<Dictionary<String, String>>(result);

                UserBasicInfo userInfo = new UserBasicInfo();

                Int32 ret = 0;
                if(rep.ContainsKey("ret") && Int32.TryParse(rep["ret"], out ret))
                    throw new Exception("invalid reply. No return code.");
                else if(ret < 0)
                    throw new Exception(rep.ContainsKey("msg") ? rep["msg"] : " failed to get user's infomation, no message. return code = " + ret);

                
                userInfo.OpenID = userid;

                if (rep.ContainsKey("nickname"))
                    userInfo.Name = rep["nickname"];

                if (rep.ContainsKey("gender"))
                    userInfo.Gender = rep["gender"];

                if (rep.ContainsKey("figureurl_qq_1"))
                    userInfo.Avatar = rep["figureurl_qq_1"];

                return userInfo;
            }
            else
            {
                throw new Exception("failed to get user's basic infomation. code = " + reply.StatusCode);
            }
        }



    }
}
