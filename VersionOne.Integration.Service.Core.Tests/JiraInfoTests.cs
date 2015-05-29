using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.Integration.Service.Worker.Domain;

namespace VersionOne.Integration.Service.Worker.Tests
{
    [TestClass]
    public class JiraInfoTests
    {
        [TestMethod]
        public void Should_ignore_duplicates_added_to_a_hash_set()
        {
            var listOMoqs = new List<Mock<IJira>>(4);

            for (var i = 0; i < listOMoqs.Capacity; i++)
            {
                var moqInstance = new Mock<IJira>();
                moqInstance.Setup(x => x.InstanceUrl).Returns("http://instance" + i);
                listOMoqs.Add(moqInstance);
            }

            var info0 = new V1JiraInfo("project0", "key0", listOMoqs[0].Object);
            var info1 = new V1JiraInfo("project1", "key1", listOMoqs[1].Object);
            var info2 = new V1JiraInfo("project2", "key2", listOMoqs[2].Object);
            var info3 = new V1JiraInfo("project3", "key3", listOMoqs[3].Object);

            var dupeInstance = new Mock<IJira>();
            dupeInstance.Setup(x => x.InstanceUrl).Returns("http://instance0");

            var infoDuplicate = new V1JiraInfo("project0", "key0", dupeInstance.Object);

            var hashSet = new HashSet<V1JiraInfo>()
            {
                info0,
                info1,
                info2,
                info3,
                infoDuplicate
            };

            hashSet.Count.ShouldEqual(4);
        }
    }
}
