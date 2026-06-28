using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UZipDotNet;

namespace MyParallelZipApp
{
    class ThreadCache
    {
        public DeflateZipFile Compressor;

        public byte[] ReadBuffer;

        public int Count;

        public ThreadCache()
        {
            Compressor = DeflateZipFile.CreateMemoryCompressor();

            ReadBuffer =  ArrayPool<byte>.Shared.Rent( 8 * 1024 * 1024);
        }
    }
    public static class Compressor
    {
        public static string Test()
        {
            return "Reference Works!";
        }
        public static Action<int>? ProgressChanged;
        public static Action<string>? StatusChanged;
        public static Action<string>? SpeedChanged;
        public static Action<string>? FilesChanged;
        public static Action<string>? TimeChanged;
        public static void compress(string filepath, string targetToCompress)
        {
            Console.WriteLine("\n--- [CPU & Cache Parallel] Compression Started ---");

            try { if (File.Exists(filepath)) File.Delete(filepath); } catch { }

            bool isDirectory = Directory.Exists(targetToCompress);
            bool isFile = File.Exists(targetToCompress);
            

  
            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"Error: Target not found: {targetToCompress}");
                return;
            }
            DateTime startTime = DateTime.Now;


            if (isDirectory)
            {

                string[] allFiles = Directory.GetFiles(targetToCompress, "*.*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length).ToArray();



                string safeFolderPath = targetToCompress.EndsWith("\\") ? targetToCompress : targetToCompress + "\\";
                Uri folderUri = new Uri(safeFolderPath);


                int totalFiles = allFiles.Length;




                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                long completedBytes = 0;
                DeflateZipFile.TotalBytes = totalBytes;
                DeflateZipFile.ProcessedBytes = 0;
                DeflateZipFile.ProgressChanged = ProgressChanged;
                DeflateZipFile.StartTime = startTime;
                DeflateZipFile.SpeedChanged = SpeedChanged;
                DeflateZipFile.TimeChanged = TimeChanged;
                long processedBytes = 0;
                StatusChanged?.Invoke("Compressing Files...");

                using (var def = new DeflateZipFile(filepath))
                {
                    Console.WriteLine("Step 1/2: Cache-Aware Parallel Compressing Files...");


                    int uiProgressCounter = 0;
                    

                    
                    object consoleLock = new object();
                   
                    ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();
                    ConcurrentBag<CompressedEntry> compressedResults = new ConcurrentBag<CompressedEntry>();
                   
                    Parallel.For(
                        0,
                        totalFiles,
                        
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                       
                        () =>
                        {
                            return new ThreadCache();
                        },

                       
                        (i, loopState, local) =>
                        {
                            
                            string file = allFiles[i];
                            long fileSize = new FileInfo(file).Length;
                            Uri fileUri = new Uri(file);
                            string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                            
                            relativePath = relativePath.Replace("\\", "/");
                           



                            byte[] fileData = File.ReadAllBytes(file);


                            
                            CompressedEntry result =
                                local.Compressor.CompressOnly(
                                    file,
                                    relativePath,
                                    fileData);

                            compressedResults.Add(result);
                            long currentBytes = Interlocked.Add(ref completedBytes, fileSize);
                            
                            int percent = (int)(currentBytes * 100.0 / totalBytes);
                            ProgressChanged?.Invoke(percent);


                            
                            int currentProgress = Interlocked.Increment(ref uiProgressCounter);
                            FilesChanged?.Invoke($"{FormatSize(currentBytes)} / {FormatSize(totalBytes)}   ({currentProgress}/{totalFiles} files)");
                            
                            local.Count++;

                            return local;
                        },

                       
                        local =>
                        {
                            coreWorkloadReports.Add(
                                $"[Core Thread {Environment.CurrentManagedThreadId,2}] processed {local.Count,4} files exclusively.");

                            ArrayPool<byte>.Shared.Return(local.ReadBuffer);

                            local.Compressor.Dispose();
                        }
                    );

                    ulong rawTotal = 0;
                    ulong compressedTotal = 0;

                    foreach (var entry in compressedResults)
                    {
                        rawTotal += entry.OriginalSize;
                        compressedTotal += entry.CompressedSize;
                    }

                    Console.WriteLine();
                    Console.WriteLine("=== Compression Summary ===");
                    Console.WriteLine($"Files Collected : {compressedResults.Count}");
                    Console.WriteLine($"Raw Size        : {rawTotal:N0} bytes");
                    Console.WriteLine($"Compressed Size : {compressedTotal:N0} bytes");

                    double ratio = (double)compressedTotal / rawTotal * 100.0;

                    Console.WriteLine($"Ratio           : {ratio:F2}%");
                    Console.WriteLine("\n\nStep 2/2: Writing Central Directory to disk archive...");
                    
                    StatusChanged?.Invoke("Writing Central Directory...");
                    foreach (var entry in compressedResults)
                    {
                        def.AddCompressedEntry(entry);
                    }

                    def.Save();

                    Console.WriteLine("\n--- Cache Parallelism: Core Workload Distribution ---");
                   
                    foreach (var report in coreWorkloadReports.OrderBy(r => r))
                    {
                        Console.WriteLine(report);
                    }
                    Console.WriteLine("-----------------------------------------------------");
                }
                StatusChanged?.Invoke("Completed");
                Console.WriteLine("-> Cache-Aware Parallel compression completed successfully!");
            }
            else if (isFile)
            {
                string[] allFiles = new string[] { targetToCompress };
                int totalFiles = allFiles.Length;



               
                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                long completedBytes = 0;
                DeflateZipFile.TotalBytes = totalBytes;
                Console.WriteLine($"TotalBytes = {totalBytes:N0}");
                Console.WriteLine($"File Size = {new FileInfo(targetToCompress).Length:N0}");
                DeflateZipFile.ProcessedBytes = 0;
                DeflateZipFile.ProgressChanged = ProgressChanged;
                StatusChanged?.Invoke("Compressing Files...");
               
                using (var def = new DeflateZipFile(filepath))
                {
                    Console.WriteLine("Step 1/2: Cache-Aware Parallel Compressing Files...");

                   
                    int uiProgressCounter = 0;
                    
                    object consoleLock = new object();
                    
                    ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();
                    ConcurrentBag<CompressedEntry> compressedResults = new ConcurrentBag<CompressedEntry>();
                    
                    Parallel.For(
                        0,
                        totalFiles,
                       
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                       
                        () =>
                        {
                            return new ThreadCache();
                        },

                        
                        (i, loopState, local) =>
                        {

                            string file = allFiles[i];
                            long fileSize = new FileInfo(file).Length;

                            
                            string fileName = Path.GetFileName(file);
                            string folderName = Path.GetFileNameWithoutExtension(file);
                            string relativePath = folderName + "/" + fileName;



                            byte[] fileData = File.ReadAllBytes(file);


                           
                            CompressedEntry result =
                                local.Compressor.CompressOnly(
                                    file,
                                    relativePath,
                                    fileData);

                            
                            compressedResults.Add(result);
                            long currentBytes = Interlocked.Add(ref completedBytes, fileSize);

                            

                            int percent = (int)(currentBytes * 100.0 / totalBytes);
                            ProgressChanged?.Invoke(percent);


                            
                            int currentProgress = Interlocked.Increment(ref uiProgressCounter);
                            FilesChanged?.Invoke($"{FormatSize(currentBytes)} / {FormatSize(totalBytes)}   ({currentProgress}/{totalFiles} files)");
                            
                            local.Count++;

                            return local;
                        },

                        
                        local =>
                        {
                            coreWorkloadReports.Add(
                                $"[Core Thread {Environment.CurrentManagedThreadId,2}] processed {local.Count,4} files exclusively.");

                            ArrayPool<byte>.Shared.Return(local.ReadBuffer);

                            local.Compressor.Dispose();
                        }
                    );

                    ulong rawTotal = 0;
                    ulong compressedTotal = 0;

                    foreach (var entry in compressedResults)
                    {
                        rawTotal += entry.OriginalSize;
                        compressedTotal += entry.CompressedSize;
                    }

                    Console.WriteLine();
                    Console.WriteLine("=== Compression Summary ===");
                    Console.WriteLine($"Files Collected : {compressedResults.Count}");
                    Console.WriteLine($"Raw Size        : {rawTotal:N0} bytes");
                    Console.WriteLine($"Compressed Size : {compressedTotal:N0} bytes");

                    double ratio =(double)compressedTotal /rawTotal * 100.0;

                    Console.WriteLine($"Ratio           : {ratio:F2}%");
                    Console.WriteLine("\n\nStep 2/2: Writing Central Directory to disk archive...");
                   
                    StatusChanged?.Invoke("Writing Central Directory...");
                    foreach (var entry in compressedResults)
                    {
                        def.AddCompressedEntry(entry);
                    }

                    def.Save();

                    Console.WriteLine("\n--- Cache Parallelism: Core Workload Distribution ---");
                    
                    foreach (var report in coreWorkloadReports.OrderBy(r => r))
                    {
                        Console.WriteLine(report);
                    }
                    Console.WriteLine("-----------------------------------------------------");
                }
                StatusChanged?.Invoke("Completed");
                Console.WriteLine("-> Cache-Aware Parallel compression completed successfully!");
            }

        }
        private static string FormatSize(long bytes)
        {
            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / GB:F2} GB";

