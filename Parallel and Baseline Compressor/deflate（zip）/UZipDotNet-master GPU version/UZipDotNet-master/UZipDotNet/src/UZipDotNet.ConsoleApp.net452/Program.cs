using System;
using System.IO;
using System.Diagnostics; // 1. 引入诊断命名空间以使用 Stopwatch

namespace UZipDotNet.ConsoleApp.net452
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderToCompress = @"D:\phone picture\dataset";
            string filepath = @"D:\phone picture\dataset.zip";
            string tempunzip = @"D:\phone picture\dataset";

            // 2. 初始化并启动秒表
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 执行压缩
            //compress(filepath, folderToCompress);

            // 如果你之后要测试解压，直接取消下面这行的注释即可
             de(filepath, tempunzip);

            // 3. 停止计时
            stopwatch.Stop();

            // 4. 打印总耗时（保留到毫秒）
            Console.WriteLine("\n========================================");
            Console.WriteLine($"Execution Finished!");
            Console.WriteLine($"Total Time Elapsed: {stopwatch.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine("========================================");

            // 防止控制台一闪而过，方便查看结果
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void compress(string filepath, string folderToCompress)
        {
            Console.WriteLine("start");
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
                using (var def = new DeflateZipFile(filepath))
                {
                    string[] allFiles = Directory.GetFiles(folderToCompress, "*.*", SearchOption.AllDirectories);

                    string safeFolderPath = folderToCompress.EndsWith("\\") ? folderToCompress : folderToCompress + "\\";
                    Uri folderUri = new Uri(safeFolderPath);

                    foreach (string file in allFiles)
                    {
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");

                        Console.WriteLine($"Compressing: {relativePath}");

                        def.Compress(file, relativePath);
                    }

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
            Console.WriteLine("Decompression started...");

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
                    Console.WriteLine($"Found {inf.ZipDir.Count} files in zip archive.");

                    for (int i = 0; i < inf.ZipDir.Count; i++)
                    {
                        var entry = inf.ZipDir[i];
                        Console.WriteLine($"Extracting: {entry.FileName}");

                        try
                        {
                            inf.Decompress(entry, tempunzip, null, true, true);
                        }
                        catch (IOException ioEx)
                        {
                            Console.WriteLine($"[Warning] Skipped locked file '{entry.FileName}': {ioEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical error during decompression: " + ex.Message);
            }

            Console.WriteLine($"Decompression ended. All available files extracted to: {tempunzip}");
        }
    }
}