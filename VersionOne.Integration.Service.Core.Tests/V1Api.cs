using NUnit.Framework;
using Should;
using VersionOne.Integration.Service.Core.VersionOne;

namespace VersionOne.Integration.Service.Worker.Tests
{
	[TestFixture]
	public class V1Api
	{
		[Test]
		public void Queries_should_be_correctly_formed()
		{
			var api = new VersionOneApi();
			var result = "";
			api.Query("Story", new[]
			{
				"Name", "ID", "Description"
			}, element => result = element.ToString());




			result.ShouldNotBeEmpty();
		}
	}
}
