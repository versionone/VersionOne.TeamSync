using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class EqualityTests_Epic
    {
        private Epic _v1Epic;
        private Issue _secondEpic;

        [TestInitialize]
        public void Context()
        {
            _v1Epic = new Epic()
            {
                Name = "Name",
                Description = "Description",
                Reference = "Reference"
            };
            _secondEpic = new Issue()
            {
                Key = "Reference",
                Fields = new Fields()
                {
                    Summary = "Name",
                    Description = "Description"
                }
            };
        }

        [TestMethod]
        public void santity_check()
        {
            _v1Epic.ItMatches(_secondEpic).ShouldBeTrue();
        }

        [TestMethod]
        public void different_description_means_update()
        {
            _v1Epic.Description = "Something else";
            _v1Epic.ItMatches(_secondEpic).ShouldBeFalse();
        }

        [TestMethod]
        public void different_name_means_update()
        {
            _v1Epic.Name = "a new name";
            _v1Epic.ItMatches(_secondEpic).ShouldBeFalse();
        }

        [TestMethod]
        public void different_reference_means_update()
        {
            _v1Epic.Reference = "something else";
            _v1Epic.ItMatches(_secondEpic).ShouldBeFalse();
        }

        [TestMethod]
        public void other_items_we_ignore()
        {
            _v1Epic.ScopeName = "a scope";
            _v1Epic.ItMatches(_secondEpic).ShouldBeTrue();
        }

    }

    [TestClass]
    public class EqualityTests_Story
    {
        private Story _story;
        private Issue _jiraStory;
        private string _reference = "JK-10";

        [TestInitialize]
        public void Context()
        {
            _story = new Story()
            {
                Name = "Name",
                Description = "Description",
                Reference = _reference,
                Estimate = "5",
                ToDo = "10",
                SuperNumber = "E-1000"
            };

            _jiraStory = new Issue()
            {
                Key = _reference,
                RenderedFields = new RenderedFields()
                {
                    Description = "Description"
                },
                Fields = new Fields()
                {
                    Summary = "Name",
                    EpicLink = "E-1000",
                    TimeTracking = new TimeTracking()
                    {
                        RemainingEstimateSeconds = 36000,
                    },
                    StoryPoints = "5"
                }
            };
        }

        [TestMethod]
        public void santity_check()
        {
            _jiraStory.ItMatchesStory(_story);
        }

        [TestMethod]
        public void different_description_means_update()
        {
            _story.Description = "Something else";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }

        [TestMethod]
        public void different_name_means_update()
        {
            _story.Name = "a new name";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }

        [TestMethod]
        public void different_reference_means_update()
        {
            _story.Reference = "something else";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }

        [TestMethod]
        public void different_estimate_means_update()
        {
            _story.Estimate = "something else";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }

        [TestMethod]
        public void different_todo_means_update()
        {
            _story.ToDo = "something else";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }

        [TestMethod]
        public void different_supernumber_means_update()
        {
            _story.SuperNumber = "something else";
            _jiraStory.ItMatchesStory(_story).ShouldBeFalse();
        }


        [TestMethod]
        public void other_items_we_ignore()
        {
            _story.ScopeName = "a scope";
            _jiraStory.ItMatchesStory(_story).ShouldBeTrue();
        }
    }

    [TestClass]
    public class EqualityTests_Defect
    {
        private Defect _defect;
        private Issue _jiraDefect;
        private string _reference = "JK-10";

        [TestInitialize]
        public void Context()
        {
            _defect = new Defect()
            {
                Name = "Name",
                Description = "Description",
                Reference = _reference,
                Estimate = "5",
                ToDo = "10",
                SuperNumber = "E-1000"
            };

            _jiraDefect = new Issue()
            {
                Key = _reference,
                RenderedFields = new RenderedFields()
                {
                    Description = "Description"
                },
                Fields = new Fields()
                {
                    Summary = "Name",
                    EpicLink = "E-1000",
                    TimeTracking = new TimeTracking()
                    {
                        RemainingEstimateSeconds = 36000,
                    },
                    StoryPoints = "5"
                }
            };

        }

        [TestMethod]
        public void santity_check()
        {
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeTrue();
        }

        [TestMethod]
        public void different_description_means_update()
        {
            _defect.Description = "Something else";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }

        [TestMethod]
        public void different_name_means_update()
        {
            _defect.Name = "a new name";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }

        [TestMethod]
        public void different_reference_means_update()
        {
            _defect.Reference = "something else";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }

        [TestMethod]
        public void different_estimate_means_update()
        {
            _defect.Estimate = "something else";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }

        [TestMethod]
        public void different_todo_means_update()
        {
            _defect.ToDo = "something else";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }

        [TestMethod]
        public void different_super_means_update()
        {
            _defect.SuperNumber = "something else";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeFalse();
        }


        [TestMethod]
        public void other_items_we_ignore()
        {
            _defect.ScopeName = "a scope";
            _jiraDefect.ItMatchesDefect(_defect).ShouldBeTrue();
        }

    }

}
