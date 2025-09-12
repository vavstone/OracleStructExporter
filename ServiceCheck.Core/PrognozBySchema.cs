using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class PrognozBySchema
    {

        public string DbId { get; set; }
        public string UserName { get; set; }
        
        public double? OjectsCount { get; set; }
        public double? DurationsInMinutes { get; set; }

        public static DateTime? CommonPrognozEnd(List<PrognozBySchema> prognozItems, DateTime fromTime)
        {
            double? commonDuration = null;
            foreach (var duration in prognozItems.Where(c => c.DurationsInMinutes != null)
                         .Select(c => c.DurationsInMinutes.Value))
            {
                if (commonDuration == null) commonDuration = 0;
                commonDuration += duration;
            }
            if (commonDuration != null)
                return fromTime.AddMinutes(commonDuration.Value);
            return null;
        }
    }
}