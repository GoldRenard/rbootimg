using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using rbootimg.Utils;

namespace rbootimg.IO
{
  public static class ImgUnpacker
  {
    public static int Unpack(byte[] data, string Destination)
    {
      if (Directory.Exists(Destination))
        return 1;
      else
      {
        ImgReader iReader;
        try { iReader = new ImgReader(data); }
        catch { return -1; }

        Directory.CreateDirectory(Destination);
        ImgUnpacker.UnpackSections(iReader, Destination);
      }
      return 0;
    }

    public static int Unpack(string fPath, string Destination)
    {
      return Unpack(File.ReadAllBytes(fPath), Destination);
    }

    public static void UnpackSections(ImgReader Reader, string Destination)
    {
      Console.WriteLine("UNPACKING SECTIONS:");
      Console.WriteLine();

      Console.Write("Saving old image header...");
      File.WriteAllBytes(Path.Combine(Destination, "image.hdr"), Reader.cHdr.ToBytes());
      ConsoleEx.WriteLine(ConsoleColor.Green, " OK.");

      Console.Write("Unpacking Kernel...");
      WriteSection(Reader.GetKernelBytes(), Path.Combine(Destination, "zImage"));
      Console.Write("Unpacking Ramdisk...");
      WriteSection(Reader.GetRamdiskBytes(), Path.Combine(Destination, "ram_disk.gz"));
      Console.Write("Unpacking Second Stage...");
      WriteSection(Reader.GetSecStateBytes(), Path.Combine(Destination, "second.bin"));
      ConsoleEx.Write(ConsoleColor.Green, "Done!");
    }

    static void WriteSection(byte[] sData, string Destination)
    {
      if (sData.Length == 0)
      {
        ConsoleEx.WriteLine(ConsoleColor.Yellow, " SKIP.");
        return;
      }
      if (mtkSectionReader.IsMtkSection(sData))
      {
        mtkSectionReader mtkSection = new mtkSectionReader(sData);
        Console.Write(" MTK Section called \"{0}\".", mtkSection.cHdr.name);
        File.WriteAllBytes(Destination + ".mtkhdr", mtkSection.cHdr.ToBytes());
        File.WriteAllBytes(Destination, mtkSection.GetSection());
      }
      else
        File.WriteAllBytes(Destination, sData);
      ConsoleEx.WriteLine(ConsoleColor.Green, " OK.");
    }
  }
}
