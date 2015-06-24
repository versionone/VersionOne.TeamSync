using System.Collections.Generic;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class StoryExtensions
    {
        public static Issue ToIssueWithOnlyNumberAsLabel(this IPrimaryWorkItem primaryWorkItem, IEnumerable<string> preExistingLabels)
        {
            return new Issue()
            {
                Fields = new Fields()
                {
                    Labels = new List<string>(preExistingLabels) { primaryWorkItem.Number }
                }
            };
        }
    }
}