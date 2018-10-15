using System;


namespace common.Models
{
    public class EventThread
    {
        public int Hash { get; set; }
        public int WorkerId { get; set; }
        public DateTime Checkpoint { get; set; }
        public DateTime LockExpirationTime { get; set; }
    }
}
