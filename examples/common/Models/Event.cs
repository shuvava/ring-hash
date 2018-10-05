using System;


namespace common.Models
{
    public class Event
    {
        public DateTime CreateTime { get; set; }
        public int Id { get; set; }
        public DateTime EventTime { get; set; }
        public int UserId { get; set; }
        public int TransactionId { get; set; }
        public string EventData { get; set; }
    }
}
