using System;
using System.Collections.Generic;
using System.Text;

namespace WakeOnLan
{
    public class MacAddress
    {

        #region Variables
        private byte[] macAddr = new byte[6];
        #endregion

        #region Statics

        private static bool IsHexDigit(Char c)
        {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            c = Char.ToUpper(c);
            numChar = Convert.ToInt32(c);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }

        public static MacAddress Parse(byte[] address)
        {
            MacAddress value = new MacAddress(address);
            return value;
        }

        public static MacAddress Parse(string macString)
        {
            int discarded = 0;
            string newString = "";
            char c;

            // remove all none A-F, 0-9, characters
            for (int i = 0; i < macString.Length; i++)
            {
                c = macString[i];
                if (IsHexDigit(c))
                    newString += c;
                else
                    discarded++;
            }

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hex = new String(new Char[] { newString[j], newString[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return MacAddress.Parse(bytes);
        }

        private static byte HexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }

        #endregion

        #region Constructors
        public MacAddress(byte[] address)
        {
            macAddr = address;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return macAddr[0].ToString("X2") + "-" +
                    macAddr[1].ToString("X2") + "-" +
                    macAddr[2].ToString("X2") + "-" +
                    macAddr[3].ToString("X2") + "-" +
                    macAddr[4].ToString("X2") + "-" +
                    macAddr[5].ToString("X2");
        }

        public override bool Equals(System.Object obj)
        {
            return ((MacAddress)obj).ToString() == this.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Public Properties
        public byte this[int index]
        {
            get
            {
                return macAddr[index];
            }
            set
            {
                macAddr[index] = value;
            }
        }
        #endregion
    }
}
