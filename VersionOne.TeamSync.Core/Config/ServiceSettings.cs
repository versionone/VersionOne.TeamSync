using System.Configuration;

namespace VersionOne.TeamSync.Core.Config
{
    public class ServiceSettings : ConfigurationSection
    {
        public static readonly ServiceSettings Settings = ConfigurationManager.GetSection("serviceSettings") as ServiceSettings;

        [ConfigurationProperty("syncIntervalInMinutes", DefaultValue = 5, IsRequired = true)]
		public int syncIntervalInMinutes
        {
            get { return (int)this["syncIntervalInMinutes"]; }
            set { this["syncIntervalInMinutes"] = value; }
        }
    }
}
