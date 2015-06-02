namespace VersionOne.TeamSync.JiraConnector
{
    public class JqOperator
    {
        public static JqOperator Equals(string property, string value)
        {
            return new JqOperator()
            {
                Property = property,
                Value = value,
                Operator = "="
            };
        }

        public static JqOperator NotEquals(string property, string value)
        {
            return new JqOperator()
            {
                Property = property,
                Value = value,
                Operator = "!="
            };
        }

        public override string ToString()
        {
            return Property + Operator + Value;
        }

        public string Property { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
