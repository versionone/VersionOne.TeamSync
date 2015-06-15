using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class JiraInfoTests
    {
        [TestMethod]
        public void Should_ignore_duplicates_added_to_a_hash_set()
        {
            var listOMoqs = new List<Mock<IJira>>(4);
            var hashSet = new HashSet<V1JiraInfo>();
            for (var i = 0; i < listOMoqs.Capacity; i++)
            {
                var moqInstance = new Mock<IJira>();
                moqInstance.Setup(x => x.InstanceUrl).Returns("http://instance" + i);

                hashSet.Add(new V1JiraInfo("project" + i, "key" + i, "category" + i, 10, moqInstance.Object));
            }

            var dupeInstance = new Mock<IJira>();
            dupeInstance.Setup(x => x.InstanceUrl).Returns("http://instance0");

            var infoDuplicate = new V1JiraInfo("project0", "key0", "category0", 10, dupeInstance.Object);
            hashSet.Add(infoDuplicate);

            hashSet.Count.ShouldEqual(4);
        }
    }
}
