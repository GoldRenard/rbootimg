using System;
using System.Linq;
using System.IO;

namespace rbootimg.IO
{
    public class ImgReader
    {
        public ImgHeader cHdr;
        byte[] cData;

        public ImgReader(string Path)
        {
            try { Init(File.ReadAllBytes(Path)); }
            catch (Exception ex) { throw ex; }
        }

        public ImgReader(byte[] data)
        {
            try { Init(data); }
            catch (Exception ex) { throw ex; }
        }

        private void Init(byte[] data)
        {
            if (data.Length < ImgHeader.HEADER_SIZE)
                throw new FileFormatException();

            cData = data;
            cHdr = ImgHeader.FromBytes(cData);

            //Check for magic
            if (!cHdr.magic.SequenceEqual(ImgHeader.BOOT_MAGIC))
                throw new FileFormatException();

            //Check for file size
            if (data.Length < ImgHeader.HEADER_SIZE + cHdr.kernel_size + cHdr.ramdisk_size + cHdr.second_size)
                throw new FileFormatException();

            cHdr.PrintInfo();
            Console.WriteLine();
        }

        public byte[] GetKernelBytes()
        {
            byte[] buf = new byte[cHdr.kernel_size];
            Array.Copy(cData, cHdr.page_size, buf, 0, cHdr.kernel_size);
            return buf;
        }

        public byte[] GetRamdiskBytes()
        {
            byte[] buf = new byte[cHdr.ramdisk_size];
            Array.Copy(cData, (1 + cHdr.GetKernelPages()) * cHdr.page_size, buf, 0, cHdr.ramdisk_size);
            return buf;
        }

        public byte[] GetSecStateBytes()
        {
            byte[] buf = new byte[cHdr.second_size];
            Array.Copy(cData, (1 + cHdr.GetKernelPages() + cHdr.GetRamdiskPages()) * cHdr.page_size, buf, 0, cHdr.second_size);
            return buf;
        }
    }
}
