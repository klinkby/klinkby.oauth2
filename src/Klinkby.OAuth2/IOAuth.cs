using System;

namespace Klinkby.OAuth2;

public interface IOAuth
{
    Uri GetOAuthUrl(string returnUrl, string state);
    string GetToken(string returnUrl);
    UserProfile GetUserProfile(string token);
}