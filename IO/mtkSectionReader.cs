using System;
using System.Linq;
using System.IO;

namespace rbootimg.IO
{
    class mtkSectionReader
    {
        const uint HEADER_SIZE = 512;
        public mtkSectionHeader cHdr;
        byte[] cData;

        public mtkSectionReader(byte[] data)
        {
            if (data.Length < HEADER_SIZE)
                throw new FileFormatException();
            cData = data;
            cHdr = mtkSectionHeader.FromBytes(cData);

            //Check for magic
            if (cHdr.magic != mtkSectionHeader.SECTION_MAGIC)
                throw new FileFormatException();

            //Check for filesize
            if (data.Length < HEADER_SIZE + cHdr.section_size)
                throw new FileFormatException();
        }

        public static bool IsMtkSection(byte[] data)
        {
            if (data.Length < mtkSectionHeader.SECTION_MAGIC_SIZE)
                return false;
            byte[] magic = new byte[mtkSectionHeader.SECTION_MAGIC_SIZE];
            Array.Copy(data, magic, mtkSectionHeader.SECTION_MAGIC_SIZE);
            return BitConverter.ToUInt32(magic, 0) == mtkSectionHeader.SECTION_MAGIC;
        }

        public byte[] GetSection()
        {
            byte[] arr = new byte[cHdr.section_size];
            Array.Copy(cData, HEADER_SIZE, arr, 0, cHdr.section_size);
            return arr;
        }
    }
}
