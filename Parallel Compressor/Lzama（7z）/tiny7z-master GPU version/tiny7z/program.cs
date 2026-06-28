using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using pdj.tiny7z.Archive;


using ILGPU;
using ILGPU.Runtime;

//gpu version

namespace pdj.tiny7z
{
    class Program
    {

        private static ConcurrentDictionary<string, byte[]> _processedMemoryBuffer = new ConcurrentDictionary<string, byte[]>();

        static void Main(string[] args)
        {
            Console.WriteLine("=== TPC6323 Heterogeneous GPU+CPU Parallel Tool ===");


            //string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Drone Videos";
            //string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z GPU Drone Videos.7z";
            //string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z GPU Drone Videos";


            //string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            //string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z GPU Fruits and Vegetables Image Recognition Dataset.7z";
            //string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\tiny 7z GPU Fruits and Vegetables Image Recognition Dataset";


            string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image\Image_1.jpg";
            string outputArchive = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\image.7z";
            string extractTargetFolder = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\lzama\image";



            int targetGpuDeviceIndex = 2;

            try
            {

                ExecuteGpuAcceleratedCompression(sourceFolder, outputArchive, targetGpuDeviceIndex);

                ExecuteAsynchronousDecompression(outputArchive, extractTargetFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nAll requested tasks finished. Press any key to exit...");
            Console.ReadKey();
        }

        static void GpuEncryptionKernel(Index1D index, ArrayView<byte> dataView, byte cryptoKey)
        {

            dataView[index] = (byte)(dataView[index] ^ cryptoKey);
        }




        static void ExecuteGpuAcceleratedCompression(string sourcePath, string targetArchiveFile, int gpuIndex)
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
            string[] filesToCompress = isDirectory
                ? Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                : new string[] { sourcePath };

            Console.WriteLine($"\n[1/2] Starting GPU Parallel Compression. Files found: {filesToCompress.Length}");

            ulong totalBytes = 0;
            foreach (var file in filesToCompress) totalBytes += (ulong)new FileInfo(file).Length;
            Console.WriteLine($"[System Check] Total dataset payload size: {(double)totalBytes / 1024 / 1024 / 1024:F4} GB");

            _processedMemoryBuffer.Clear();

            Stopwatch timer = Stopwatch.StartNew();
            Console.WriteLine("--- [GPU Phase 1] Massive Data-Parallelism (XOR Kernel) Launched ---");

            using (var context = ILGPU.Context.CreateDefault())
            {
                if (gpuIndex < 0 || gpuIndex >= context.Devices.Length)
                {
                    Console.WriteLine($"[Warning] Target GPU index [{gpuIndex}] is out of range. Defaulting to preferred device.");
                    gpuIndex = 0;
                }

                var selectedDevice = context.Devices[gpuIndex];

                using (var accelerator = selectedDevice.CreateAccelerator(context))
                {
                    Console.WriteLine($"\n>> [HARDWARE ACTIVE] Successfully bounded to Parallel Device [{gpuIndex}]: {accelerator.Name}");
                    Console.WriteLine($">> Architecture Drive Backend: {selectedDevice.AcceleratorType}");

                    var gpuKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<byte>, byte>(GpuEncryptionKernel);
                    byte secretKey = 125;

                    foreach (var file in filesToCompress)
                    {
                        byte[] fileData = File.ReadAllBytes(file);

                        if (fileData.Length > 0)
                        {
                            using (var buffer = accelerator.Allocate1D<byte>(fileData.Length))
                            {
                                buffer.CopyFromCPU(fileData);
                                gpuKernel((int)buffer.Length, buffer.View, secretKey);
                                accelerator.Synchronize();
                                fileData = buffer.GetAsArray1D();
                            }
                        }

                        _processedMemoryBuffer.TryAdd(file, fileData);
                    }
                }
            }

            Console.WriteLine("\n--- [CPU Phase 2] Streaming Processing Buffer into Single .7z File ---");

            using (var stream = File.Create(targetArchiveFile))
            using (var archive = new SevenZipArchive(stream, FileAccess.Write))
            using (var compressor = archive.Compressor())
            {
                compressor.CompressHeader = true;
                compressor.PreserveDirectoryStructure = true;
                compressor.Solid = false;

                bool[] fileCountedRegistry = new bool[_processedMemoryBuffer.Count];
                ulong totalCompressedRawBytes = 0;
                object lockObject = new object();

                Stopwatch throttleTimer = Stopwatch.StartNew();
                long lastTicks = 0;
                long ticksInterval = Stopwatch.Frequency / 10;

                compressor.ProgressDelegate = (provider, included, currentFileIndex, currentFileSize, filesSize, rawSize, compressedSize) =>
                {
                    if (currentFileIndex >= 0 && currentFileIndex < fileCountedRegistry.Length)
                    {
                        lock (lockObject)
                        {
                            if (!fileCountedRegistry[currentFileIndex])
                            {
                                fileCountedRegistry[currentFileIndex] = true;
                                if (currentFileIndex > 0)
                                {
                                    var prevPath = _processedMemoryBuffer.Keys.ElementAt(currentFileIndex - 1);
                                    totalCompressedRawBytes += (ulong)new FileInfo(prevPath).Length;
                                }
                            }
                        }
                    }

                    long currentTicks = throttleTimer.ElapsedTicks;
                    if (currentTicks - lastTicks >= ticksInterval)
                    {
                        lastTicks = currentTicks;
                        DrawProgressBar(totalCompressedRawBytes + rawSize, totalBytes);
                    }
                    return true;
                };

                foreach (var filePath in _processedMemoryBuffer.Keys)
                {
                    string relativePath;
                    if (isDirectory)
                    {
                        relativePath = filePath.Substring(sourcePath.Length).TrimStart('\\', '/');
                    }
                    else
                    {
                        relativePath = Path.GetFileName(filePath);
                    }

                    compressor.AddFile(filePath, relativePath);
                }

                Console.WriteLine("[System] Initiating intensive LZMA encoding on CPU. Please wait...");
                compressor.Finalize();
                throttleTimer.Stop();
            }

            timer.Stop();
            DrawProgressBar(totalBytes, totalBytes);
            Console.WriteLine("\n-> Heterogeneous GPU+CPU folder compression completed successfully!");

            PrintPerformanceReport("Heterogeneous GPU+CPU Parallel Pipeline", timer);
        }



        static void ExecuteAsynchronousDecompression(string archiveFile, string targetDir)
        {
            if (!File.Exists(archiveFile)) return;
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            Directory.CreateDirectory(targetDir);

            ulong archiveSizeBase = (ulong)new FileInfo(archiveFile).Length;

            Console.WriteLine($"\n[2/2] Starting Decompression of file: {archiveFile}");
            Console.WriteLine($"[System Check] True Archive Package Physical Size: {(double)archiveSizeBase / 1024 / 1024 / 1024:F2} GB");

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

                            if (currentlyExtractedBytes >= archiveSizeBase)
                            {
                                DrawProgressBar(archiveSizeBase - 1024, archiveSizeBase);
                            }
                            else
                            {
                                DrawProgressBar(currentlyExtractedBytes, archiveSizeBase);
                            }
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

            PrintPerformanceReport("Asynchronous Task Decompressor (Disk Monitor)", timer);
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