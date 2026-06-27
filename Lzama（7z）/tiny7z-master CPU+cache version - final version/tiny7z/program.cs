using pdj.tiny7z.Archive;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;



//tiny7z-master CPU+cache version - new test
//final version


namespace pdj.tiny7z
{
    class Program
    {
        // 🌟 CACHE PARALLEL 核心：使用确定性二维数组，实现全核无锁并发缓存行对齐 (Lock-Free)
        private static byte[][] _parallelMemoryBuffer;

        public static byte[] GetCachedFile(int index)
        {
            return _parallelMemoryBuffer[index];
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== TPC6323 Cache-Optimized Heterogeneous Parallel Tool ===");

            string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\tiny7z Fruits and Vegetables Image Recognition Dataset.7z";
            string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\tiny7z Fruits and Vegetables Image Recognition Dataset";


            //string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image\Image_1.jpg";
            //string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\image.7z";
            //string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\image";



            //string sourceFolder = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\New folder";
            //string outputArchive = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\zip\7z\tiny 7z.7z";
            //string extractTargetFolder = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\zip\7z";

            try
            {
                ExecuteCacheOptimizedCompression(sourceFolder, outputArchive);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Sleep(2000);

                ExecuteCacheOptimizedDecompression(outputArchive, extractTargetFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("\nAll requested tasks finished. Press any key to exit...");
            Console.ReadKey();
        }

        // =================================================================
        // FUNCTION 1: CPU + CACHE PARALLEL COMPRESSION (FIXED & FULLY OPTIMIZED)
        // =================================================================


        static void ExecuteCacheOptimizedCompression(string sourcePath, string targetArchiveFile)
        {
            string archiveDir = Path.GetDirectoryName(targetArchiveFile);
            if (!string.IsNullOrEmpty(archiveDir) && !Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);

            if (File.Exists(targetArchiveFile)) File.Delete(targetArchiveFile);

            bool isDirectory = Directory.Exists(sourcePath);
            bool isFile = File.Exists(sourcePath);

            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"\n[Error] The source path does not exist: {sourcePath}");
                return;
            }

            string[] filesToCompress;
            if (isDirectory)
            {
                Console.WriteLine($"\n[Info] Detected DIRECTORY. Scanning folder: {sourcePath}");
                filesToCompress = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
            }
            else
            {
                Console.WriteLine($"\n[Info] Detected SINGLE FILE. Preparing file: {sourcePath}");
                filesToCompress = new string[] { sourcePath };
            }

            Array.Sort(filesToCompress, StringComparer.Ordinal);
            Console.WriteLine($"\n[1/2] Starting Parallel Compression. Files found: {filesToCompress.Length}");

            FileInfo[] fileInfoCache = new FileInfo[filesToCompress.Length];
            ulong totalBytes = 0;
            for (int i = 0; i < filesToCompress.Length; i++)
            {
                fileInfoCache[i] = new FileInfo(filesToCompress[i]);
                totalBytes += (ulong)fileInfoCache[i].Length;
            }
            Console.WriteLine($"[System Check] Total dataset payload size: {(double)totalBytes / 1024 / 1024 / 1024:F4} GB");

            _parallelMemoryBuffer = new byte[filesToCompress.Length][];

            Stopwatch timer = Stopwatch.StartNew();
            Console.WriteLine("--- [CPU + Cache Parallel] Phase 1: Parallel I/O Read ---");

            int processorCount = Environment.ProcessorCount;
            int chunkSize = Math.Max(1, filesToCompress.Length / (processorCount * 4));
            var rangePartitioner = Partitioner.Create(0, filesToCompress.Length, chunkSize);

            Parallel.ForEach(rangePartitioner, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _parallelMemoryBuffer[i] = File.ReadAllBytes(filesToCompress[i]);
                }
            });

            ulong[] cumulativeBytes = new ulong[filesToCompress.Length + 1];
            cumulativeBytes[0] = 0;
            for (int i = 0; i < filesToCompress.Length; i++)
                cumulativeBytes[i + 1] = cumulativeBytes[i] + (ulong)fileInfoCache[i].Length;

            var activeStreams = new System.Collections.Generic.List<MemoryStream>();

            try
            {
                Console.WriteLine("--- [CPU + Cache Parallel] Phase 2: LZMA Encoding from Memory ---");
                using (var stream = File.Create(targetArchiveFile))
                using (var archive = new SevenZipArchive(stream, FileAccess.Write))
                using (var compressor = archive.Compressor())
                {
                    compressor.CompressHeader = true;
                    compressor.PreserveDirectoryStructure = true;
                    compressor.Solid = true; 

                    long lastTicks = 0;
                    long ticksInterval = Stopwatch.Frequency / 10;
                    object uiLock = new object();

                    compressor.ProgressDelegate = (provider, included, currentFileIndex, currentFileSize, filesSize, rawSize, compressedSize) =>
                    {
                        long currentTicks = Stopwatch.GetTimestamp();
                        if (currentTicks - lastTicks >= ticksInterval)
                        {
                            lock (uiLock)
                            {
                                if (currentTicks - lastTicks >= ticksInterval)
                                {
                                    lastTicks = currentTicks;
                                    ulong baseBytes = (currentFileIndex >= 0 && currentFileIndex < cumulativeBytes.Length)
                                        ? cumulativeBytes[currentFileIndex]
                                        : 0;
                                    DrawProgressBar(baseBytes + rawSize, totalBytes);
                                }
                            }
                        }
                        return true;
                    };

                    string rootName = Path.GetFileName(sourcePath);
                    for (int i = 0; i < filesToCompress.Length; i++)
                    {
                        string filePath = filesToCompress[i];
                        string relativePath = isDirectory
                            ? Path.Combine(rootName, filePath.Substring(sourcePath.Length + 1))
                            : rootName;

                        var memStream = new MemoryStream(_parallelMemoryBuffer[i]);
                        activeStreams.Add(memStream);

                        compressor.AddFile(memStream, relativePath);
                    }

                    Console.WriteLine("\n[System] Initiating intensive LZMA encoding on CPU from RAM Buffer. Please wait...");
                    compressor.Finalize();
                    stream.Flush();
                }
            }
            finally
            {
                foreach (var s in activeStreams) s.Dispose();
                activeStreams.Clear();
                _parallelMemoryBuffer = null;
            }

