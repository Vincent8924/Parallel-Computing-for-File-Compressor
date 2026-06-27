using System;
using System.IO;
using System.Linq;

// 解决 ZipEntry 找不到的问题
using UZipDotNet;

// 只有在 .NET 4.0 及以上版本才引入并行库
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace UZipDotNet.ConsoleApp.net452
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\UZipDotNet-master\test";
            string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\UZipDotNet-master\after.zip";
            string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\UZipDotNet-master\test";

            // 测试并行压缩
            compress(filepath, folderToCompress);

            // 测试并行解压
            de(filepath, tempunzip);
        }

        static void compress(string filepath, string folderToCompress)
        {
            Console.WriteLine("--- Compression Started ---");
            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ignore : " + ex);
            }

            if (Directory.Exists(folderToCompress))
            {
                string[] allFiles = Directory.GetFiles(folderToCompress, "*.*", SearchOption.AllDirectories);
                string safeFolderPath = folderToCompress.EndsWith("\\") ? folderToCompress : folderToCompress + "\\";
                Uri folderUri = new Uri(safeFolderPath);

                using (var def = new DeflateZipFile(filepath))
                {
                    object locker = new object();

                    // 1. 如果是 .NET 4.0 及以上现代版本，跑多核并行
#if !NET20 && !NET35
                    Console.WriteLine("Running with CPU Parallel Mode...");
                    Parallel.ForEach(allFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
                    {
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");

                        lock (locker)
                        {
                            def.Compress(file, relativePath);
                        }
                    });
                    // 2. 如果是老旧的 .NET 2.0 / 3.5 版本，自动退回单线程兼容模式，防止报错
#else
                    Console.WriteLine("Running with Legacy Single Thread Mode...");
                    foreach (string file in allFiles)
                    {
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");
                        def.Compress(file, relativePath);
                    }
#endif

                    Console.WriteLine("Writing to disk...");
                    def.Save();
                }
                Console.WriteLine("Folder compression completed successfully!");
            }
            else
            {
                Console.WriteLine($"Error: Folder not found: {folderToCompress}");
            }
        }

        static void de(string filepath, string tempunzip)
        {
            Console.WriteLine("\n--- Decompression Started ---");

            if (tempunzip.EndsWith("\\test") || tempunzip.EndsWith("/test"))
            {
                tempunzip = tempunzip + "_unzipped";
            }

            if (!Directory.Exists(tempunzip))
            {
                Directory.CreateDirectory(tempunzip);
            }

            try
            {
                using (var inf = new InflateZipFile(filepath))
                {
                    int totalFiles = inf.ZipDir.Count;
                    Console.WriteLine($"Found {totalFiles} files in zip archive.");

                    // 【终极修正 1】：这里直接使用常规的 System.Collections.ArrayList 转换 
                    // 或者直接用原本的集合，避开所有未知的类名
                    var entries = new object[totalFiles];
                    for (int i = 0; i < totalFiles; i++)
                    {
                        entries[i] = inf.ZipDir[i];
                    }

                    // 1. 现代环境并行解压
#if NET452 || NET45 || NET46 || NETCOREAPP
                    System.Threading.Tasks.Parallel.For(0, totalFiles, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                    {
                        // 【终极修正 2】：强制转成 object 类型传入，Decompress 方法内部重载会自动识别
                        object entry = entries[i];
                        try
                        {
                            inf.Decompress((dynamic)entry, tempunzip, null, true, true);
                        }
                        catch (Exception ioEx)
                        {
                            Console.WriteLine($"[Warning] Skipped file: {ioEx.Message}");
                        }
                    });
// 2. 老旧环境串行解压
#else
                    for (int i = 0; i < totalFiles; i++)
                    {
                        object entry = entries[i];
                        try
                        {
                            inf.Decompress((dynamic)entry, tempunzip, null, true, true);
                        }
                        catch (Exception ioEx)
                        {
                            Console.WriteLine($"[Warning] Skipped file: {ioEx.Message}");
                        }
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical error during decompression: " + ex.Message);
            }

            Console.WriteLine($"Decompression ended. Extracted to: {tempunzip}");
        }
    }
}