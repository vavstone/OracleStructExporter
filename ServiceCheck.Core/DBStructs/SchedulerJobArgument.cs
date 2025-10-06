namespace ServiceCheck.Core.DBStructs
{
    public class SchedulerJobArgument
    {
        public string Owner { get; set; }
        public string JobName { get; set; }
        public string ArgumentName { get; set; }
        public int ArgumentPosition { get; set; }
        public string Value { get; set; }
    }
}
