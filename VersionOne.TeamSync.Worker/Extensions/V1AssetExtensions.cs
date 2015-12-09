using System.Collections.Generic;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.V1Connector.Interfaces;

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

        public static string Oid(this IV1Asset asset)
        {
            return string.Format("{0}:{1}", asset.AssetType, asset.ID);
        }
    }
}