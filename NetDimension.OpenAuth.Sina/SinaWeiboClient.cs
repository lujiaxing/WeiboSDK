﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetDimension.OpenAuth.Sina
{
	public class SinaWeiboClient : OpenAuthenticationClientBase
	{
		const string AUTH_URL = "https://api.weibo.com/oauth2/authorize";
		const string TOKEN_URL = "https://api.weibo.com/oauth2/access_token";
		const string API_URL = "https://api.weibo.com/2/";

		public string UID
		{
			get;
			set;
		}

        public override string UserID
        {
            get
            {
                return UID;
            }
        }

        public SinaWeiboClient(string appKey, string appSecret, string callbackUrl, string accessToken = null, string uid = null)
			: base(appKey, appSecret, callbackUrl, accessToken)
		{
			ClientName = "SinaWeibo";
			UID = uid;

			if (!(string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(uid)))
			{
				isAccessTokenSet = true;
			}
		}

		protected override string AuthorizationCodeUrl
		{
			get { return AUTH_URL; }
		}

		protected override string AccessTokenUrl
		{
			get { return TOKEN_URL; }
		}

		protected override string BaseApiUrl
		{
			get { return API_URL; }
		}

		public override string GetAuthorizationUrl()
		{
			var ub = new UriBuilder(AuthorizationCodeUrl);
			ub.Query = string.Format("client_id={0}&response_type=code&redirect_uri={1}&state={2}", ClientId, Uri.EscapeDataString(CallbackUrl), Uri.EscapeDataString(State));



			return ub.ToString();
		}



		public override void GetAccessTokenByCode(string code)
		{


			var response = HttpPost(TOKEN_URL, new
			{
				client_id = ClientId,
				client_secret = ClientSecret,
				grant_type = "authorization_code",
				code = code,
				redirect_uri = CallbackUrl
			});

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return;

			var result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
			if (result["access_token"] == null)
			{
				return;
			}
			AccessToken = result.Value<string>("access_token");
			UID = result.Value<string>("uid");

			isAccessTokenSet = true;
		}

		public override Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> parameters)
		{
			if (IsAuthorized)
			{
				if (parameters == null)
					parameters = new Dictionary<string, object>();

				if (!parameters.ContainsKey("source"))
				{
					parameters["source"] = ClientId;
				}

				if (!parameters.ContainsKey("access_token"))
				{
					parameters["access_token"] = AccessToken;
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

				if (!parameters.ContainsKey("source"))
				{
					parameters["source"] = ClientId;
				}

				if (!parameters.ContainsKey("access_token"))
				{
					parameters["access_token"] = AccessToken;
				}
			}

			return base.HttpPostAsync(api, parameters);
		}

        /// <summary>
        /// 获取用户基本信息
        /// </summary>
        /// <param name="accessToken">ACCESS TOKEN</param>
        /// <param name="userid">用户ID. 就是 http://weibo.com/u/xxxxxxxxxx &lt;- 这一串数字 </param>
        /// <returns></returns>
        public override UserBasicInfo GetUserBasicInfomation(String accessToken, String userid)
        {
            String requestUrl = String.Format(API_URL, "users/show.json");

            var reply = HttpPost(requestUrl, new
            {
                access_token = accessToken,
                uid = userid
            });

            if (reply.IsSuccessStatusCode)
            {
                var result = reply.Content.ReadAsStringAsync().Result;

                Dictionary<String, String> rep = JsonConvert.DeserializeObject<Dictionary<String, String>>(result);

                UserBasicInfo userInfo = new UserBasicInfo();

                if (rep.ContainsKey("id"))
                    userInfo.OpenID = rep["id"];

                if (rep.ContainsKey("screen_name"))
                    userInfo.Name = rep["screen_name"];

                if (rep.ContainsKey("gender"))
                    userInfo.Gender = rep["gender"] == "m" ? "男" : "女";

                if (rep.ContainsKey("avatar_large"))
                    userInfo.Avatar = rep["avatar_large"];

                return userInfo;
            }
            else
            {
                throw new Exception("failed to get user's basic infomation. code = " + reply.StatusCode);
            }
        }



    }
}
