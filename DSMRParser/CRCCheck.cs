
using System;
using System.Collections.Generic;
using System.Text;

namespace DSMRParser
{


    public enum Crc16Mode : ushort
    {
        ARINC_NORMAL = 0XA02B, ARINC_REVERSED = 0xD405, ARINC_REVERSED_RECIPROCAL = 0XD015,
        CCITT_NORMAL = 0X1021, CCITT_REVERSED = 0X8408, CCITT_REVERSED_RECIPROCAL = 0X8810,
        CDMA2000_NORMAL = 0XC867, CDMA2000_REVERSED = 0XE613, CDMA2000_REVERSED_RECIPROCAL = 0XE433,
        DECT_NORMAL = 0X0589, DECT_REVERSED = 0X91A0, DECT_REVERSED_RECIPROCAL = 0X82C4,
        T10_DIF_NORMAL = 0X8BB7, T10_DIF_REVERSED = 0XEDD1, T10_DIF_REVERSED_RECIPROCAL = 0XC5DB,
        DNP_NORMAL = 0X3D65, DNP_REVERSED = 0XA6BC, DNP_REVERSED_RECIPROCAL = 0X9EB2,
        IBM_NORMAL = 0X8005, IBM_REVERSED = 0XA001, IBM_REVERSED_RECIPROCAL = 0XC002,
        OPENSAFETY_A_NORMAL = 0X5935, OPENSAFETY_A_REVERSED = 0XAC9A, OPENSAFETY_A_REVERSED_RECIPROCAL = 0XAC9A,
        OPENSAFETY_B_NORMAL = 0X755B, OPENSAFETY_B_REVERSED = 0XDDAE, OPENSAFETY_B_REVERSED_RECIPROCAL = 0XBAAD,
        PROFIBUS_NORMAL = 0X1DCF, PROFIBUS_REVERSED = 0XF3B8, PROFIBUS_REVERSED_RECIPROCAL = 0X8EE7

    }
    public class Crc16
    {
        readonly ushort[] table = new ushort[256];

        public ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        public Crc16(Crc16Mode mode)
        {
            ushort polynomial = (ushort)mode;
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
    }
}
