using System.Configuration;

namespace VersionOne.TeamSync.Core.Config
{
    public class V1Settings : ConfigurationSection
    {
        public static readonly V1Settings Settings = ConfigurationManager.GetSection("v1Settings") as V1Settings;

        [ConfigurationProperty("authenticationType", DefaultValue = 0, IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 4)]
        public int AuthenticationType
        {
            get { return (int)this["authenticationType"]; }
            set { this["authenticationType"] = value; }
        }

        [ConfigurationProperty("url", IsRequired = false, DefaultValue = "https://www11.v1host.com/V1Integrations")]
        //[RegexStringValidator(@"https?\://\S+")]
        public string Url
        {
            get { return (string)this["url"]; }
            set { this["url"] = value; }
        }

        [ConfigurationProperty("accessToken", IsRequired = false)]
        public string AccessToken
        {
            get { return (string)this["accessToken"]; }
            set { this["accessToken"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = false)]
        //[StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\", MinLength = 1, MaxLength = 256)]
        public string Username
        {
            get { return (string)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = false)]
        public string Password
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("syncIdField", IsRequired = false)]
        public string SyncIdField
        {
            get { return (string)this["syncIdField"]; }
            set { this["syncIdField"] = value; }
        }

        [ConfigurationProperty("proxy")]
        public Proxy Proxy
        {
            get { return (Proxy)this["proxy"]; }
            set { this["proxy"] = value; }
        }
    }

    public class Proxy : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = false)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("url", IsRequired = true)]
        public string Url
        {
            get { return (string)this["url"]; }
            set { this["url"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = true)]
        public string Username
        {
            get { return (string)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("domain", IsRequired = false)]
        public string Domain
        {
            get { return (string)this["domain"]; }
            set { this["domain"] = value; }
        }
    }
}
