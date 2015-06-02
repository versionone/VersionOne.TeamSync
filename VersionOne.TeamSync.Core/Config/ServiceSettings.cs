using System.Configuration;

namespace VersionOne.TeamSync.Core.Config
{
    public class ServiceSettings : ConfigurationSection
    {
        public static readonly ServiceSettings Settings = ConfigurationManager.GetSection("serviceSettings") as ServiceSettings;

        [ConfigurationProperty("syncIntervalInSeconds", DefaultValue = 5, IsRequired = true)]
		public int syncIntervalInSeconds
        {
			get { return (int)this["syncIntervalInSeconds"]; }
			set { this["syncIntervalInSeconds"] = value; }
        }
    }
}
