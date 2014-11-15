using System;
using System.Web;

namespace Klinkby.OAuth2
{
    public abstract class OAuthBase : IOAuth
    {
        private Uri _authority;

        protected HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        public Uri Authority
        {
            get
            {
                return _authority ??
                       (_authority = new Uri(Context.Request.Url.GetLeftPart(UriPartial.Authority), UriKind.Absolute));
            }
            set { _authority = value; }
        }

        public abstract Uri GetOAuthUrl(string returnUrl, string state);

        public abstract string GetToken(string returnUrl);

        public abstract UserProfile GetUserProfile(string token);

        protected Uri GetAbsoluteUrl(string relativeUrl)
        {
            return new Uri(Authority, relativeUrl);
        }
    }
}