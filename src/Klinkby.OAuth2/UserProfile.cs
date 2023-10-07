using System;

namespace Klinkby.OAuth2;

[Serializable]
public class UserProfile
{
    public string Provider { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Uri Link { get; set; }
    public string Email { get; set; }
    public short? TimeZone { get; set; }
    public string Locale { get; set; }
    public bool Verified { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public Uri Picture { get; set; }
    public Gender Gender { get; set; }

    public override string ToString()
    {
        return Name;
    }
}