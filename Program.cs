using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using rbootimg.IO;
using rbootimg.Utils;
using System.IO;

namespace rbootimg
{
  public enum WorkMode
  {
    PureBuild,
    Pack,
    Unpack
	};
  public struct WorkData
  {
    public WorkMode Mode;

    public String pack_dir;
    public byte[] unpack_data;

    public byte[] kernel_data;
    public byte[] ramdisk_data;
    public byte[] second_data;

    public Nullable<uint> base_offset;
    public Nullable<uint> kernel_offset;
    public Nullable<uint> ramdisk_offset;
    public Nullable<uint> second_offset;
    public Nullable<uint> tags_offset;

    public byte[] mtk_kernel_hdr_data;
    public byte[] mtk_ramdisk_hdr_data;
    public String mtk_ramdisk_name;
    public bool mtk_force;

    public String cmdline;
    public String board;
    public Nullable<uint> pagesize;

    public String output_path;
	};

  class Program
  {
    #region Strings
    const string MSG_DIR_ALREADY_EXISTS = @"The directory already exists:

{0}

Maybe it's an old unpacked image that you need,
so you must rename or delete it by yourself.";

    const string MSG_WRONG_IMAGE = @"Error: Wrong Android Boot Image";
    #endregion Strings

    public static WorkData Data;
    public const int BOOT_ARGS_SIZE = 512;
    public const int BOOT_EXTRA_ARGS_SIZE = 1024;
    static void Main(string[] args)
		{
			Version v = System.Reflection.Assembly.GetEntryAssembly ().GetName ().Version;
			Console.Title = string.Format ("rbootimg - Android Boot Image Repack Tool [Version {0}.{1}.{2}]", v.Major, v.Minor, v.Build);
			ConsoleEx.WriteLine (ConsoleColor.White, Console.Title);
			ConsoleEx.WriteLine (ConsoleColor.White, "(c) GoldRenard, 2013.");
			Console.WriteLine ();

			try {
				InitializeComandLineParser ();
				ParseArguments (args);
				CheckArgs ();
			} catch {
				PrintUsage ();
				return;
			}

			switch (Data.Mode) {
			case WorkMode.Unpack:
				int result = ImgUnpacker.Unpack (Data.unpack_data, Data.output_path);
				if (result == 1)
					ConsoleEx.WriteLine (ConsoleColor.Yellow, MSG_DIR_ALREADY_EXISTS, Data.output_path);
				if (result == -1)
					ConsoleEx.WriteLine (ConsoleColor.Red, MSG_WRONG_IMAGE);
				break;
			case WorkMode.Pack:
			case WorkMode.PureBuild:
				ImgPacker.Pack (Data);
				break;
			}
		}

    private static void InitializeComandLineParser()
		{
			string[] optionalArguments = {
				"--pack = ",
				"--unpack = ",

				"--kernel = ",
				"--ramdisk = ",
				"--second = ",

				"--base = ",
				"--kernel_offset = ",
				"--ramdisk_offset = ",
				"--second_offset = ",
				"--tags_offset = ",
				"--pagesize = ",

				"--mtk_kernel_header = ",
				"--mtk_ramdisk_header = ",
				"--mtk_ramdisk_name = ",

				"--cmdline = ",
				"--board = ",

				"--output = ",
			};
			CommandLineArgumentParser.DefineOptionalParameter (optionalArguments);

			string[] switches = {
				"--mtk_force"
			};
			CommandLineArgumentParser.DefineSwitches (switches);
		}

    private static void ParseArguments(string[] args)
		{
			if (args.Length == 1 && args [0].Trim () == "--help")
				PrintUsage ();
			else
				CommandLineArgumentParser.ParseArguments (args);
		}

    private static void CheckArgs()
		{
			if (!CommandLineArgumentParser.IsSpecified ("--output")) {
				ConsoleEx.WriteLine (ConsoleColor.Red, "Error: no output filename/directory specified");
				throw new CommandLineArgumentException ();
			}
			Data.output_path = CommandLineArgumentParser.GetParamValue ("--output");

			//Если мы хотим собрать образ
			if (!CommandLineArgumentParser.IsSpecified ("--unpack") && !CommandLineArgumentParser.IsSpecified ("--pack")) {
				try {
					CheckPackArgs (true);
				} catch (Exception ex) {
					throw ex;
				}
				Data.Mode = WorkMode.PureBuild;
				return;
			}

			//Если указан unpack (но не указан pack), мы распаковываем. При этом нам нужны только два аргумента - сам unpack и output
			if (CommandLineArgumentParser.IsSpecified ("--unpack") && !CommandLineArgumentParser.IsSpecified ("--pack")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--unpack"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: no input image specified");
					throw new CommandLineArgumentException ();
				}
				Data.unpack_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--unpack"));
				Data.Mode = WorkMode.Unpack;
				return;
			}

