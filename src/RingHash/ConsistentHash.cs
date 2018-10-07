using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace RingHash
{
    public class ConsistentHash<TNode> : IConsistentHash<TNode>
    {
        private const uint DefaultRingReplicas = 16;

        private readonly HashAlgorithm _hashFunction;
        private readonly IDictionary<uint, TNode> _nodes;
        private readonly IDictionary<uint, uint> _nodesMap;
        private uint[] _ringHashArray;


        public ConsistentHash(HashAlgorithm hashFunction, uint replicasCount = DefaultRingReplicas)
        {
            _hashFunction = hashFunction;
            ReplicasCount = replicasCount;
            //TODO: [vs] make it Concurrent Dictionary
            _nodes = new Dictionary<uint, TNode>();
            _nodesMap = new Dictionary<uint, uint>();
            RingHashes = new List<uint>();
        }


        public IList<uint> RingHashes { get; }

        public IReadOnlyDictionary<uint, TNode> Nodes => (IReadOnlyDictionary<uint, TNode>) _nodes;


        public uint ReplicasCount { get; }


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
                RingHashes.Add(replicas);
            }

            _ringHashArray = RingHashes.OrderBy(i => i).ToArray();
        }


        public bool ContainsNode(TNode node)
        {
            var nodeHash = GetHash(node.ToString());
            return _nodes.ContainsKey(nodeHash);
        }


        public IEnumerable<TNode> GetNodes()
        {
            return _nodes.Values.ToList();
        }


        public TNode GetShardForKey(string key)
        {
            var hash = GetShardHashForKey(key);

            return _nodes[hash];
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


        private uint GetShardHashForKey(string key)
        {
            var hash = GetHash(key);

            return GetShardHashForHash(hash);
        }


        public uint GetShardHashForHash(uint hash)
        {
            var replica = GetNextShard(hash);

            return _nodesMap[replica];
        }


        private uint GetHash(string source)
        {
            var data = _hashFunction.ComputeHash(Encoding.UTF8.GetBytes(source));

            return BitConverter.ToUInt32(data, 0);
        }


        private IEnumerable<uint> GetReplicasHashes(TNode node)
        {
            var hashes = new List<uint>();
            var nodeHash = GetHash(node.ToString());

            foreach (var num in Enumerable.Range(0, (int) ReplicasCount))
            {
                var source = $"{nodeHash} {num:D4}";
                hashes.Add(GetHash(source));
            }

            return hashes;
        }


        public void AddNodes(IEnumerable<TNode> nodes)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }


        private uint GetNextShard(uint keyHash)
        {
            var index = Array.FindIndex(_ringHashArray, w => w > keyHash);

            if (index == -1)
            {
                index = 0;
            }

            return _ringHashArray[index];
        }
    }
}
