using System;

namespace common.Models
{
    public class EventThread
    {
        public int Hash { get; set; }
        public int WorkerId { get; set; }
        public DateTime ThreadCheckpoint { get; set; }
    }
}