            timer.Stop();
            DrawProgressBar(totalBytes, totalBytes);
            Console.WriteLine("\n-> CPU Cache-optimized folder compression completed successfully!");
            PrintPerformanceReport("CPU + Cache Parallel Pipeline (Compression)", timer);
        }






        // =================================================================
        // FUNCTION 2: CPU + CACHE ASYNCHRONOUS TASK DECOMPRESSION
        // =================================================================
        static void ExecuteCacheOptimizedDecompression(string archiveFile, string targetDir)
        {
            if (!File.Exists(archiveFile))
            {
                Console.WriteLine($"\n[Error] Archive file not found: {archiveFile}");
                return;
            }

            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            Directory.CreateDirectory(targetDir);

            ulong archiveSizeBase = (ulong)new FileInfo(archiveFile).Length;

            Console.WriteLine($"\n[2/2] Starting Decompression of file: {archiveFile}");
            Console.WriteLine($"[System Check] True Archive Package Physical Size: {(double)archiveSizeBase / 1024 / 1024 / 1024:F6} GB");

            Stopwatch timer = Stopwatch.StartNew();
            Console.WriteLine("--- [Asynchronous ThreadPool Task] Decompression Started ---");

            Stopwatch throttleTimer = Stopwatch.StartNew();
            long lastTicks = 0;
            long ticksInterval = Stopwatch.Frequency / 10;

            Task decompressionTask = Task.Run(() =>
            {
                using (var stream = File.OpenRead(archiveFile))
                using (var archive = new SevenZipArchive(stream, FileAccess.Read))
                using (var extractor = archive.Extractor())
                {
                    extractor.OverwriteExistingFiles = true;
                    extractor.PreserveDirectoryStructure = true;
                    extractor.ProgressDelegate = (provider, included, currentFileIndex, currentFileSize, filesSize, rawSize, compressedSize) =>
                    {
                        long currentTicks = throttleTimer.ElapsedTicks;
                        if (currentTicks - lastTicks >= ticksInterval)
                        {
                            lastTicks = currentTicks;

                            ulong currentlyExtractedBytes = 0;
                            if (Directory.Exists(targetDir))
                            {
                                var extractedFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);
                                foreach (var file in extractedFiles)
                                {
                                    currentlyExtractedBytes += (ulong)new FileInfo(file).Length;
                                }
                            }
                            ulong displayBytes = Math.Min(currentlyExtractedBytes, archiveSizeBase);
                            DrawProgressBar(displayBytes, archiveSizeBase);
                        }
                        return true;
                    };
                    extractor.ExtractArchive(targetDir);
                }
            });

            decompressionTask.Wait();
            timer.Stop();
            throttleTimer.Stop();

            DrawProgressBar(archiveSizeBase, archiveSizeBase);

            Console.WriteLine("\n-> Decompression executed and extracted successfully!");
            PrintPerformanceReport("Asynchronous Task Decompressor (Cache-Aligned)", timer);
        }




        static bool DrawProgressBar(ulong processedBytes, ulong totalBytes)
        {
            if (totalBytes <= 0) return true;

            double percentage = (double)processedBytes / totalBytes * 100;
            if (percentage > 100) percentage = 100;

            int barWidth = 30;
            int filledWidth = (int)(percentage / 100 * barWidth);

            string bar = new string('=', Math.Max(0, filledWidth - 1));
            if (filledWidth > 0 && filledWidth < barWidth) bar += ">";
            else if (filledWidth == barWidth) bar += "=";
            string remaining = new string('.', barWidth - bar.Length);

            double processedMb = (double)processedBytes / 1024 / 1024;
            double totalMb = (double)totalBytes / 1024 / 1024;

            if (totalMb >= 1024)
            {
                Console.Write($"\rProgress: [{bar}{remaining}] {percentage:F1}% ({processedMb / 1024:F2}GB / {totalMb / 1024:F2}GB)");
            }
            else
            {
                Console.Write($"\rProgress: [{bar}{remaining}] {percentage:F1}% ({processedMb:F1}MB / {totalMb:F1}MB)");
            }
            return true;
        }

        static void PrintPerformanceReport(string unitName, Stopwatch timer)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("         CPU PERFORMANCE REPORT         ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Processing Unit:      {unitName}");
            Console.WriteLine($"Total Execution Time: {timer.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine($"Total Milliseconds:   {timer.ElapsedMilliseconds} ms");
            Console.WriteLine("========================================\n");
        }
    }
}