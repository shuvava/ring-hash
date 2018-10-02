namespace RingHash
{
    public interface IConsistentHash<TNode>
    {
        uint ReplicasCount { get; }
        void AddNode(TNode node);
        TNode GetShardForKey(string key);
        void RemoveNode(TNode node);
    }
}
