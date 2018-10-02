using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace RingHash
{
    public class ConsistentHash<TNode> : IConsistentHash<TNode> where TNode: INode 
    {
        public const uint RingSize = uint.MaxValue;
        private const uint DefaultRingReplicas = 16;

        private readonly HashAlgorithm _hashFunction;
        private readonly IDictionary<int, TNode> _nodes;
        private readonly IDictionary<int, int> _nodesMap;
        private readonly IList<int> _ringHash;
        private int[] _ringHashArray;


        public ConsistentHash(HashAlgorithm hashFunction, uint replicasCount = DefaultRingReplicas)
        {
            _hashFunction = hashFunction;
            ReplicasCount = replicasCount;
            //TODO: [vs] make it Concurent Dictionary 
            _nodes = new Dictionary<int, TNode>();
            _nodesMap = new Dictionary<int, int>();
            _ringHash = new List<int>();
        }


        public uint ReplicasCount { get; }


        private int GetHash(string source)
        {
            var data = _hashFunction.ComputeHash(Encoding.UTF8.GetBytes(source));

            return BitConverter.ToInt32(data, 0);
        }

        private IEnumerable<int> GetReplicasHashes(TNode node)
        {
            var hashes = new List<int>();

            foreach (var num in Enumerable.Range(0, (int)ReplicasCount))
            {
                var source = $"{node.ToString()} {num}";
                hashes.Add(GetHash(source));
            }

            return hashes;
        }


        public void AddNode(TNode node)
        {
            var nodeHash = GetHash(node.ToString());

            if (_nodes.ContainsKey(nodeHash))
            {
                return;
            }

            var replicasHashes = GetReplicasHashes(node);
            _nodes.Add(nodeHash, node);

            foreach (var replicas in replicasHashes)
            {
                _nodesMap.Add(replicas, nodeHash);
                _ringHash.Add(replicas);
            }

            _ringHashArray = _ringHash.OrderBy(i => i).ToArray();
        }


        public void AddNodes(IEnumerable<TNode> nodes)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }


        public int GetNextShard(int keyHash)
        {
            var index = Array.FindIndex(_ringHashArray, w => w > keyHash);

            if (index == -1)
            {
                index = 0;
            }

            return _ringHashArray[index];
        }


        public int GetPrevShard(int keyHash)
        {
            var index = Array.FindIndex(_ringHashArray, w => w < keyHash);

            if (index == -1)
            {
                index = _ringHashArray.Length - 1;
            }

            return _ringHashArray[index];
        }


        public int GetShardForKey(string key)
        {
            var hash = GetHash(key);

            return GetNextShard(hash);
        }


        public void RemoveNode(TNode node)
        {
            var nodeHash = GetHash(node.ToString());
            if (!_nodes.ContainsKey(nodeHash))
            {
                return;
            }
            var replicasHashes = GetReplicasHashes(node);

            foreach (var replicas in replicasHashes)
            {
                if (_nodesMap.ContainsKey(replicas))
                {
                    _nodesMap.Remove(replicas);
                }
            }

            _nodes.Remove(nodeHash);
        }
    }
}