            if (bytes >= MB)
                return $"{bytes / MB:F1} MB";

            if (bytes >= KB)
                return $"{bytes / KB:F1} KB";

            return $"{bytes} B";
        }
        public static void de(string filepath, string tempunzip)
        {
            Console.WriteLine("\n--- [CPU & Cache Parallel] Decompression Started ---");

            
            if (tempunzip.EndsWith("\\test") || tempunzip.EndsWith("/test"))
                tempunzip = tempunzip + "_unzipped";

           
            if (!Directory.Exists(tempunzip)) Directory.CreateDirectory(tempunzip);
            int totalFiles = 0;
            string[] fileNames = null;
            long totalBytes = 0;
            try
            {
                

                
                using (var infoReader = new InflateZipFile(filepath))
                {
                    
                    totalFiles = infoReader.ZipDir.Count;
                    
                    fileNames = new string[totalFiles];
                    
                    for (int i = 0; i < totalFiles; i++)
                    {
                        dynamic entry = infoReader.ZipDir[i];
                        fileNames[i] = entry.FileName;
                        totalBytes += (long)entry.FileSize;
                    }
                }

                Console.WriteLine($"Found {totalFiles} files in zip archive.");
                Console.WriteLine("Running Cache-Aware Parallel Extraction across CPU Cores...\n");
                DateTime startTime = DateTime.Now;
                long completedBytes = 0;
                StatusChanged?.Invoke("Extracting Files...");
                
                int uiProgressCounter = 0;
                
                object consoleLock = new object();

              
                ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();

                
                Parallel.For(
                    0,
                    totalFiles,
                    
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                    
                    () => 0,

                    
                    (i, loopState, localCacheCounter) =>
                    {
                        
                        string targetFileName = fileNames[i];

                        try
                        {
                            long currentBytes = 0;
                           
                            using (var threadSafeInf = new InflateZipFile(filepath))
                            {
                                dynamic entry =
                                    threadSafeInf.ZipDir.First(x => x.FileName == targetFileName);

                                threadSafeInf.Decompress(entry, tempunzip, null, true, true);

                                currentBytes = Interlocked.Add(ref completedBytes, (long)entry.FileSize);

                                int currentProgress =
                                    Interlocked.Increment(ref uiProgressCounter);

                                int percent =Math.Min((int)(currentBytes * 100.0 / totalBytes),100);

                                ProgressChanged?.Invoke(percent);
                                FilesChanged?.Invoke($"{FormatSize(currentBytes)} / {FormatSize(totalBytes)} ({currentProgress}/{totalFiles} files)");
                            }
                            double elapsed =(DateTime.Now - startTime).TotalSeconds;

                            double speedMB =elapsed > 0? completedBytes / 1024.0 / 1024.0 / elapsed: 0;

                            SpeedChanged?.Invoke($"{speedMB:F1} MB/s");
                            TimeChanged?.Invoke(
                                (DateTime.Now - startTime)
                                    .ToString(@"hh\:mm\:ss")
                            );
                            
                            return localCacheCounter + 1;
                        }
                        catch (Exception ioEx)
                        {
                           
                            lock (consoleLock)
                            {
                                
                                Console.Write("\r".PadRight(80) + "\r");
                                Console.WriteLine($"[Warning] Skipped file [{targetFileName}]: {ioEx.Message}");
                            }
                            
                            return localCacheCounter;
                        }
                    },

                    
                    (finalLocalCacheCount) =>
                    {
                        
                        string report = $"[Core Thread {Environment.CurrentManagedThreadId,2}] processed {finalLocalCacheCount,4} files exclusively.";
                        coreWorkloadReports.Add(report);
                    }
                );

                Console.WriteLine("\n\n--- Cache Parallelism: Core Workload Distribution ---");
               
                foreach (var report in coreWorkloadReports.OrderBy(r => r))
                {
                    Console.WriteLine(report);
                }
                Console.WriteLine("-----------------------------------------------------");

            }
            catch (Exception ex)
            {
                Console.WriteLine("\nCritical error during decompression: " + ex.Message);
            }
            StatusChanged?.Invoke("Completed");

            ProgressChanged?.Invoke(100);
            FilesChanged?.Invoke($"{FormatSize(totalBytes)} / {FormatSize(totalBytes)} ({totalFiles}/{totalFiles} files)");
            Console.WriteLine($"\n-> Cache-Aware Parallel decompression ended. Target path: {tempunzip}");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {

            //string folderToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Fruits and Vegetables Image Recognition Dataset.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Fruits and Vegetables Image Recognition Dataset";


            //string folderToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Drone Videos";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Drone Videos.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Drone Videos";


            //string folderToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\cache_image.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\cazhe_image";

            string folderToCompress = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\New folder";
            string filepath = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\zip\deflate\CPUcache\test.zip";
            string tempunzip = @"C:\Users\KUEK\Downloads\pass zk\pass zk\Dataset\zip\deflate\CPUcache\zip";




    
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

      
            Stopwatch totalStopwatch = new Stopwatch();
            totalStopwatch.Start();

       
            //compress(filepath, folderToCompress);


            //de(filepath, tempunzip);


            totalStopwatch.Stop();

            //show Benchmark
            Console.WriteLine("\n========================================");
            Console.WriteLine("  CPU & CACHE PARALLEL PERFORMANCE REPORT  ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Architecture:         Cache-Aware Parallelism (TLS)");
            Console.WriteLine($"CPU Cores Utilized:   {Environment.ProcessorCount} Physical/Logical Cores");
            Console.WriteLine($"Total Execution Time: {totalStopwatch.Elapsed.TotalSeconds:F4} seconds");
            Console.WriteLine($"Total Milliseconds:   {totalStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine("========================================");

            Console.WriteLine("\nAll tasks completed successfully! Press any key to exit.");
            Console.ReadKey();
        }


        static void DrawProgressBar(string taskName, long processedBytes,long totalBytes, DateTime startTime, int width = 30)
        {

            double percentage =
    totalBytes == 0
    ? 1.0
    : (double)processedBytes / totalBytes;

            int progressBlocks =
                (int)(percentage * width);

            string progressBar =
                new string('█', progressBlocks) +
                new string('-', width - progressBlocks);

            TimeSpan elapsed =
                DateTime.Now - startTime;

            double speedMB =
                elapsed.TotalSeconds > 0
                ? processedBytes / 1024.0 / 1024.0 / elapsed.TotalSeconds
                : 0;

            double currentMB =
                processedBytes / 1024.0 / 1024.0;

            double totalMB =
                totalBytes / 1024.0 / 1024.0;

            Console.Write(
                $"\r{taskName,-20} " +
                $"[{progressBar}] " +
                $"{percentage * 100,6:0.0}% " +
                $"({currentMB:N1}MB / {totalMB:N1}MB) " +
                $"Elapsed:{elapsed:hh\\:mm\\:ss} " +
                $"Speed:{speedMB:0.0} MB/s"
            );
        }

        
        
        
    }
}