			//Если указан pack (но не указан unpack), мы запаковываем. При этом у нас могут быть все
			//дополнительные элементы (включая ядро и рамдиск, но как необязательные)
			if (CommandLineArgumentParser.IsSpecified ("--pack") && !CommandLineArgumentParser.IsSpecified ("--unpack")) {
				if (!Directory.Exists (CommandLineArgumentParser.GetParamValue ("--pack"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: no input directory specified");
					throw new CommandLineArgumentException ();
				}
				try {
					CheckPackArgs (false);
				} catch (Exception ex) {
					throw ex;
				}
				Data.pack_dir = CommandLineArgumentParser.GetParamValue ("--pack");
				Data.Mode = WorkMode.Pack;
				return;
			}

			//Если мы дошли до сюда, это значит что шаловливые ручонки юзера задали оба ключа -pack и -unpack
			//Говорим что так делать нехорошо.
			ConsoleEx.WriteLine (ConsoleColor.Red, "Error: you've defined both -pack and -unpack. What do you mean?");
			throw new CommandLineArgumentException ();
		}

    private static void CheckPackArgs(bool IsKRRequired)
		{
			#region Images
			//Если образы требуются, выдать исключение в случае их отсутствия
			if (IsKRRequired) {
				if (!CommandLineArgumentParser.IsSpecified ("--kernel")) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: no kernel image specified");
					throw new CommandLineArgumentException ();
				}

				if (!CommandLineArgumentParser.IsSpecified ("--ramdisk")) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: no ramdisk image specified");
					throw new CommandLineArgumentException ();
				}
			}

