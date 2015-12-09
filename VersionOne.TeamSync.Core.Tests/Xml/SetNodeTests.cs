using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.JiraWorker.Extensions;

namespace VersionOne.TeamSync.Core.Tests.Xml
{
    [TestClass]
    public class SetNodeTests
    {
        [TestMethod]
        public void basic_attribute_setter_builds_out_an_asset_payload()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddSetNode("example", "a value");
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset>\r\n  <Attribute act=\"set\" name=\"example\">a value</Attribute>\r\n</Asset>");
        }

        [TestMethod]
        public void basic_attribute_setter_skips_an_empty_value()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddSetNode("example", string.Empty);
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset></Asset>");
        }
    }

    [TestClass]
    public class SetNullableNodeTests
    {
        [TestMethod]
        public void nullable_attribute_setter_builds_out_an_asset_payload()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddNullableSetNode("example", "a value");
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset>\r\n  <Attribute act=\"set\" name=\"example\">a value</Attribute>\r\n</Asset>");
        }

        [TestMethod]
        public void nullable_attribute_setter_creates_an_empty_node()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddNullableSetNode("example", string.Empty);
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset>\r\n  <Attribute act=\"set\" name=\"example\" />\r\n</Asset>");
        }
    }

    [TestClass]
    public class SetRelationNodeTests
    {
        [TestMethod]
        public void relation_attribute_setter_builds_out_an_asset_payload()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddSetRelationNode("example", "a value");
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset>\r\n  <Relation act=\"set\" name=\"example\">\r\n    <Asset idref=\"a value\" />\r\n  </Relation>\r\n</Asset>");
        }

        [TestMethod]
        public void relation_attribute_setter_skips_an_empty_value()
        {
            var xDoc = XDocument.Parse("<Asset></Asset>");
            xDoc.AddSetRelationNode("example", string.Empty);
            var result = xDoc.ToString();
            result.ShouldEqual("<Asset></Asset>");
        }
    }

}
