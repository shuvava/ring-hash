namespace RingHashUnitTest
{
    public class Node
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
