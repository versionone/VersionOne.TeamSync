using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using VersionOne.TeamSync.Core.Config;

namespace VersionOne.TeamSync.JiraConnector.Config
{
    public interface IJiraSettings
    {
        JiraServerCollection Servers { get; set; }
        string RunFromThisDateOn { get; }
        string GetJiraPriorityIdFromMapping(string baseUrl, string v1Priority);
        string GetV1PriorityIdFromMapping(string baseUrl, string jiraPriority);

        string GetJiraStatusFromMapping(string baseUrl, string jiraProject, string v1Status);
        string GetV1StatusFromMapping(string baseUrl, string jiraProject, string jiraStatus);
    }

    public class JiraSettings : ConfigurationSection, IJiraSettings
    {
        private static IJiraSettings _instance;

        public static IJiraSettings Instance
        {
            set { _instance = value; }
        }

        public static IJiraSettings GetInstance()
        {
            if (_instance == null)
                _instance = ConfigurationManager.GetSection("jiraSettings") as JiraSettings;

            return _instance;
        }

        [ConfigurationProperty("runFromThisDateOn", IsRequired = false, DefaultValue = "01/01/1980")]
        [CallbackValidator(Type = typeof(JiraSettings), CallbackMethodName = "ValidateRunFromThisDateOn")]
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

        public string GetJiraPriorityIdFromMapping(string baseUrl, string v1Priority)
        {
            var jiraServer = Servers.Cast<JiraServer>().Single(serverSettings => serverSettings.Url.Equals(baseUrl));
            var jiraPriorityIdFromMapping = jiraServer.PriorityMappings.DefaultJiraPriorityId;
            if (!string.IsNullOrEmpty(v1Priority))
            {
                var mapping = jiraServer.PriorityMappings.Cast<PriorityMapping>().FirstOrDefault(pm => pm.V1Priority.Equals(v1Priority));
                if (mapping != null)
                    jiraPriorityIdFromMapping = mapping.JiraIssuePriorityId;
            }

            return jiraPriorityIdFromMapping;
        }

        public string GetV1PriorityIdFromMapping(string baseUrl, string jiraPriority)
        {
            var jiraServer = Servers.Cast<JiraServer>().Single(serverSettings => serverSettings.Url.Equals(baseUrl));
            var v1PriorityIdFromMapping = string.Empty;
            if (!string.IsNullOrEmpty(jiraPriority))
            {
                var mapping = jiraServer.PriorityMappings.Cast<PriorityMapping>().FirstOrDefault(pm => pm.JiraPriority.Equals(jiraPriority));
                if (mapping != null)
                    v1PriorityIdFromMapping = mapping.V1WorkitemPriorityId;
            }

            return v1PriorityIdFromMapping;
        }

        public string GetJiraStatusFromMapping(string baseUrl, string jiraProject, string v1Status)
        {
            var jiraServer = Servers.Cast<JiraServer>().Single(serverSettings => serverSettings.Url.Equals(baseUrl));
            var projectMapping = jiraServer.ProjectMappings.Cast<ProjectMapping>().FirstOrDefault(pm => pm.JiraProject.Equals(jiraProject));
            if (projectMapping != null)
            {
                var statusMapping =
                    projectMapping.StatusMappings.Cast<StatusMapping>().FirstOrDefault(sm => sm.Enabled && sm.V1Status.Equals(v1Status));

                if (statusMapping != null)
                    return statusMapping.JiraStatus;
            }

            return null;
        }

        public string GetV1StatusFromMapping(string baseUrl, string jiraProject, string jiraStatus)
        {
            var jiraServer = Servers.Cast<JiraServer>().Single(serverSettings => serverSettings.Url.Equals(baseUrl));
            var projectMapping = jiraServer.ProjectMappings.Cast<ProjectMapping>().FirstOrDefault(pm => pm.JiraProject.Equals(jiraProject));
            if (projectMapping != null)
            {
                var statusMapping =
                    projectMapping.StatusMappings.Cast<StatusMapping>().FirstOrDefault(sm => sm.Enabled && sm.JiraStatus.Equals(jiraStatus));

                if (statusMapping != null)
                    return statusMapping.V1Status;
            }

            return null;
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

        public string DefaultJiraPriorityId { get; set; }

        [ConfigurationProperty("defaultJiraPriority", IsRequired = true)]
        public string DefaultJiraPriority
        {
            get { return (string)this["defaultJiraPriority"]; }
            set { this["defaultJiraPriority"] = value; }
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

        [ConfigurationProperty("jiraStatus", IsRequired = true)]
        public string JiraStatus
        {
            get { return (string)this["jiraStatus"]; }
            set { this["jiraStatus"] = value; }
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
            return ((StatusMapping)(element)).V1Status + "/" + ((StatusMapping)(element)).JiraStatus;
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
