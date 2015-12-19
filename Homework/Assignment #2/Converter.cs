using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication2
{
    public class ArmenianUnicodeConverter
    {                                                                                                                                                                                                                         //'A'                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  0x57  0x87                                                                                                                         
        private byte[] CodePage ={ 
            0,    1,    2,    3,    4,    5,    6,    7,    8,    9,    10,   11,   12,   13,   14,   15, 
            16,   17,   18,   19,   20,   21,   22,   23,   24,   25,   26,   27,   28,   29,   30,   31, 
            32,   33,   34,   35,   36,   37,   38,   39,   0x87, 41,   42,   43,   44,   45,   46,   47, 
            
            0x5B,   0x5E, 0x31, 0x61, 0x32, 0x62, 0x33, 0x63, 0x34, 0x64, 0x35, 0x65, 0x36, 0x66, 0x37, 0x67,
            0x38, 0x68, 0x39, 0x69, 0x3A, 0x6A, 0x3B, 0x6B, 0x3C, 0x6C, 0x3D, 0x6D, 0x3E, 0x6E, 0x3F, 0x6F, 
            0x40, 0x70, 0x41, 0x71, 0x42, 0x72, 0x43, 0x73, 0x44, 0x74, 0x45, 0x75, 0x46, 0x76, 0x47, 0x77, 
            0x48, 0x78, 0x49, 0x79, 0x4A, 0x7A, 0x4B, 0x7B, 0x4C, 0x7C, 0x4D, 0x7D, 0x4E, 0x7E, 0x4F, 0x7F, 
            0x50, 0x80, 0x51, 0x81, 0x52, 0x82, 0x53, 0x83, 0x54, 0x84, 0x55, 0x85, 0x56, 0x86, 0x5B, 0x5D};

        private static ArmenianUnicodeConverter _Current;
        public static ArmenianUnicodeConverter Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new ArmenianUnicodeConverter();
                }
                return _Current;
            }
        }

        public ArmenianUnicodeConverter()
        {

        }

        public string ConvertStringFrom1252ToUnicode(string Source)
        {
            if (string.IsNullOrEmpty(Source))
            {
                return string.Empty;
            }
            Encoding ansi = Encoding.GetEncoding(1252);
            byte[] ansiString = ansi.GetBytes(Source);
            byte[] unicodeString = new byte[ansiString.Length * 2];
            for (int i = 0, j = 0; i < ansiString.Length; i++, j += 2)
            {
                int x = ansiString[i];
                if (x < 128)
                {
                    unicodeString[j] = Convert.ToByte(x);
                    unicodeString[j + 1] = 0;// Convert.ToByte(x); 
                }
                else
                {
                    x -= 128;
                    unicodeString[j] = CodePage[x];
                    unicodeString[j + 1] = Convert.ToByte(5);
                }
            }
            char[] c = Encoding.Unicode.GetChars(unicodeString);
            return new string(c, 0, c.Length);
        }

       
    }
}
