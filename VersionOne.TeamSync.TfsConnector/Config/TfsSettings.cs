using System;
using System.Configuration;
using System.Linq;
using VersionOne.TeamSync.Core.Config;

namespace VersionOne.TeamSync.TfsConnector.Config
{
    public interface ITfsSettings
    {
        TfsServerCollection Servers { get; set; }
        string RunFromThisDateOn { get; }
    }

    public class TfsSettings : ConfigurationSection, ITfsSettings
    {
        private static ITfsSettings _instance;

        public static ITfsSettings Instance
        {
            set { _instance = value; }
        }

        public static ITfsSettings GetInstance()
        {
            if (_instance == null)
                _instance = ConfigurationManager.GetSection("tfsSettings") as TfsSettings;

            return _instance;
        }

        [ConfigurationProperty("runFromThisDateOn", IsRequired = false, DefaultValue = "01/01/1980")]
        [CallbackValidator(Type = typeof(TfsSettings), CallbackMethodName = "ValidateRunFromThisDateOn")]
        public string RunFromThisDateOn
        {
            get { return (string)this["runFromThisDateOn"]; }
        }

        public static void ValidateRunFromThisDateOn(object value)
        {
            var validator = new RegexStringValidator(@"^([0]?[1-9]|[1][0-2])[./-]([0]?[1-9]|[1|2][0-9]|[3][0|1])[./-]([0-9]{4})$");
            try
            {
                validator.Validate(value);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(string.Format("Invalid date: {0}", value), e.ParamName, e);
            }
        }

        [ConfigurationProperty("servers", IsDefaultCollection = true)]
        public TfsServerCollection Servers
        {
            get
            {
                return (TfsServerCollection)this["servers"];
            }
            set
            {
                this["servers"] = value;
            }
        }
    }

    public class TfsServer : ConfigurationElement
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

    [ConfigurationCollection(typeof(TfsServer), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class TfsServerCollection : ConfigurationElementCollection
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
            return new TfsServer();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TfsServer)(element)).Name;
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

        public TfsServer this[int idx]
        {
            get
            {
                return (TfsServer)BaseGet(idx);
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

        [ConfigurationProperty("TfsProject", IsRequired = true)]
        public string TfsProject
        {
            get { return (string)this["TfsProject"]; }
            set { this["TfsProject"] = value; }
        }

        [ConfigurationProperty("epicSyncType", IsRequired = true)]
        public string EpicSyncType
        {
            get { return (string)this["epicSyncType"]; }
            set { this["epicSyncType"] = value; }
        }

        [ConfigurationProperty("statusMappings")]
        public StatusMappingCollection StatusMappings
        {
            get
            {
                return (StatusMappingCollection)this["statusMappings"];
            }
            set
            {
                this["statusMappings"] = value;
            }
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
            return ((ProjectMapping)(element)).V1Project + "/" + ((ProjectMapping)(element)).TfsProject;
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
        public string V1WorkitemPriorityId { get; set; }
        public string TfsIssuePriorityId { get; set; }

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

        [ConfigurationProperty("TfsPriority", IsRequired = true)]
        public string TfsPriority
        {
            get { return (string)this["TfsPriority"]; }
            set { this["TfsPriority"] = value; }
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
            return ((PriorityMapping)(element)).V1Priority + "/" + ((PriorityMapping)(element)).TfsPriority;
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

        public string DefaultTfsPriorityId { get; set; }

        [ConfigurationProperty("defaultTfsPriority", IsRequired = true)]
        public string DefaultTfsPriority
        {
            get { return (string)this["defaultTfsPriority"]; }
            set { this["defaultTfsPriority"] = value; }
        }
    }

    public class StatusMapping : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("v1Status", IsRequired = true)]
        public string V1Status
        {
            get { return (string)this["v1Status"]; }
            set { this["v1Status"] = value; }
        }

        [ConfigurationProperty("TfsStatus", IsRequired = true)]
        public string TfsStatus
        {
            get { return (string)this["TfsStatus"]; }
            set { this["TfsStatus"] = value; }
        }
    }


    [ConfigurationCollection(typeof(StatusMapping), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class StatusMappingCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "status";

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
            return new StatusMapping();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StatusMapping)(element)).V1Status + "/" + ((StatusMapping)(element)).TfsStatus;
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

        public StatusMapping this[int idx]
        {
            get
            {
                return (StatusMapping)BaseGet(idx);
            }
        }
    }
}
