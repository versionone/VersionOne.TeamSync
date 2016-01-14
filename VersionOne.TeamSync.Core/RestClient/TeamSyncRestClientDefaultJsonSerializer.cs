using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp.Serializers;

namespace VersionOne.TeamSync.Core.RestClient {

	public class TeamSyncRestClientDefaultJsonSerializer : ISerializer
	{
		public TeamSyncRestClientDefaultJsonSerializer()
		{
			ContentType = "application/json";
		}

		private JsonSerializerSettings _settings = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new LowercaseContractResolver()
		};

		public string Serialize(object obj)
		{
			return JsonConvert.SerializeObject(obj, _settings);
		}

		public string RootElement { get; set; }
		public string Namespace { get; set; }
		public string DateFormat { get; set; }
		public string ContentType { get; set; }
	}

	public class LowercaseContractResolver : DefaultContractResolver
	{
		protected override string ResolvePropertyName(string propertyName)
		{
			return propertyName.ToLower();
		}
	}
}