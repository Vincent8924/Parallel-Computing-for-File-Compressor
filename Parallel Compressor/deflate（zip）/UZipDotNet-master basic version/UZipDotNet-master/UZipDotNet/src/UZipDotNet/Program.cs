using System;
using System.Diagnostics;
using System.IO;



//basic version
namespace UZipDotNet.ConsoleApp.net452
{
    class Program
    {
        static void Main(string[] args)
        {
            //string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Fruits and Vegetables Image Recognition Dataset.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Fruits and Vegetables Image Recognition Dataset";


            //string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Drone Videos";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Drone Videos.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Drone Videos";


            string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image\Image_1.jpg";
            string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\image.zip";
            string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate";



            Stopwatch totalStopwatch = new Stopwatch();
            totalStopwatch.Start();


            compress(filepath, targetToCompress);


            de(filepath, tempunzip);

            totalStopwatch.Stop();


            Console.WriteLine("\n========================================");
            Console.WriteLine("        CPU PERFORMANCE REPORT          ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Processing Unit:      Standard CPU (Serial)");
            Console.WriteLine($"Total Execution Time: {totalStopwatch.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine($"Total Milliseconds:   {totalStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine("========================================");

            Console.WriteLine("\nAll CPU Tasks completed! Press any key to exit...");
            Console.ReadKey();
        }


        static void DrawProgressBar(string taskName, int progress, int total, int width = 30)
        {

            float percentage = (total == 0) ? 100 : ((float)progress / total);
            int progressBlocks = (int)(percentage * width);


            string progressBar = new string('█', progressBlocks) + new string('-', width - progressBlocks);

            Console.Write($"\r{taskName,-20} [{progressBar}] {percentage * 100:0.0}% ({progress}/{total})");
        }

        static void compress(string filepath, string targetToCompress)
        {
            Console.WriteLine("\n--- [CPU Serial] Adaptive Compression Started ---");
            try
            {
                if (File.Exists(filepath)) File.Delete(filepath);
            }
            catch (Exception ex) { Console.WriteLine("Ignore : " + ex); }

            bool isDirectory = Directory.Exists(targetToCompress);
            bool isFile = File.Exists(targetToCompress);

            

            using (var def = new DeflateZipFile(filepath))
            {
                if (isFile)
                {
                    Console.WriteLine("Target detected as: Single File");

                    string fileName = Path.GetFileName(targetToCompress);

                    string folderName = Path.GetFileNameWithoutExtension(targetToCompress);

                    string relativePath = folderName + "/" + fileName;

                    def.Compress(targetToCompress, relativePath);

                    DrawProgressBar("Compressing File", 1, 1);
                    Console.WriteLine();
                }
                else if (isDirectory)
                {
                    Console.WriteLine("Target detected as: Directory Folder");

                    string[] allFiles = Directory.GetFiles(targetToCompress, "*.*", SearchOption.AllDirectories);
                    int totalFiles = allFiles.Length;

                    string safeFolderPath = targetToCompress.EndsWith("\\") ? targetToCompress : targetToCompress + "\\";
                    Uri folderUri = new Uri(safeFolderPath);

                    for (int i = 0; i < totalFiles; i++)
                    {
                        string file = allFiles[i];
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");

                        def.Compress(file, relativePath);

                        DrawProgressBar("Compressing Files", i + 1, totalFiles);
                    }
                    Console.WriteLine();
                }

                def.Save();
            }
            Console.WriteLine("-> Compression completed successfully!");
        }




        //decompress

        static void de(string filepath, string tempunzip)
        {
            Console.WriteLine("\n--- [CPU Serial] Decompression Started ---");

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

                    for (int i = 0; i < totalFiles; i++)
                    {
                        var entry = inf.ZipDir[i];

                        try
                        {
                            inf.Decompress(entry, tempunzip, null, true, true);
                        }
                        catch (IOException ioEx)
                        {

                            Console.Write("\r".PadRight(80) + "\r");
                            Console.WriteLine($"[Warning] Skipped locked file '{entry.FileName}': {ioEx.Message}");
                        }

                        DrawProgressBar("Extracting Files", i + 1, totalFiles);
                    }
                    Console.WriteLine(); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nCritical error during decompression: " + ex.Message);
            }

            Console.WriteLine($"-> CPU Decompression ended. All available files extracted to: {tempunzip}");
        }



    }
}