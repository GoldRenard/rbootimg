using System;
using System.Collections.Generic;
using System.IO;

namespace rbootimg.IO
{
    public static class mtkSectionWriter
    {
        public static bool Write(mtkSectionHeader hdr, byte[] data, string outFile)
        {
            byte[] bytes = GetBytes(hdr, data);
            if (bytes != null)
            {
                try { File.WriteAllBytes(outFile, bytes); }
                catch { return false; }
                return true;
            }
            return false;
        }

        public static byte[] GetBytes(mtkSectionHeader hdr, byte[] data)
        {
            hdr.section_size = (uint)data.Length;
            List<byte> outbytes = new List<byte>();
            outbytes.AddRange(hdr.ToBytes());
            outbytes.AddRange(data);
            return outbytes.ToArray();
        }
    }
}
