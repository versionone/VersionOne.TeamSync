using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.Integration.Service.Core.Config
{
    public class ServiceSettings : ConfigurationSection
    {
        public static readonly ServiceSettings Settings = ConfigurationManager.GetSection("serviceSettings") as ServiceSettings;

        [ConfigurationProperty("syncInterval", DefaultValue = 5000, IsRequired = true)]
        public int SyncInterval
        {
            get { return (int)this["syncInterval"]; }
            set { this["syncInterval"] = value; }
        }
    }
}
