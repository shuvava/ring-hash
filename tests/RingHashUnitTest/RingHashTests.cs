using System.Collections.Generic;
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
