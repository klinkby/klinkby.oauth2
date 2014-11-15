using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Web;

namespace Klinkby.OAuth2
{
    public class LiveOAuth : OAuthBase
    {
        private const string OAuthUrlFormat =
            "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={2}&response_type=code&redirect_uri={1}&state={3}";

        private const string GraphTokenUrl = "https://login.live.com/oauth20_token.srf";

        private const string GraphTokenPostFormat =
            "code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code";

        private const string GraphMeUrlFormat = "https://apis.live.net/v5.0/me?access_token={0}";

        private static readonly DataContractJsonSerializer UserSerializer =
            new DataContractJsonSerializer(typeof (LiveUser));

        private static readonly DataContractJsonSerializer ErrorSerializer =
            new DataContractJsonSerializer(typeof (ErrorResponse));

        private static readonly DataContractJsonSerializer TokenSerializer =
            new DataContractJsonSerializer(typeof (TokenResponse));

        private static readonly DataContractJsonSerializer GraphErrorSerializer =
            new DataContractJsonSerializer(typeof (GraphErrorResponse));

        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _scope;

        public LiveOAuth(string appId, string appSecret, string scope)
        {
            _appId = appId;
            _appSecret = appSecret;
            _scope = scope;
        }

        public override Uri GetOAuthUrl(string returnUrl, string state)
        {
            var absReturnUrl = new Uri(Authority, returnUrl);
            string oauthUrl = string.Format(CultureInfo.InvariantCulture, OAuthUrlFormat,
                _appId, Context.Server.UrlEncode(absReturnUrl.ToString()),
                Context.Server.UrlEncode(_scope), Context.Server.UrlEncode(state));
            return new Uri(oauthUrl);
        }

        public override string GetToken(string returnUrl)
        {
            HttpRequest req = Context.Request;
            string errorDescription = req.QueryString["error"];
            if (!string.IsNullOrEmpty(errorDescription))
                throw new OAuthException(errorDescription);
            string state = req.QueryString["state"];
            //if (state != Context.Session["oauth_state"] as string) // TODO
            //    throw new OAuthException("The state does not match. You may be a victim of CSRF.");
            string code = req.QueryString["code"];
            var absReturnUrl = new Uri(Authority, returnUrl);
            string graphTokenPost = string.Format(CultureInfo.InvariantCulture, GraphTokenPostFormat,
                code, _appId, _appSecret,
                Context.Server.UrlEncode(absReturnUrl.ToString()));
            string token;
            try
            {
                TokenResponse tokenResponse;
                byte[] resBuf;
                using (var wc = new WebClient())
                {
                    wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    resBuf = wc.UploadData(GraphTokenUrl, wc.Encoding.GetBytes(graphTokenPost));
                }
                using (var ms = new MemoryStream(resBuf))
                    tokenResponse = (TokenResponse) TokenSerializer.ReadObject(ms);
                token = tokenResponse.access_token;
            }
            catch (WebException e)
            {
                throw new OAuthException(GetErrorMessage(e));
            }
            return token;
        }

        public override UserProfile GetUserProfile(string token)
        {
            string graphMeUrl = string.Format(CultureInfo.InvariantCulture, GraphMeUrlFormat, token);
            byte[] meBuf;
            try
            {
                using (var wc = new WebClient())
                    meBuf = wc.DownloadData(graphMeUrl);
            }
            catch (WebException e)
            {
                throw new OAuthException(GetGraphErrorMessage(e));
            }
            LiveUser me;
            using (var ms = new MemoryStream(meBuf))
                me = (LiveUser) UserSerializer.ReadObject(ms);
            UserProfile profile = me.ToUserProfile();
            return profile;
        }

        private static string GetErrorMessage(WebException e)
        {
            using (Stream res = e.Response.GetResponseStream())
            {
                Debug.Assert(res != null, "res != null");
                var err = (ErrorResponse) ErrorSerializer.ReadObject(res);
                return err.error_description;
            }
        }

        private static string GetGraphErrorMessage(WebException e)
        {
            using (Stream res = e.Response.GetResponseStream())
            {
                Debug.Assert(res != null, "res != null");
                var err = (GraphErrorResponse) GraphErrorSerializer.ReadObject(res);
                return err.message;
            }
        }

        [Serializable]
        private class Emails
        {
            public string account;
        }


        [Serializable]
        private class ErrorResponse
        {
            public string error;
            public string error_description;
        }

        [Serializable]
        private class GraphErrorResponse
        {
            public string code;
            public string error;
            public string message;
        }

        [Serializable]
        private class LiveUser
        {
            private const string XmlDateFormat = "yyyy-MM-ddTHH:mm:sszzz";
            public Emails emails;
            public string first_name;
            public string gender;
            public string id;
            public string last_name;
            public string link;
            public string locale;
            public string name;
            public string updated_time;

            internal UserProfile ToUserProfile()
            {
                DateTime dt;
//                short tz;
                Gender eGender;
                Enum.TryParse(gender, false, out eGender);
                return new UserProfile
                {
                    Provider = "Live",
                    Email = emails.account,
                    FirstName = first_name,
                    Id = id,
                    LastName = last_name,
                    Link = string.IsNullOrEmpty(link) ? null : new Uri(link),
                    Locale = locale,
                    Name = name,
                    Gender = eGender,
                    TimeZone = null,
                    UpdatedTime =
                        DateTime.TryParseExact(updated_time, XmlDateFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out dt)
                            ? (DateTime?) dt
                            : null,
                    Verified = true,
                };
            }
        }


        [Serializable]
        private class TokenResponse
        {
            public string access_token;
            public string authentication_token;
            public int expires_in;
            public string scope;
            public string token_type;
        }
    }
}