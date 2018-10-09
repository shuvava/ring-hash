using System;
using System.Security.Cryptography;
using System.Text;


namespace RingHashUnitTest
{
    public class StrToIntHashAlgorithm: HashAlgorithm
    {
        private int _value;
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            var str = Encoding.UTF8.GetString(array);
            _value = 0;

            foreach (var s in str.Split(" "))
            {
                _value += int.Parse(s);
            }
        }


        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_value);
        }


        public override void Initialize()
        {
        }
    }
}
