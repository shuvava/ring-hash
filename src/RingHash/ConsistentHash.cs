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

            if (replicasCount < 1)
            {
                replicasCount = 1;
            }

            ReplicasCount = replicasCount;
            //TODO: [vs] make it Concurrent Dictionary
            _nodes = new Dictionary<uint, TNode>();
            _nodesMap = new Dictionary<uint, uint>();
            RingHashes = new List<uint>();
        }


        public IList<uint> RingHashes { get; }

        public IReadOnlyDictionary<uint, TNode> Nodes => (IReadOnlyDictionary<uint, TNode>) _nodes;


        public uint ReplicasCount { get; }


        public bool AddNode(TNode node)
        {
            var nodeHash = GetHash(node.ToString());

            if (_nodes.ContainsKey(nodeHash))
            {
                return false;
            }

            var replicasHashes = GetReplicasHashes(node);
            _nodes.Add(nodeHash, node);

            foreach (var replicas in replicasHashes)
            {
                _nodesMap.Add(replicas, nodeHash);
                RingHashes.Add(replicas);
            }

            _ringHashArray = RingHashes.OrderBy(i => i).ToArray();

            return true;
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


        public bool RemoveNode(TNode node)
        {
            var nodeHash = GetHash(node.ToString());

            if (!_nodes.ContainsKey(nodeHash))
            {
                return false;
            }

            var replicasHashes = GetReplicasHashes(node);

            foreach (var replicas in replicasHashes)
            {
                if (!_nodesMap.ContainsKey(replicas))
                {
                    continue;
                }

                _nodesMap.Remove(replicas);
                RingHashes.Remove(replicas);
            }

            _nodes.Remove(nodeHash);
            _ringHashArray = RingHashes.OrderBy(i => i).ToArray();

            return true;
        }


        public IEnumerable<Tuple<uint, uint>> GetNodeKeyRange(TNode node)
        {
            var nodeHash = GetHash(node.ToString());

            if (!_nodes.ContainsKey(nodeHash))
            {
                return Enumerable.Empty<Tuple<uint, uint>>();
            }

            var replicasHashes = GetReplicasHashes(node);
            var result = new List<Tuple<uint, uint>>();

            foreach (var hash in replicasHashes)
            {
                var nextHash = GetNextShard(hash);

                if (nextHash > hash)
                {
                    result.Add(new Tuple<uint, uint>(hash + 1, nextHash));
                }
                else
                {
                    result.Add(new Tuple<uint, uint>(hash + 1, uint.MaxValue));
                    result.Add(new Tuple<uint, uint>(uint.MinValue, nextHash));
                }
            }

            return result;
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
