using System;

namespace worker.Models
{

    public class Node
    {
        public int Id { get; set; }
        public DateTime LastCheckpointTime { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"Node {Id}";
        }
    }
}
