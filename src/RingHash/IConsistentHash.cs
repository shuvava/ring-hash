using System.Collections.Generic;


namespace RingHash
{
    public interface IConsistentHash<in TNode>
    {
        uint ReplicasCount { get; }
        void AddNode(TNode node);
        int GetShardForKey(string key);
        void RemoveNode(TNode node);
    }
}
