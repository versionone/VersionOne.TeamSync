namespace VersionOne.JiraConnector.Connector
{
    public sealed class JiraResource
    {
        public static readonly JiraResource Issue = new JiraResource("issue");
        public static readonly JiraResource Comment = new JiraResource("comment");

        private JiraResource(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}