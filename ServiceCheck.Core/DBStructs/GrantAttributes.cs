namespace ServiceCheck.Core
{
    public class GrantAttributes
    {
        public string ObjectSchema { get; set; }
        public string ObjectName { get; set; }
        public string Grantee { get; set; }
        public string Privilege { get; set; }
        public string Grantable { get; set; }
        public string Hierarchy { get; set; }
    }
}
