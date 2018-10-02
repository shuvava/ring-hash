using RingHash;


namespace RingHashUnitTest
{
    public class Node : INode
    {
        public Node(int value)
        {
            Value = value;
        }
        public int Value { get; }


        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
