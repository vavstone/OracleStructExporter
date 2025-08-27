namespace OracleStructExporter.Core
{
    public class SequenceAttributes
    {
        public string SequenceName { get; set; }
        public double? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int IncrementBy { get; set; }
        public string CycleFlag { get; set; }
        public string OrderFlag { get; set; }
        public int CacheSize { get; set; }
        public double LastNumber { get; set; }
    }
}
