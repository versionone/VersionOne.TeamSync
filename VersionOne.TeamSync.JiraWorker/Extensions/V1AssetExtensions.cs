using System.Collections.Generic;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.JiraWorker.Extensions
{
    public static class V1AssetExtensions
    {
        public static Issue ToIssueWithOnlyNumberAsLabel(this IPrimaryWorkItem primaryWorkItem, IEnumerable<string> preExistingLabels)
        {
            return new Issue
            {
                Fields = new Fields
                {
                    Labels = new List<string>(preExistingLabels) { primaryWorkItem.Number }
                }
            };
        }
    }
}