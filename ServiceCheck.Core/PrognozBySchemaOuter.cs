using System;

namespace ServiceCheck.Core
{
    public class PrognozBySchemaOuter
    {

        public string DbId { get; set; }
        public string UserName { get; set; }
        public string DbLink { get; set; }
        public string DbFolder { get; set; }
        
        public double? OjectsCount { get; set; }
        public double? DurationsInMinutes { get; set; }

        public static DateTime? CommonPrognozEnd(PrognozBySchemaOuter prognozItems, DateTime fromTime)
        {
            if (prognozItems.DurationsInMinutes != null)
            {
                return fromTime.AddMinutes(prognozItems.DurationsInMinutes.Value);
            }
            return null;
        }
    }
}