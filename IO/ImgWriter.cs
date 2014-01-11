using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;

namespace rbootimg.IO
{
    public class ImgWriter
    {
        const byte SHA_DIGEST_SIZE = 20;

        public static bool Write(ImgHeader hdr, byte[] kernel, byte[] ramdisk, byte[] second, string OutFile, ref long sz)
        {
            if (hdr == null)
              return false;

            //Setting new sizes
            hdr.kernel_size = (kernel != null) ? (uint)kernel.Length : 0;
            hdr.ramdisk_size = (ramdisk != null) ? (uint)ramdisk.Length : 0;
            hdr.second_size = (second != null) ? (uint)second.Length : 0;

            //Generating data to calc SHA1
            List<byte> data_to_hash = new List<byte>();

            data_to_hash.AddRange(kernel);
            data_to_hash.AddRange(BitConverter.GetBytes(hdr.kernel_size));
            data_to_hash.AddRange(ramdisk);
            data_to_hash.AddRange(BitConverter.GetBytes(hdr.ramdisk_size));
            if (second != null)
                data_to_hash.AddRange(second);
            data_to_hash.AddRange(BitConverter.GetBytes(hdr.second_size));

            SHA1 sha = new SHA1CryptoServiceProvider();

            byte[] sha_bytes = sha.ComputeHash(data_to_hash.ToArray(), 0, data_to_hash.Count);

            hdr.id = new byte[32];
            Array.Copy(sha_bytes, 0, hdr.id, 0,
                SHA_DIGEST_SIZE > hdr.id.Length ? hdr.id.Length : SHA_DIGEST_SIZE);
            byte[] hdr_bytes = hdr.ToBytes();

            try
            {
              FileStream fs = File.Open(OutFile, FileMode.Create, FileAccess.Write);

              fs.Write(hdr_bytes, 0, hdr_bytes.Length);
              write_padding(ref fs, hdr.page_size, (uint)hdr_bytes.Length);

              fs.Write(kernel, 0, kernel.Length);
              write_padding(ref fs, hdr.page_size, (uint)kernel.Length);

              fs.Write(ramdisk, 0, ramdisk.Length);
              write_padding(ref fs, hdr.page_size, (uint)ramdisk.Length);

              if (second != null)
              {
                fs.Write(second, 0, second.Length);
                write_padding(ref fs, hdr.page_size, (uint)second.Length);
              }
              sz = fs.Length;
              fs.Dispose();
            }
            catch { return false; }
            return true;
        }

        static void write_padding(ref FileStream fs, uint pagesize, uint itemsize)
        {
            uint pagemask = pagesize - 1;
            uint count;

            if ((itemsize & pagemask) == 0)
                return;

            count = pagesize - (itemsize & pagemask);
            byte[] padding = new byte[16384];

            fs.Write(padding, 0, (int)count);
        }
    }
}
