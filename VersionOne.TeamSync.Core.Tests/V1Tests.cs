using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.JiraWorker.Extensions;
using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class v1_bits
    {
        protected IV1 V1;
        protected Mock<IV1Connector> MockV1Connector;

        protected User Assignee;
        protected Member Member;

        protected virtual void BuildContext()
        {
            MockV1Connector = new Mock<IV1Connector>();

            Assignee = new User
            {
                displayName = "Administrator",
                name = "admin",
                emailAddress = "admin@versionone.com"
            };

            Member = Assignee.ToV1Member();
        }

        protected bool MembersAreEquals(Member member)
        {
            return
                Member.Name.Equals(member.Name) &&
                Member.Nickname.Equals(member.Nickname) &&
                Member.Email.Equals(member.Email);
        }
    }

    [TestClass]
    public class when_assignee_does_not_exists_in_v1 : v1_bits
    {
        private const string CreatedMember = @"<Asset href=""/VersionOne/rest-1.v1/Data/Member/20"" id=""Member:20""></Asset>";

        [TestInitialize]
        public async void Context()
        {
            BuildContext();

            MockV1Connector.Setup(
                x => x.Query("Member", It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<Func<XElement, Member>>()))
                .ReturnsAsync(new List<Member>());
            MockV1Connector.Setup(
                x =>
                    x.Post(It.Is<Member>(m => MembersAreEquals(m)),
                        It.Is<XDocument>(doc => doc.ToString().Equals(Assignee.ToV1Member().CreatePayload().ToString()))))
                .ReturnsAsync(XDocument.Parse(CreatedMember));

            V1 = new V1(MockV1Connector.Object);
            await V1.SyncMemberFromJiraUser(Assignee);
        }

        [TestMethod]
        public void should_call_create_member_just_once()
        {
            MockV1Connector.Verify(
                x =>
                    x.Post(It.Is<Member>(m => MembersAreEquals(m)),
                        It.Is<XDocument>(doc => doc.ToString().Equals(Assignee.ToV1Member().CreatePayload().ToString()))),
                Times.Once);
        }
    }

    [TestClass]
    public class when_assignee_exists_in_v1_and_it_matches_equals_true : v1_bits
    {
        [TestInitialize]
        public async void Context()
        {
            BuildContext();

            MockV1Connector.Setup(
                x => x.Query("Member", It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<Func<XElement, Member>>()))
                .ReturnsAsync(new List<Member> { Assignee.ToV1Member() });

            V1 = new V1(MockV1Connector.Object);
            await V1.SyncMemberFromJiraUser(Assignee);
        }

        [TestMethod]
        public void should_create_a_post_to_create_member()
        {
            MockV1Connector.Verify(x => x.Post(It.IsAny<Member>(), It.IsAny<XDocument>()), Times.Never);
        }
    }

    [TestClass]
    public class when_assignee_exists_in_v1_and_it_matches_equals_false : v1_bits
    {
        private const string CreatedMember = @"<Asset href=""/VersionOne/rest-1.v1/Data/Member/20"" id=""Member:20""></Asset>";

        [TestInitialize]
        public async void Context()
        {
            BuildContext();

            var modifiedAssignee = new User
            {
                displayName = "TeamSync User",
                name = "teamsyncuser",
                emailAddress = "teamsyncuser@versionone.com"
            };

            MockV1Connector.Setup(
                x => x.Query("Member", It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<Func<XElement, Member>>()))
                .ReturnsAsync(new List<Member> { modifiedAssignee.ToV1Member() });
            MockV1Connector.Setup(
                x =>
                    x.Post(It.Is<Member>(m => MembersAreEquals(m)),
                        It.Is<XDocument>(
                            doc => doc.ToString().Equals(Assignee.ToV1Member().CreateUpdatePayload().ToString()))))
                .ReturnsAsync(XDocument.Parse(CreatedMember));

            V1 = new V1(MockV1Connector.Object);
            await V1.SyncMemberFromJiraUser(Assignee);
        }

        [TestMethod]
        public void should_create_a_post_to_update_member()
        {
            MockV1Connector.Verify(
                x =>
                    x.Post(It.Is<Member>(m => MembersAreEquals(m)),
                        It.Is<XDocument>(
                            doc => doc.ToString().Equals(Assignee.ToV1Member().CreateUpdatePayload().ToString()))),
                Times.Once);
        }
    }

    [TestClass]
    public class when_creating_a_story : v1_bits
    {
        private XDocument _xDocument;
        private Story _createdStory;
        private readonly Story _story = new Story { Name = "Name" };

        [TestInitialize]
        public async void Context()
        {
            BuildContext();

            _xDocument = XDocument.Parse("<Asset href=\"/VersionOne/rest-1.v1/Data/Story/1126/2089\" id=\"Story:1126:2089\"><Attribute name=\"Name\">Example 2</Attribute><Relation name=\"Scope\"><Asset href=\"/VersionOne/rest-1.v1/Data/Scope/0\" idref=\"Scope:0\" /></Relation></Asset>");
            MockV1Connector.Setup(x => x.Post(_story, It.IsAny<XDocument>())).ReturnsAsync(_xDocument);

            _story.Number.ShouldBeNull();
            _story.ID.ShouldBeNull();

            V1 = new V1(MockV1Connector.Object);
            _createdStory = await V1.CreateStory(_story);
        }

        [TestMethod]
        public void should_post_to_v1()
        {
            MockV1Connector.Verify(x => x.Post(_story, It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_set_the_story_id()
        {
            _createdStory.ID.ShouldNotBeEmpty();
            _createdStory.ID.ShouldEqual("1126");
        }
    }
}
