using System;

namespace Klinkby.OAuth2;

public abstract class OAuthBase : IOAuth
{
    protected OAuthBase(Uri requestUrl)
    {
        RequestUrl = requestUrl;
        Authority = new Uri(requestUrl.Authority, UriKind.Absolute);
    }

    protected Uri RequestUrl { get; }

    public Uri Authority { get; }

    public abstract Uri GetOAuthUrl(string returnUrl, string state);

    public abstract string GetToken(string returnUrl);

    public abstract UserProfile GetUserProfile(string token);
}