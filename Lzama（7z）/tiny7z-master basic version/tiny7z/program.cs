using System;
using System.IO;
using pdj.tiny7z.Archive;
using System.Diagnostics; // 必须有这个，才能用 Stopwatch


// basic version


namespace pdj.tiny7z

{

    class Program

    {

        static void Main(string[] args)

        {

            Console.WriteLine("=== tiny7z Direct Execution Tool ===");



            
            //string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            //string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z original Fruits and Vegetables Image Recognition Dataset.7z";
            //string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z original Fruits and Vegetables Image Recognition Dataset";



            //string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Drone Videos";
            //string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z original Drone Videos.7z";
            //string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z original Drone Videos";

                                    
            string sourcePath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image - Copy\Image_1.jpg";
            string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\image.7z";
            string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama";



            // ==========================================


            //直接test 方便

            Console.WriteLine($"\n[1/2] Starting compression of folder: {sourcePath}");
            CompressDirectory(sourcePath, outputArchive);
            DecompressArchive(outputArchive, extractTargetFolder);

            //try

            //{

            //    // 1. Task: Compression

            //    Console.WriteLine($"\n[1/2] Starting compression of folder: {sourceFolder}");

            //    if (Directory.Exists(sourceFolder))

            //    {

            //        CompressDirectory(sourceFolder, outputArchive);

            //        Console.WriteLine("Compression completed successfully!");

            //    }

            //    else

            //    {

            //        Console.WriteLine($"Error: Source folder does not exist: {sourceFolder}");

            //    }



            //    //// 2. Task: Decompression

            //    //Console.WriteLine($"\n[2/2] Starting decompression of file: {outputArchive}");

            //    //if (File.Exists(outputArchive))

            //    //{

            //    // DecompressArchive(outputArchive, extractTargetFolder);

            //    // Console.WriteLine("Decompression completed successfully!");

            //    //}

            //    //else

            //    //{

            //    // Console.WriteLine($"Error: Archive file does not exist: {outputArchive}");

            //    //}

            //}

            //catch (Exception ex)

            //{

            //    Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");

            //    Console.WriteLine(ex.StackTrace);

            //}



            Console.WriteLine("\nAll tasks finished. Press any key to exit...");

            Console.ReadKey();

        }



        /// <summary>
        /// Compresses an entire directory into a single .7z archive file and generates a performance report
        /// </summary>
        static void CompressDirectory(string sourcePath, string targetArchiveFile)
        {
            string archiveDir = Path.GetDirectoryName(targetArchiveFile);
            if (!string.IsNullOrEmpty(archiveDir) && !Directory.Exists(archiveDir))
            {
                Directory.CreateDirectory(archiveDir);
            }
            if (File.Exists(targetArchiveFile))
            {
                File.Delete(targetArchiveFile);
            }
            bool isDirectory = Directory.Exists(sourcePath);
            bool isFile = File.Exists(sourcePath);

            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"\n[Error] The source path does not exist: {sourcePath}");
                return;
            }
            Stopwatch timer = Stopwatch.StartNew();
            Console.WriteLine("--- [CPU Serial] Compression Started ---");

            using (var stream = File.Create(targetArchiveFile))
            using (var archive = new SevenZipArchive(stream, FileAccess.Write))

            using (var compressor = archive.Compressor())
            {
                compressor.CompressHeader = true;
                compressor.PreserveDirectoryStructure = true;
                compressor.Solid = false;

                compressor.ProgressDelegate = (provider, included, currentFileIndex, currentFileSize, filesSize, rawSize, compressedSize) =>
                {
                    return DrawProgressBar(included, currentFileSize, filesSize, rawSize);
                };

                if (isDirectory)
                {
                    Console.WriteLine($"[Info] Detected DIRECTORY. Adding folder: {sourcePath}");
                    compressor.AddDirectory(sourcePath);
                }
                else if (isFile)
                {
                    Console.WriteLine($"[Info] Detected SINGLE FILE. Adding file: {sourcePath}");
                    string fileName = Path.GetFileName(sourcePath);
                    compressor.AddFile(sourcePath, fileName);
                }
                Console.WriteLine("\n[System] Initiating intensive LZMA encoding on CPU. Please wait...");
                compressor.Finalize();
            }
            timer.Stop();
            Console.WriteLine("\n-> CPU compression completed successfully!");
            Console.WriteLine("\n========================================");
            Console.WriteLine("         CPU PERFORMANCE REPORT         ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Processing Unit:      Standard CPU (Serial)");
            Console.WriteLine($"Total Execution Time: {timer.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine($"Total Milliseconds:   {timer.ElapsedMilliseconds} ms");
            Console.WriteLine("========================================");
        }




        /// <summary>
        /// Extracts a .7z archive into a specific target directory and generates a performance report
        /// </summary>
        static void DecompressArchive(string archiveFile, string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            Stopwatch timer = Stopwatch.StartNew();
            Console.WriteLine("--- [CPU Serial] Decompression Started ---");

            using (var stream = File.OpenRead(archiveFile))
            using (var archive = new SevenZipArchive(stream, FileAccess.Read))
            using (var extractor = archive.Extractor())
            {
                extractor.OverwriteExistingFiles = true;
                extractor.PreserveDirectoryStructure = true;

                extractor.ProgressDelegate = (provider, included, currentFileIndex, currentFileSize, filesSize, rawSize, compressedSize) =>
                {
                    return DrawProgressBar(included, currentFileSize, filesSize, rawSize);
                };

                extractor.ExtractArchive(targetDir);
            }

            timer.Stop();
            Console.WriteLine("\n-> CPU Folder decompression completed successfully!");

            Console.WriteLine("\n========================================");
            Console.WriteLine("         CPU PERFORMANCE REPORT         ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Processing Unit:      Standard CPU (Serial)");
            Console.WriteLine($"Total Execution Time: {timer.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine($"Total Milliseconds:   {timer.ElapsedMilliseconds} ms");
            Console.WriteLine("========================================");
        }

        static bool DrawProgressBar(bool included, ulong currentFileSize, ulong filesSize, ulong rawSize)
        {
            if (!included || filesSize <= 0) return true;

            double percentage = (double)rawSize / filesSize * 100;
            if (percentage > 100) percentage = 100;

            int barWidth = 30;
            int filledWidth = (int)(percentage / 100 * barWidth);

            string bar = new string('=', Math.Max(0, filledWidth - 1));
            if (filledWidth > 0 && filledWidth < barWidth) bar += ">";
            else if (filledWidth == barWidth) bar += "=";
            string remaining = new string('.', barWidth - bar.Length);

            double rawMb = (double)rawSize / 1024 / 1024;
            double totalMb = (double)filesSize / 1024 / 1024;

            Console.Write($"\rProgress: [{bar}{remaining}] {percentage:F2}% ({rawMb:F1}MB / {totalMb:F1}MB)");

            return true;
        }



    }

}


