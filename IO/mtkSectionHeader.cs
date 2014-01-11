using System;
using System.Text;
using System.Runtime.InteropServices;

namespace rbootimg.IO
{
    [StructLayout(LayoutKind.Explicit, Size = 512, Pack = 1, CharSet = CharSet.Ansi)]
    public class mtkSectionHeader
    {
        public const uint SECTION_MAGIC = 0x58881688;
        public const byte SECTION_MAGIC_SIZE = 4;
        public const byte SECTION_NAME_SIZE = 32;
        public const int SECTION_UNKNOWN_FF_SIZE = 472;

        [FieldOffset(0)]
        public uint magic = SECTION_MAGIC;

        [FieldOffset(4)]
        public uint section_size;                                               /* size in bytes */

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SECTION_NAME_SIZE)]
        [FieldOffset(8)]
        public string name;                                                     /* section name */

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SECTION_UNKNOWN_FF_SIZE)]
        [FieldOffset(40)]
        public byte[] unknown_ff;

        public mtkSectionHeader() { }

        public mtkSectionHeader(string name)
        {
            SetName(name);
            unknown_ff = new byte[SECTION_UNKNOWN_FF_SIZE];
            for (int i = 0; i < SECTION_UNKNOWN_FF_SIZE; i++)
                unknown_ff[i] = 0xFF;
        }

        public static mtkSectionHeader FromBytes(byte[] bytes)
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            mtkSectionHeader stuff = (mtkSectionHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(mtkSectionHeader));
            handle.Free();
            return stuff;
        }

        public void SetName(string name_)
        {
            if (name_.Length > SECTION_NAME_SIZE - 1)
                name_ = name_.Substring(0, SECTION_NAME_SIZE - 1) + '\0';
            name = name_;
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
    }
}
