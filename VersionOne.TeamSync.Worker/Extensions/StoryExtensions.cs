using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class StoryExtensions
    {
        public static Issue ToIssueWithOnlyNumberAsLabel(this Story story)
        {
            return new Issue()
            {
                Fields = new Fields()
                {
                    Labels = new List<string>() {story.Number}
                }
            };
        }
    }
}
