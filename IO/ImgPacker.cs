using System;
using System.IO;
using rbootimg.Utils;

namespace rbootimg.IO
{
  public static class ImgPacker
  {
    public static int Pack(WorkData Data)
    {
      string mKernel, mRamdisk, mSecondStage, mtkKernelHdr, mtkRamdiskHdr;
      mKernel = mRamdisk = mSecondStage = mtkKernelHdr = mtkRamdiskHdr = string.Empty;
      ImgHeader hdr;
      //Для запаковки папки обязательно нужен исходный заголовок
      if (Data.Mode == WorkMode.Pack)
      {
        string mHeader = Path.Combine(Data.pack_dir, "image.hdr");
        mKernel = Path.Combine(Data.pack_dir, "zImage");
        mRamdisk = Path.Combine(Data.pack_dir, "ram_disk.gz");
        mSecondStage = Path.Combine(Data.pack_dir, "second.bin");
        mtkKernelHdr = mKernel + ".mtkhdr";
        mtkRamdiskHdr = mRamdisk + ".mtkhdr";

        Console.WriteLine("Packing image...");
        Console.WriteLine("Source folder: {0}", Data.pack_dir);
        Console.WriteLine("Destination file: {0}", Data.output_path);
        Console.Write("Looking for template header 'image.hdr'... ");
        if (File.Exists(mHeader))
        {
          ConsoleEx.WriteLine(ConsoleColor.Green, "OK");
          hdr = ImgHeader.FromBytes(File.ReadAllBytes(mHeader));
        }
        else
        {
          ConsoleEx.WriteLine(ConsoleColor.Red, "Error: file not found");
          return 1;
        }
      }
      else
        hdr = new ImgHeader();

      //Обновляем данные в заголовке согласно новым входным
      UpdateHeader(ref hdr, Data);

      Console.Write("Looking for source images... ");
      //Пытаемся загрузить образы либо из файла пакета, либо из входного. Приоритет на входной.
      byte[] bKernel = LoadImage(mKernel, Data.kernel_data);
      byte[] bRamdisk = LoadImage(mRamdisk, Data.ramdisk_data);
      byte[] bSecondStage = LoadImage(mSecondStage, Data.second_data);

      //Если что-то не загрузилось ни из одного источника - возвращаеам ошибку
      if (bKernel == null || bRamdisk == null)
      {
        ConsoleEx.WriteLine(ConsoleColor.Red, "FAILED");
        return -2;      // NO SOURCE IMAGES FOUND
      }
      ConsoleEx.WriteLine(ConsoleColor.Green, "OK.");

      //Проверяем, есть ли хоть один источник заголовка МТК.
      //Если хоть что-то есть, то считаем что наш образ полностью для МТК-устройства
      //Также если установлен флаг mtk_force, то необходимо создавать заголовки вручную
      if (File.Exists(mtkKernelHdr) || File.Exists(mtkRamdiskHdr)
          || Data.mtk_kernel_hdr_data != null || Data.mtk_ramdisk_hdr_data != null
          || Data.mtk_force)
      {
        ConsoleEx.WriteLine(ConsoleColor.White, "Image will be in MTK format!");

        byte[] bMtkKernelHdr = LoadImage(mtkKernelHdr, Data.mtk_kernel_hdr_data);
        byte[] bMtkRamdiskHdr = LoadImage(mtkRamdiskHdr, Data.mtk_ramdisk_hdr_data);

        if (bMtkKernelHdr == null)
          ConsoleEx.WriteLine(ConsoleColor.Yellow, "Template of MTK kernel header not found. Will be created a fresh one.");
        if (bMtkRamdiskHdr == null)
          ConsoleEx.WriteLine(ConsoleColor.Yellow, "Template of MTK ramdisk header not found. Will be created a fresh one.");

        //если что-то не загрузили, то необходимо создать новый экземпляр
        mtkSectionHeader smtkKernelHdr = (bMtkKernelHdr != null)
            ? mtkSectionHeader.FromBytes(bMtkKernelHdr)
            : new mtkSectionHeader("KERNEL");
        mtkSectionHeader smtkRamdiskHdr = (bMtkRamdiskHdr != null)
            ? mtkSectionHeader.FromBytes(bMtkRamdiskHdr)
            : new mtkSectionHeader("ROOTFS");

        //Если установлено имя секции рутфс, меняем
        if (Data.mtk_ramdisk_name != null)
        {
          Console.WriteLine("Defining name of ramdisk as '{0}'", Data.mtk_ramdisk_name);
          smtkRamdiskHdr.SetName(Data.mtk_ramdisk_name);
        }

        Console.WriteLine("Generating MTK sections...");
        //создаем новые МТК-секции
        bKernel = mtkSectionWriter.GetBytes(smtkKernelHdr, bKernel);
        bRamdisk = mtkSectionWriter.GetBytes(smtkRamdiskHdr, bRamdisk);

      }

      //Показываем таблицу шаблона
      Console.WriteLine("Creating image... ");
      hdr.kernel_size = (uint)bKernel.Length;
      hdr.ramdisk_size = (uint)bRamdisk.Length;
      hdr.second_size = bSecondStage == null ? (uint)0 : (uint)bSecondStage.Length;
      hdr.PrintInfo();

      //Записываем итоговый
      long sz = 0;
      if (ImgWriter.Write(hdr, bKernel, bRamdisk, bSecondStage, Data.output_path, ref sz))
      {
        Console.WriteLine();
        ConsoleEx.Write(ConsoleColor.Green, "Done!");
        if (sz > 0x600000)
          ConsoleEx.WriteLine(ConsoleColor.Yellow, " But be careful! Size of compiled image is more than 6MB!", sz);
        return 0;   //OK
      }
      ConsoleEx.WriteLine(ConsoleColor.Red, "Oops. Something is wrong.");
      return -1;  //WRITING FAILED

    }

    public static byte[] LoadImage(string imgPath, byte[] alter_data)
    {
      if (alter_data != null)
        return alter_data;
      if (File.Exists(imgPath))
        return File.ReadAllBytes(imgPath);
      return null;
    }

    public static void UpdateHeader(ref ImgHeader hdr, WorkData Data)
    {
      uint OffsetBase = hdr.GetConjecturalBase();
      if (Data.base_offset != null)
      {
        hdr.SetBase(OffsetBase, Data.base_offset.Value);
        OffsetBase = Data.base_offset.Value;
      }
      if (Data.kernel_offset != null)
        hdr.SetAddress(ref hdr.kernel_addr, OffsetBase, Data.kernel_offset.Value);
      if (Data.ramdisk_offset != null)
        hdr.SetAddress(ref hdr.ramdisk_addr, OffsetBase, Data.ramdisk_offset.Value);
      if (Data.second_offset != null)
        hdr.SetAddress(ref  hdr.second_addr, OffsetBase, Data.second_offset.Value);
      if (Data.tags_offset != null)
        hdr.SetAddress(ref hdr.tags_addr, OffsetBase, Data.tags_offset.Value);
      if (Data.pagesize != null)
        hdr.page_size = Data.pagesize.Value;
      if (Data.cmdline != null)
        hdr.SetCmdLine(Data.cmdline);
      if (Data.board != null)
        hdr.SetName(Data.board);
    }
  }
}
