using System.Collections.Generic;


namespace RingHash
{
    public interface IConsistentHash<TNode>
    {
        uint ReplicasCount { get; }
        void AddNode(TNode node);
        bool ContainsNode(TNode node);
        IEnumerable<TNode> GetNodes();
        TNode GetShardForKey(string key);
        void RemoveNode(TNode node);
    }
}
