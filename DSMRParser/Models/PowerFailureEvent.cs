using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using DSMRParser.Converters;

namespace DSMRParser.Models
{
    public class PowerFailureEvent
    {
        [TypeConverter(typeof(ObisTimestampConverter))]
        public DateTime Timestamp { get; set; }
        [TypeConverter(typeof(ObisDurationConverter))]
        public int Duration { get; set; } = 0;
    }

    public class PowerFailureEventEqualityComparer : IEqualityComparer<PowerFailureEvent>
    {
        public bool Equals(PowerFailureEvent pfe1, PowerFailureEvent pfe2)
        {
            if (pfe2 == null && pfe1 == null)
                return true;
            else if (pfe1 == null || pfe2 == null)
                return false;
            else if (pfe1.Timestamp == pfe2.Timestamp && pfe1.Duration == pfe2.Duration)
                return true;
            else
                return false;
        }

        public int GetHashCode(PowerFailureEvent pfe)
        {
            long hCode = pfe.Timestamp.Ticks ^ pfe.Duration;
            return hCode.GetHashCode();
        }
    }
}
