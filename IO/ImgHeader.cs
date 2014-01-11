using System;
using System.Text;
using System.Runtime.InteropServices;
using rbootimg.Utils;

namespace rbootimg.IO
{
  [StructLayout(LayoutKind.Explicit, Size = 1632, Pack = 1, CharSet = CharSet.Ansi)]
  public class ImgHeader
  {
    public const uint HEADER_SIZE = 1632;
    public static readonly byte[] BOOT_MAGIC = { 0x41, 0x4E, 0x44, 0x52, 0x4F, 0x49, 0x44, 0x21 };
    public const byte BOOT_MAGIC_SIZE = 8;
    public const byte BOOT_NAME_SIZE = 16;
    public const byte BOOT_UNUSED_SIZE = 2;
    public const byte BOOT_ID_SIZE = 32;
    public const int BOOT_ARGS_SIZE = 512;
    public const int BOOT_EXTRA_ARGS_SIZE = 1024;
    public static uint BASE_ADDR = 0x10000000;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BOOT_MAGIC_SIZE)]
    [FieldOffset(0)]
    public byte[] magic = (byte[])BOOT_MAGIC.Clone();

    [FieldOffset(8)]
    public uint kernel_size;                            /* size in bytes */
    [FieldOffset(12)]
    public uint kernel_addr = BASE_ADDR + 0x00008000;   /* physical load addr */

    [FieldOffset(16)]
    public uint ramdisk_size;                           /* size in bytes */
    [FieldOffset(20)]
    public uint ramdisk_addr = BASE_ADDR + 0x01000000;  /* physical load addr */

    [FieldOffset(24)]
    public uint second_size;                            /* size in bytes */
    [FieldOffset(28)]
    public uint second_addr = BASE_ADDR + 0x00f00000;   /* physical load addr */

    [FieldOffset(32)]
    public uint tags_addr = BASE_ADDR + 0x00000100;     /* physical addr for kernel tags */
    [FieldOffset(36)]
    public uint page_size = 2048;                       /* flash page size we assume */

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BOOT_UNUSED_SIZE)]
    [FieldOffset(40)]
    public uint[] unused = new uint[BOOT_UNUSED_SIZE];  /* future expansion: should be 0 */

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BOOT_NAME_SIZE)]
    [FieldOffset(48)]
    public string name;                                 /* asciiz product name */

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BOOT_ARGS_SIZE)]
    [FieldOffset(64)]
    public string cmdline;                              /* command line for kernel */

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BOOT_ID_SIZE)]
    [FieldOffset(576)]
    public byte[] id = new byte[BOOT_ID_SIZE];          /* timestamp / checksum / sha1 / etc */

    /* Supplemental command line data; kept here to maintain
     * binary compatibility with older versions of mkbootimg */
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BOOT_EXTRA_ARGS_SIZE)]
    [FieldOffset(608)]
    public string extra_cmdline;

    #region Converting
    public static ImgHeader FromBytes(byte[] bytes)
    {
      GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      ImgHeader stuff = (ImgHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ImgHeader));
      handle.Free();
      return stuff;
    }

    public byte[] ToBytes()
    {
      int size = Marshal.SizeOf(this);
      byte[] arr = new byte[size];
      IntPtr ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(this, ptr, true);
      Marshal.Copy(ptr, arr, 0, size);
      Marshal.FreeHGlobal(ptr);
      return arr;
    }
    #endregion

    #region Set/Get
    public void SetCmdLine(string cmdline_)
    {
      if (cmdline_.Length > BOOT_ARGS_SIZE - 1)
      {
        cmdline = cmdline_.Substring(0, BOOT_ARGS_SIZE - 1) + '\0';
        extra_cmdline = cmdline_.Substring(BOOT_ARGS_SIZE - 1);
        if (extra_cmdline.Length > BOOT_EXTRA_ARGS_SIZE - 1)
          extra_cmdline = extra_cmdline.Substring(0, BOOT_EXTRA_ARGS_SIZE - 1) + '\0';
      }
      else
        cmdline = cmdline_ + '\0';
    }

    public void SetName(string name_)
    {
      if (name_.Length > BOOT_NAME_SIZE - 1)
        name_ = name_.Substring(0, BOOT_NAME_SIZE - 1) + '\0';
      name = name_;
    }

    public void SetBase(uint old_base, uint new_base)
    {
      kernel_addr = kernel_addr - old_base + new_base;
      ramdisk_addr = ramdisk_addr - old_base + new_base;
      second_addr = second_addr - old_base + new_base;
      tags_addr = tags_addr - old_base + new_base;
    }

    public void SetAddress(ref uint address, uint base_, uint value)
    {
      address = base_ + value;
    }

    public uint GetConjecturalBase()
    {
      if (((kernel_addr - 0x00008000) == (ramdisk_addr - 0x01000000)) == ((second_addr - 0x00f00000) == (tags_addr - 0x00000100)))
        return kernel_addr - 0x00008000;
      if ((kernel_addr - 0x00008000) == (ramdisk_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      if ((kernel_addr - 0x00008000) == (second_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      if ((kernel_addr - 0x00008000) == (tags_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      if ((ramdisk_addr - 0x00008000) == (second_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      if ((ramdisk_addr - 0x00008000) == (tags_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      if ((second_addr - 0x00008000) == (tags_addr - 0x01000000))
        return kernel_addr - 0x00008000;
      return 0x10000000;
    }

    public uint GetKernelPages() { return (kernel_size + page_size - 1) / page_size; }
    public uint GetRamdiskPages() { return (ramdisk_size + page_size - 1) / page_size; }
    public uint GetSecStagePages() { return (second_size + page_size - 1) / page_size; }

    public void PrintInfo()
    {
      string _cmdline = cmdline + extra_cmdline;

      Console.Write(@"IMAGE INFORMATION:

Page size:    ");
      ConsoleEx.WriteLine(ConsoleColor.White, "{0}", page_size);
      Console.Write("Tags address: ");
      ConsoleEx.WriteLine(ConsoleColor.White, "0x{0:x8}", tags_addr);
      Console.Write("Product name: ");
      ConsoleEx.WriteLine(ConsoleColor.White, "{0}", (name == null || name.Length == 0) ? "[Not Defined]" : name);
      Console.Write("Command line: ");
      ConsoleEx.WriteLine(ConsoleColor.White, "{0}", (_cmdline == null || _cmdline.Length == 0) ? "[Not Defined]" : _cmdline);

      Console.WriteLine(@"===============SECTIONS TABLE=================
Name        Size          Address       Pages
==============================================");
      Console.Write("Kernel      ");
      ConsoleEx.WriteLine(ConsoleColor.White, "0x{0:x8}    0x{1:x8}    {2}", kernel_size, kernel_addr, GetKernelPages());
      Console.Write("Ramdisk     ");
      ConsoleEx.WriteLine(ConsoleColor.White, "0x{0:x8}    0x{1:x8}    {2}", ramdisk_size, ramdisk_addr, GetRamdiskPages());
      Console.Write("Second      ");
      ConsoleEx.WriteLine(ConsoleColor.White, "0x{0:x8}    0x{1:x8}    {2}", second_size, second_addr, GetSecStagePages());
    }
  }
    #endregion
}
