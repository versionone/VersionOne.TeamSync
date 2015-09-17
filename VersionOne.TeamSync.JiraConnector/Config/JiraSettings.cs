using System;
using System.Configuration;
using System.Linq;
using VersionOne.TeamSync.Core.Config;

namespace VersionOne.TeamSync.JiraConnector.Config
{
    public class JiraSettings : ConfigurationSection
    {
        public static readonly JiraSettings Settings = ConfigurationManager.GetSection("jiraSettings") as JiraSettings;

        [ConfigurationProperty("servers", IsDefaultCollection = true)]
        public JiraServerCollection Servers
        {
            get
            {
                return (JiraServerCollection)this["servers"];
            }
            set
            {
                this["servers"] = value;
            }
        }

        public static string GetPriorityIdFromMapping(string baseUrl, string v1Priority)
        {
            var jiraServer =
                Settings.Servers.Cast<JiraServer>()
                    .Single(serverSettings => serverSettings.Url.Equals(baseUrl));
            var jiraPriorityId =
                jiraServer.PriorityMappings.Cast<PriorityMapping>()
                    .First(pm => pm.V1Priority.Equals(v1Priority))
                    .JiraIssuePriorityId;
            return jiraPriorityId;
        }
    }

    public class JiraServer : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
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

        [ConfigurationProperty("ignoreCertificate", IsRequired = true)]
        public bool IgnoreCertificate
        {
            get { return (bool)this["ignoreCertificate"]; }
            set { this["ignoreCertificate"] = value; }
        }

        [ConfigurationProperty("proxy")]
        public Proxy Proxy
        {
            get { return (Proxy)this["proxy"]; }
            set { this["proxy"] = value; }
        }

        [ConfigurationProperty("projectMappings")]
        public ProjectMappingCollection ProjectMappings
        {
            get
            {
                return (ProjectMappingCollection)this["projectMappings"];
            }
            set
            {
                this["projectMappings"] = value;
            }
        }

        [ConfigurationProperty("priorityMappings")]
        public PriorityMappingCollection PriorityMappings
        {
            get
            {
                return (PriorityMappingCollection)this["priorityMappings"];
            }
            set
            {
                this["priorityMappings"] = value;
            }
        }
    }

    [ConfigurationCollection(typeof(JiraServer), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class JiraServerCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "server";

        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new JiraServer();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((JiraServer)(element)).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        public JiraServer this[int idx]
        {
            get
            {
                return (JiraServer)BaseGet(idx);
            }
        }
    }

    public class ProjectMapping : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("v1Project", IsRequired = true)]
        public string V1Project
        {
            get { return (string)this["v1Project"]; }
            set { this["v1Project"] = value; }
        }

        [ConfigurationProperty("jiraProject", IsRequired = true)]
        public string JiraProject
        {
            get { return (string)this["jiraProject"]; }
            set { this["jiraProject"] = value; }
        }

        [ConfigurationProperty("epicSyncType", IsRequired = true)]
        public string EpicSyncType
        {
            get { return (string)this["epicSyncType"]; }
            set { this["epicSyncType"] = value; }
        }
    }

    [ConfigurationCollection(typeof(ProjectMapping), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class ProjectMappingCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "project";

        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProjectMapping();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ProjectMapping)(element)).V1Project + "/" + ((ProjectMapping)(element)).JiraProject;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        public ProjectMapping this[int idx]
        {
            get
            {
                return (ProjectMapping)BaseGet(idx);
            }
        }
    }

    public class PriorityMapping : ConfigurationElement
    {
        public string V1PortfolioItemPriorityId { get; set; }
        public string V1WorkitemPriorityId { get; set; }
        public string JiraIssuePriorityId { get; set; }

        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("v1Priority", IsRequired = true)]
        public string V1Priority
        {
            get { return (string)this["v1Priority"]; }
            set { this["v1Priority"] = value; }
        }

        [ConfigurationProperty("jiraPriority", IsRequired = true)]
        public string JiraPriority
        {
            get { return (string)this["jiraPriority"]; }
            set { this["jiraPriority"] = value; }
        }
    }

    [ConfigurationCollection(typeof(PriorityMapping), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class PriorityMappingCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "priority";

        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PriorityMapping();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PriorityMapping)(element)).V1Priority + "/" + ((PriorityMapping)(element)).JiraPriority;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        public PriorityMapping this[int idx]
        {
            get { return (PriorityMapping)BaseGet(idx); }
        }
    }
}
