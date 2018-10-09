using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using RingHash;

using Xunit;


namespace RingHashUnitTest
{
    public class RingHashTests
    {
        public RingHashTests()
        {
            _hashFuncMoq = new StrToIntHashAlgorithm();
        }


        private readonly HashAlgorithm _hashFuncMoq;


        [Fact]
        public void GetNextShard_SimpleTwoShardTest()
        {
            // arrange
            var rHash = new ConsistentHash<Node>(_hashFuncMoq, 1);
            var first = new Node(3);
            var second = new Node(6);
            var nodes = new List<Node> { first, second };
            rHash.AddNodes(nodes);
            // act
            var shard1 = rHash.GetShardHashForHash(rHash.Nodes.Keys.First());
            var node1 = rHash.Nodes[shard1];
            var shard2 = rHash.GetShardHashForHash(rHash.Nodes.Keys.Skip(1).First());
            var node2 = rHash.Nodes[shard2];
            // assert
            Assert.Equal(second, node1);
            Assert.Equal(first, node2);
        }

        [Fact]
        public void GetNextShard_hash_in_the_middle_shard()
        {
            // arrange
            var rHash = new ConsistentHash<Node>(_hashFuncMoq, 2);
            var first = new Node(3);
            var second = new Node(6);
            var third = new Node(9);
            rHash.AddNodes(new List<Node> {first, second, third});
            // act
            var shard = rHash.GetShardHashForHash(5);
            var node = rHash.Nodes[shard];
            // assert
            Assert.Equal(second, node);
        }


        [Fact]
        public void GetNextShard_hash_less_min_shard()
        {
            // arrange
            var rHash = new ConsistentHash<Node>(_hashFuncMoq, 2);
            var first = new Node(3);
            var second = new Node(6);
            var third = new Node(9);
            rHash.AddNodes(new List<Node> {first, second, third});
            // act
            var shard = rHash.GetShardHashForHash(2);
            var node = rHash.Nodes[shard];
            // assert
            Assert.Equal(first, node);
        }


        [Fact]
        public void GetNextShard_hash_more_max_shard()
        {
            // arrange
            var rHash = new ConsistentHash<Node>(_hashFuncMoq, 2);
            var first = new Node(3);
            var second = new Node(6);
            var third = new Node(9);
            rHash.AddNodes(new List<Node> {first, second, third});
            // act
            var shard = rHash.GetShardHashForHash(12);
            var node = rHash.Nodes[shard];
            // assert
            Assert.Equal(first, node);
        }
    }
}
