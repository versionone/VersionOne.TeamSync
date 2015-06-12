using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class when_creating_a_story
    {
        private V1 _v1;
        private Story _story = new Story() {Name = "Name"};
        private Story _createdStory;
        private Mock<IV1Connector> _mockV1;
        private XDocument _xDocument;

        [TestInitialize]
        public async void Context()
        {
            _mockV1 = new Mock<IV1Connector>();
            _xDocument = XDocument.Parse("<Asset href=\"/VersionOne/rest-1.v1/Data/Story/1126/2089\" id=\"Story:1126:2089\"><Attribute name=\"Name\">Example 2</Attribute><Relation name=\"Scope\"><Asset href=\"/VersionOne/rest-1.v1/Data/Scope/0\" idref=\"Scope:0\" /></Relation></Asset>");
            _mockV1.Setup(x => x.Post(_story, It.IsAny<XDocument>()))
                .ReturnsAsync(_xDocument);

            _story.Number.ShouldBeNull();
            _story.ID.ShouldBeNull();

            _v1 = new V1(_mockV1.Object, new TimeSpan());
            _createdStory = await _v1.CreateStory(_story);
        }

        [TestMethod]
        public void should_post_to_v1()
        {
            _mockV1.Verify(x => x.Post(_story, It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_set_the_story_id()
        {
            _createdStory.ID.ShouldNotBeEmpty();
            _createdStory.ID.ShouldEqual("1126");
        }
    
    }
}
