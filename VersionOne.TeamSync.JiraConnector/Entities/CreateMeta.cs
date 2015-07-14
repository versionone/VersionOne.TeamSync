using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp.Deserializers;

namespace VersionOne.TeamSync.JiraConnector.Entities
{
    public class CreateMeta
    {
        public CreateMeta()
        {
            Projects = new List<MetaProject>();
        }

        public List<MetaProject> Projects { get; set; }
    }

    public class MetaProject
    {
        public MetaProject()
        {
            IssueTypes = new List<MetaIssueType>();
        }

        public string Key { get; set; }

        public List<MetaIssueType> IssueTypes { get; set; }

        public MetaIssueType Epic
        {
            get { return IssueTypes.SingleOrDefault(x => x.Name == "Epic"); }
        }

        public MetaIssueType Story
        {
            get { return IssueTypes.SingleOrDefault(x => x.Name == "Story"); }
        }

        public List<MetaProperty> OfficialEpicCustomFields
        {
            get { return Epic.Fields.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Schema) && x.Schema.StartsWith("com.pyxis.greenhopper.jira:gh")).ToList(); }
        }

        public List<MetaProperty> StoryCustomFields
        {
            get { return Story.Fields.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Schema)).ToList(); }
        }

        public MetaProperty EpicName
        {
            get { return OfficialEpicCustomFields.FirstOrDefault(x => x.Property == "Epic Name"); }
        }

        public MetaProperty EpicLink
        {
            get { return OfficialEpicCustomFields.FirstOrDefault(x => x.Property == "Epic Link"); }
        }

        public MetaProperty StoryPoints
        {
            get { return StoryCustomFields.SingleOrDefault(x => x.Property == "Story Points") ?? MetaProperty.EmptyProperty("Story Points"); }
        }
    }

    public class MetaIssueType
    {
        public string Name { get; set; }
        public MetaField Fields { get; set; }
    }

    [JsonConverter(typeof(MetaData))]
    public class MetaField
    {
        public MetaField()
        {
            Properties = new List<MetaProperty>();
        }

        public List<MetaProperty> Properties { get; set; }
    }

    public class MetaProperty
    {
        public string Property { get; set; }
        public string Schema { get; set; }
        public string Key { get; set; }
        public bool IsEmptyProperty { get; private set; }
        public static MetaProperty EmptyProperty(string key)
        {
            return new MetaProperty
            {
                Key = key,
                Property = "custom_key",
                Schema = "not_found",
                IsEmptyProperty = true
            };
        }
    }

    public class MetaData : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var item = JObject.Load(reader).ToObject<Dictionary<string, object>>();
            //var properties = item.Properties().ToList();
            var resultList = new List<MetaProperty>();

            foreach (var key in item.Keys)
            {
                dynamic meta = item[key];
                var name = meta.name;
                var customNamespace = meta.schema.custom;

                resultList.Add(new MetaProperty()
                {
                    Key = key,
                    Property = name,
                    Schema = customNamespace
                });
            }

            return new MetaField()
            {
                Properties = resultList
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
