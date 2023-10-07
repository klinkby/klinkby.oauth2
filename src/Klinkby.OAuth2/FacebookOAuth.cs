using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Web;

namespace Klinkby.OAuth2;

public class FacebookOAuth : OAuthBase
{
    private const string OAuthUrlFormat =
        "https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope={2}&state={3}";

    private const string GraphTokenUrlFormat =
        "https://graph.facebook.com/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}";

    private const string GraphMeUrlFormat = "https://graph.facebook.com/me?{0}";

    private static readonly DataContractJsonSerializer UserSerializer = new(typeof(FacebookUser));

    private static readonly DataContractJsonSerializer ErrorSerializer = new(typeof(ErrorResponse));

    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _scope;

    public FacebookOAuth(string appId, string appSecret, string scope, Uri requestUrl)
        : base(requestUrl)
    {
        _appId = appId;
        _appSecret = appSecret;
        _scope = scope;
    }

    public override Uri GetOAuthUrl(string returnUrl, string state)
    {
        var absReturnUrl = new Uri(Authority, returnUrl);
        var oauthUrl = string.Format(CultureInfo.InvariantCulture, OAuthUrlFormat,
            _appId, WebUtility.UrlEncode(absReturnUrl.ToString()), _scope,
            WebUtility.UrlEncode(state));
        return new Uri(oauthUrl);
    }

    public override string GetToken(string returnUrl)
    {
        var query = HttpUtility.ParseQueryString(RequestUrl.Query);
        var errorDescription = query["error_description"];
        if (!string.IsNullOrEmpty(errorDescription))
            throw new OAuthException(errorDescription);
        var state = query["state"];
        //if (state != Context.Session["oauth_state"] as string) // TODO
        //    throw new OAuthException("The state does not match. You may be a victim of CSRF.");
        var code = query["code"];
        var absReturnUrl = new Uri(Authority, returnUrl);
        var getTokenUri = string.Format(CultureInfo.InvariantCulture, GraphTokenUrlFormat,
            _appId, WebUtility.UrlEncode(absReturnUrl.ToString()), _appSecret,
            WebUtility.UrlEncode(code));
        string token;
        try
        {
            using (var wc = new WebClient())
            {
                token = wc.DownloadString(getTokenUri);
            }
        }
        catch (WebException e)
        {
            throw new OAuthException(GetErrorMessage(e), e);
        }

        return token;
    }

    public override UserProfile GetUserProfile(string token)
    {
        var graphMeUrl = string.Format(CultureInfo.InvariantCulture, GraphMeUrlFormat, token);
        byte[] meBuf;
        try
        {
            using (var wc = new WebClient())
            {
                meBuf = wc.DownloadData(graphMeUrl);
            }
        }
        catch (WebException e)
        {
            throw new OAuthException(GetErrorMessage(e), e);
        }

        FacebookUser me;
        using (var ms = new MemoryStream(meBuf))
        {
            me = (FacebookUser)UserSerializer.ReadObject(ms);
        }

        var profile = me.ToUserProfile();
        return profile;
    }

    private static string GetErrorMessage(WebException e)
    {
        using (var res = e.Response.GetResponseStream())
        {
            Debug.Assert(res != null, "res != null");
            var err = (ErrorResponse)ErrorSerializer.ReadObject(res);
            return err.error.message;
        }
    }

    [Serializable]
    private class ErrorResponse
    {
        public err error;

        [Serializable]
        public class err
        {
            public string message;
            public string type;
        }
    }

    [Serializable]
    private class FacebookUser
    {
        private const string XmlDateFormat = "yyyy-MM-ddTHH:mm:sszzz";
        public string email;

        public string first_name;
        public decimal id;
        public string last_name;
        public string link;
        public string locale;
        public string name;
        public string timezone;
        public string updated_time;
        public bool verified;

        internal UserProfile ToUserProfile()
        {
            DateTime dt;
            short tz;
            return new UserProfile
            {
                Provider = "Facebook",
                Email = email,
                FirstName = first_name,
                Id = id.ToString(CultureInfo.InvariantCulture),
                LastName = last_name,
                Link = string.IsNullOrEmpty(link) ? null : new Uri(link),
                Locale = locale,
                Name = name,
                TimeZone = short.TryParse(timezone, out tz) ? tz : null,
                UpdatedTime =
                    DateTime.TryParseExact(updated_time, XmlDateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal, out dt)
                        ? dt
                        : null,
                Verified = verified
            };
        }
    }
}