			//Если образы заданы, проверить их существование
			if (CommandLineArgumentParser.IsSpecified ("--kernel")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--kernel"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: specified kernel image does not exist: '{0}'", CommandLineArgumentParser.GetParamValue ("--kernel"));
					throw new CommandLineArgumentException ();
				}
				Data.kernel_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--kernel"));
			}

			if (CommandLineArgumentParser.IsSpecified ("--ramdisk")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--ramdisk"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: specified ramdisk image does not exist: '{0}'", CommandLineArgumentParser.GetParamValue ("--ramdisk"));
					throw new CommandLineArgumentException ();
				}
				Data.ramdisk_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--ramdisk"));
			}

			if (CommandLineArgumentParser.IsSpecified ("--second")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--second"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: specified second-bootloader image does not exist: '{0}'", CommandLineArgumentParser.GetParamValue ("--second"));
					throw new CommandLineArgumentException ();
				}
				Data.second_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--second"));
			}
			#endregion

			#region Offsets and pagesize

			if (CommandLineArgumentParser.IsSpecified ("--base")) {
				try {
					Data.base_offset = Convert.ToUInt32 (CommandLineArgumentParser.GetParamValue ("--base"), 16);
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: base offset must be integer (may be in hex)");
					throw new CommandLineArgumentException ();
				}
			}

			if (CommandLineArgumentParser.IsSpecified ("--kernel_offset")) {
				try {
					Data.kernel_offset = Convert.ToUInt32 (CommandLineArgumentParser.GetParamValue ("--kernel_offset"), 16);
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: kernel offset must be integer (may be in hex)");
					throw new CommandLineArgumentException ();
				}
			}

			if (CommandLineArgumentParser.IsSpecified ("--ramdisk_offset")) {
				try {
					Data.ramdisk_offset = Convert.ToUInt32 (CommandLineArgumentParser.GetParamValue ("--ramdisk_offset"), 16);
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: ramdisk offset must be integer (may be in hex)");
					throw new CommandLineArgumentException ();
				}
			}

			if (CommandLineArgumentParser.IsSpecified ("--second_offset")) {
				try {
					Data.second_offset = Convert.ToUInt32 (CommandLineArgumentParser.GetParamValue ("--second_offset"), 16);
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: second-bootloader offset must be integer (may be in hex)");
					throw new CommandLineArgumentException ();
				}
			}

			if (CommandLineArgumentParser.IsSpecified ("--tags_offset")) {
				try {
					Data.tags_offset = Convert.ToUInt32 (CommandLineArgumentParser.GetParamValue ("--tags_offset"), 16);
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: tags offset must be integer (may be in hex)");
					throw new CommandLineArgumentException ();
				}
			}

			if (CommandLineArgumentParser.IsSpecified ("--pagesize")) {
				try {
					Data.pagesize = uint.Parse (CommandLineArgumentParser.GetParamValue ("--pagesize"));
				} catch {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: pagesize must be integer");
					throw new CommandLineArgumentException ();
				}

				if ((Data.pagesize != 2048) && (Data.pagesize != 4096) && (Data.pagesize != 8192) && (Data.pagesize != 16384)) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: unsupported page size - {0}", Data.pagesize);
					throw new CommandLineArgumentException ();
				}
			}

			#endregion Offsets and pagesize

			#region MTK
			//Если MTK заголовки заданы, проверить их существование
			if (CommandLineArgumentParser.IsSpecified ("--mtk_kernel_header")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--mtk_kernel_header"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: specified MTK kernel header does not exist: '{0}'", CommandLineArgumentParser.GetParamValue ("--mtk_kernel_header"));
					throw new CommandLineArgumentException ();
				}
				Data.mtk_kernel_hdr_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--mtk_kernel_header"));
			}

			if (CommandLineArgumentParser.IsSpecified ("--mtk_ramdisk_header")) {
				if (!File.Exists (CommandLineArgumentParser.GetParamValue ("--mtk_ramdisk_header"))) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: specified MTK ramdisk header does not exist: '{0}'", CommandLineArgumentParser.GetParamValue ("--mtk_ramdisk_header"));
					throw new CommandLineArgumentException ();
				}
				Data.mtk_ramdisk_hdr_data = File.ReadAllBytes (CommandLineArgumentParser.GetParamValue ("--mtk_ramdisk_header"));
			}

			//Проверим длину имени МТК-рамдиска
			if (CommandLineArgumentParser.IsSpecified ("--mtk_ramdisk_name")) {
				if (CommandLineArgumentParser.GetParamValue ("--mtk_ramdisk_name").Length > (mtkSectionHeader.SECTION_NAME_SIZE - 1)) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: MTK ramdisk name too large");
					throw new CommandLineArgumentException ();
				}
				Data.mtk_ramdisk_name = CommandLineArgumentParser.GetParamValue ("--mtk_ramdisk_name");
			}
			#endregion MTK

			//Проверим длину командной строки ядра. Должна быть короче (BOOT_ARGS_SIZE + BOOT_EXTRA_ARGS_SIZE - 2).
			if (CommandLineArgumentParser.IsSpecified ("--cmdline")) {
				if (CommandLineArgumentParser.GetParamValue ("--cmdline").Length > (ImgHeader.BOOT_ARGS_SIZE + ImgHeader.BOOT_EXTRA_ARGS_SIZE - 2)) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: kernel commandline too large");
					throw new CommandLineArgumentException ();
				}
				Data.cmdline = CommandLineArgumentParser.GetParamValue ("--cmdline");
			}

			//Проверим длину имени девайса
			if (CommandLineArgumentParser.IsSpecified ("--board")) {
				if (CommandLineArgumentParser.GetParamValue ("--board").Length > (ImgHeader.BOOT_NAME_SIZE - 1)) {
					ConsoleEx.WriteLine (ConsoleColor.Red, "Error: board name too large");
					throw new CommandLineArgumentException ();
				}
				Data.board = CommandLineArgumentParser.GetParamValue ("--board");
			}

			Data.mtk_force = CommandLineArgumentParser.IsSwitchOn ("--mtk_force");
		}

    private static void PrintUsage()
		{
			Console.WriteLine (@"
USAGE 1 - Make image with specified kernel, ramdisk, etc:
rbootimg.exe
    --kernel <filename>
    --ramdisk <filename>
    [ --second <2ndbootloader-filename> ]
    [ --base <address> ]                  - default is 0x10000000
    [ --kernel_offset <address> ]         - default is 0x00008000
    [ --ramdisk_offset <address> ]        - default is 0x01000000
    [ --second_offset <address> ]         - default is 0x00f00000
    [ --tags_offset <address> ]           - default is 0x00000100
    [ --pagesize <pagesize> ]             - default is 2048
    [ --mtk_kernel_header <filename> ]
    [ --mtk_ramdisk_header <filename> ]
    [ --mtk_force ]                       - create fresh MTK headers even if they aren't defined
    [ --mtk_ramdisk_name <name> ]
    [ --cmdline <kernel-commandline> ]
    [ --board <boardname> ]
    --output <filename>

USAGE 2 - Split image to headers and sections:
rbootimg.exe
    --unpack <filename>
    --output <directory>

USAGE 3 - Union headers and sections to image:
rbootimg.exe
    --pack <directory>
    [ --kernel <filename> ]
    [ --ramdisk <filename> ]
    [ --second <2ndbootloader-filename> ]
    [ --base <address> ]                  - default is 0x10000000
    [ --kernel_offset <address> ]         - default is 0x00008000
    [ --ramdisk_offset <address> ]        - default is 0x01000000
    [ --second_offset <address> ]         - default is 0x00f00000
    [ --tags_offset <address> ]           - default is 0x00000100
    [ --pagesize <pagesize> ]             - default is 2048
    [ --mtk_kernel_header <filename> ]
    [ --mtk_ramdisk_header <filename> ]
    [ --mtk_force ]                       - create fresh MTK headers even if they aren't defined
    [ --mtk_ramdisk_name <name> ]
    [ --cmdline <kernel-commandline> ]
    [ --board <boardname> ]
    --output <filename>

    TIP: Optional parameters will override information from specified headers");
		}
	}
}
