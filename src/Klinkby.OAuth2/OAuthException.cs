using System;
using System.Runtime.Serialization;
using System.Security;

namespace Klinkby.OAuth2;

[Serializable]
public class OAuthException : Exception
{
    public OAuthException()
    {
    }

    public OAuthException(string message) : base(message)
    {
    }

    [SecuritySafeCritical]
    protected OAuthException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OAuthException(string message, Exception innerException) : base(message, innerException)
    {
    }
}