using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web;

namespace Klinkby.OAuth2;

public class GoogleOAuth : OAuthBase
{
    private const string OAuthUrlFormat =
        "https://accounts.google.com/o/oauth2/auth?client_id={0}&redirect_uri={1}&scope={2}&state={3}&response_type=code";

    private const string GraphTokenUrl = "https://accounts.google.com/o/oauth2/token";

    private const string GraphTokenPostFormat =
        "code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code";

    private const string GraphMeUrlFormat =
        "https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token={0}";

    private static readonly DataContractJsonSerializer TokenResponseSerializer = new(typeof(TokenResponse));

    private static readonly DataContractJsonSerializer UserSerializer = new(typeof(GoogleUser));

    private static readonly DataContractJsonSerializer ErrorSerializer = new(typeof(ErrorResponse));

    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _scope;

    public GoogleOAuth(string appId, string appSecret, string scope, Uri requestUrl)
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
            _appId, WebUtility.UrlEncode(absReturnUrl.ToString()),
            WebUtility.UrlEncode(_scope), WebUtility.UrlEncode(state));
        return new Uri(oauthUrl);
    }

    public override string GetToken(string returnUrl)
    {
        var query = HttpUtility.ParseQueryString(RequestUrl.Query);
        var errorDescription = query["error"];
        if (!string.IsNullOrEmpty(errorDescription))
            throw new OAuthException(errorDescription);
        var state = query["state"];
        //if (state != Context.Session["oauth_state"] as string) // TODO
        //    throw new OAuthException("The state does not match. You may be a victim of CSRF.");
        var code = query["code"];
        var absReturnUrl = new Uri(Authority, returnUrl);
        var graphTokenPost = string.Format(CultureInfo.InvariantCulture, GraphTokenPostFormat,
            WebUtility.UrlEncode(code), _appId, _appSecret,
            WebUtility.UrlEncode(absReturnUrl.ToString()));
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
            {
                tokenResponse = (TokenResponse)TokenResponseSerializer.ReadObject(ms);
            }

            token = tokenResponse.access_token;
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

        GoogleUser me;
        using (var ms = new MemoryStream(meBuf))
        {
            me = (GoogleUser)UserSerializer.ReadObject(ms);
        }

        var profile = me.ToUserProfile();
        return profile;
    }

    private static string GetErrorMessage(WebException e)
    {
        if (e.Response.ContentType == "application/json")
            using (var res = e.Response.GetResponseStream())
            {
                Debug.Assert(res != null, "res != null");
                var err = (ErrorResponse)ErrorSerializer.ReadObject(res);
                return err.error;
            }

        return ((HttpWebResponse)e.Response).StatusDescription;
    }

    [Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    [Serializable]
    private class GoogleUser
    {
        private string email;
        private string family_name;
        [OptionalField] private string gender;
        private string given_name;
        private decimal id;
        [OptionalField] private string link;
        private string name;
        [OptionalField] private string picture;
        private bool verified_email;

        internal UserProfile ToUserProfile()
        {
            Gender g;
            return new UserProfile
            {
                Provider = "Google",
                Email = email,
                FirstName = given_name,
                Id = id.ToString(CultureInfo.InvariantCulture),
                LastName = family_name,
                Link = string.IsNullOrEmpty(link) ? null : new Uri(link),
                //Locale = locale,
                Name = name,
                //TimeZone = timezone,
                //UpdatedTime = updated_time,
                Verified = verified_email,
                Picture = string.IsNullOrEmpty(picture) ? null : new Uri(picture),
                Gender = Enum.TryParse(gender, true, out g) ? g : Gender.Other
            };
        }
    }

    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public string expires_in;
        [OptionalField] public string refresh_token;
        public string token_type;
    }
}