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
            //防呆设计：如果那个位置已经有同名的ZIP了，就先把它删掉以免冲突
            try { if (File.Exists(filepath)) File.Delete(filepath); } catch { }

            bool isDirectory = Directory.Exists(targetToCompress);
            bool isFile = File.Exists(targetToCompress);
            

            //如果源文件夹根本不存在，直接走人
            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"Error: Target not found: {targetToCompress}");
                return;
            }
            DateTime startTime = DateTime.Now;


            if (isDirectory)
            {
                //翻箱倒柜把源文件夹里所有的文件路径都找出来
                string[] allFiles = Directory.GetFiles(targetToCompress, "*.*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length).ToArray();



                string safeFolderPath = targetToCompress.EndsWith("\\") ? targetToCompress : targetToCompress + "\\";
                Uri folderUri = new Uri(safeFolderPath);


                int totalFiles = allFiles.Length;



                //处理一下文件夹路径的斜杠，为了后面算相对路径用
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
                //召唤我们魔改后的ZIP压缩引擎
                using (var def = new DeflateZipFile(filepath))
                {
                    Console.WriteLine("Step 1/2: Cache-Aware Parallel Compressing Files...");

                    //用来记录控制台UI画到哪里的全局计数器
                    int uiProgressCounter = 0;
                    //因为往同一个ZIP里写东西必须排队，所以准备一把"ZIP文件锁"

                    //准备一把"控制台锁"，防止多个核心同时抢着画进度条导致画面崩溃
                    object consoleLock = new object();
                    //准备一个超级安全的大口袋，用来装每个CPU核心下班后交上来的工作报告
                    ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();
                    ConcurrentBag<CompressedEntry> compressedResults = new ConcurrentBag<CompressedEntry>();
                    //【核心黑科技登场：Cache-AwareParallelism(缓存感知并行)】
                    Parallel.For(
                        0,
                        totalFiles,
                        //自动把机器上所有的CPU核心都拉过来干活
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                        //1.给每个刚来上班的CPU核心发一个小本子(L1Cache计数器)，初始值写上0
                        () =>
                        {
                            return new ThreadCache();
                        },

                        //2.这是每个CPU核心疯狂干活的区域
                        (i, loopState, local) =>
                        {
                            //拿到当前要处理的文件路径
                            string file = allFiles[i];
                            long fileSize = new FileInfo(file).Length;
                            Uri fileUri = new Uri(file);
                            string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                            //把Windows习惯的反斜杠换成通用的正斜杠
                            relativePath = relativePath.Replace("\\", "/");
                            //【破局点1：绝对并行无锁读盘！】



                            byte[] fileData = File.ReadAllBytes(file);


                            //【破局点2：极速内存排队压缩】
                            //读完数据要真正塞进唯一的ZIP包里了，这里必须排队！不然ZIP文件就坏了

                            //lock (zipLock)
                            //{
                            //调用我们魔改的"内存直写"大招，直接把内存里的数据拍进去
                            //def.CompressFromMemory(file, fileData, relativePath);
                            CompressedEntry result =
                                local.Compressor.CompressOnly(
                                    file,
                                    relativePath,
                                    fileData);

                            compressedResults.Add(result);
                            long currentBytes = Interlocked.Add(ref completedBytes, fileSize);
                            //Console.WriteLine(
                            //    $"{result.FileName}");

                            //Console.WriteLine(
                            //    $"Raw={result.OriginalSize}");

                            //Console.WriteLine(
                            //    $"Compressed={result.CompressedSize}");
                            //}
                            int percent = (int)(currentBytes * 100.0 / totalBytes);
                            ProgressChanged?.Invoke(percent);


                            //【破局点3：UI节流防卡顿】
                            //如果每压一个文件就画一次进度条，电脑会被拖慢
                            int currentProgress = Interlocked.Increment(ref uiProgressCounter);
                            FilesChanged?.Invoke($"{FormatSize(currentBytes)} / {FormatSize(totalBytes)}   ({currentProgress}/{totalFiles} files)");
                            //设定每搞定20个文件才画一次(或者刚好是最后一个文件才画)



                            //核心干完活了，在自己的专属小本子(L1Cache)上偷偷记上一笔：我又搞定了一个！全程无锁极快！
                            local.Count++;

                            return local;
                        },

                        //3.当某个CPU核心把分给它的活全干完了，准备下线时触发
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
                    //等所有核心都下班后，主线程单枪匹马把ZIP的"中央目录"写进文件尾部，正式封口
                    StatusChanged?.Invoke("Writing Central Directory...");
                    foreach (var entry in compressedResults)
                    {
                        def.AddCompressedEntry(entry);
                    }

                    def.Save();

                    Console.WriteLine("\n--- Cache Parallelism: Core Workload Distribution ---");
                    //把大口袋里的报告倒出来，按照核心编号排个序，一行行打印出来给你看战果
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



                //处理一下文件夹路径的斜杠，为了后面算相对路径用
                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                long completedBytes = 0;
                DeflateZipFile.TotalBytes = totalBytes;
                Console.WriteLine($"TotalBytes = {totalBytes:N0}");
                Console.WriteLine($"File Size = {new FileInfo(targetToCompress).Length:N0}");
                DeflateZipFile.ProcessedBytes = 0;
                DeflateZipFile.ProgressChanged = ProgressChanged;
                StatusChanged?.Invoke("Compressing Files...");
                //召唤我们魔改后的ZIP压缩引擎
                using (var def = new DeflateZipFile(filepath))
                {
                    Console.WriteLine("Step 1/2: Cache-Aware Parallel Compressing Files...");

                    //用来记录控制台UI画到哪里的全局计数器
                    int uiProgressCounter = 0;
                    //因为往同一个ZIP里写东西必须排队，所以准备一把"ZIP文件锁"

                    //准备一把"控制台锁"，防止多个核心同时抢着画进度条导致画面崩溃
                    object consoleLock = new object();
                    //准备一个超级安全的大口袋，用来装每个CPU核心下班后交上来的工作报告
                    ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();
                    ConcurrentBag<CompressedEntry> compressedResults = new ConcurrentBag<CompressedEntry>();
                    //【核心黑科技登场：Cache-AwareParallelism(缓存感知并行)】
                    Parallel.For(
                        0,
                        totalFiles,
                        //自动把机器上所有的CPU核心都拉过来干活
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                        //1.给每个刚来上班的CPU核心发一个小本子(L1Cache计数器)，初始值写上0
                        () =>
                        {
                            return new ThreadCache();
                        },

                        //2.这是每个CPU核心疯狂干活的区域
                        (i, loopState, local) =>
                        {

                            string file = allFiles[i];
                            long fileSize = new FileInfo(file).Length;

                            //【终极路径修正】：提取单文件名，并用去掉后缀的名称作为逻辑文件夹包裹它
                            //这招叫同名虚拟文件夹封装，既能让Windows直接秒开，又能满足开源库底层对'/'的严格要求
                            string fileName = Path.GetFileName(file);
                            string folderName = Path.GetFileNameWithoutExtension(file);
                            string relativePath = folderName + "/" + fileName;



                            byte[] fileData = File.ReadAllBytes(file);


                            //【破局点2：极速内存排队压缩】
                            //读完数据要真正塞进唯一的ZIP包里了，这里必须排队！不然ZIP文件就坏了

                            //lock (zipLock)
                            //{
                            //调用我们魔改的"内存直写"大招，直接把内存里的数据拍进去
                            //def.CompressFromMemory(file, fileData, relativePath);
                            CompressedEntry result =
                                local.Compressor.CompressOnly(
                                    file,
                                    relativePath,
                                    fileData);

                            
                            compressedResults.Add(result);
                            long currentBytes = Interlocked.Add(ref completedBytes, fileSize);

                            //Console.WriteLine(
                            //    $"{result.FileName}");

                            //Console.WriteLine(
                            //    $"Raw={result.OriginalSize}");

                            //Console.WriteLine(
                            //    $"Compressed={result.CompressedSize}");
                            //}

                            int percent = (int)(currentBytes * 100.0 / totalBytes);
                            ProgressChanged?.Invoke(percent);


                            //【破局点3：UI节流防卡顿】
                            //如果每压一个文件就画一次进度条，电脑会被拖慢
                            int currentProgress = Interlocked.Increment(ref uiProgressCounter);
                            FilesChanged?.Invoke($"{FormatSize(currentBytes)} / {FormatSize(totalBytes)}   ({currentProgress}/{totalFiles} files)");
                            //设定每搞定20个文件才画一次(或者刚好是最后一个文件才画)



                            //核心干完活了，在自己的专属小本子(L1Cache)上偷偷记上一笔：我又搞定了一个！全程无锁极快！
                            local.Count++;

                            return local;
                        },

                        //3.当某个CPU核心把分给它的活全干完了，准备下线时触发
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
                    //等所有核心都下班后，主线程单枪匹马把ZIP的"中央目录"写进文件尾部，正式封口
                    StatusChanged?.Invoke("Writing Central Directory...");
                    foreach (var entry in compressedResults)
                    {
                        def.AddCompressedEntry(entry);
                    }

                    def.Save();

                    Console.WriteLine("\n--- Cache Parallelism: Core Workload Distribution ---");
                    //把大口袋里的报告倒出来，按照核心编号排个序，一行行打印出来给你看战果
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

            //防呆设计：清理一下解压的文件夹路径
            if (tempunzip.EndsWith("\\test") || tempunzip.EndsWith("/test"))
                tempunzip = tempunzip + "_unzipped";

            //如果解压的目标文件夹不存在，就建一个
            if (!Directory.Exists(tempunzip)) Directory.CreateDirectory(tempunzip);
            int totalFiles = 0;
            string[] fileNames = null;
            long totalBytes = 0;
            try
            {
                

                //1.战前准备：单线程去摸底
                //先用一个临时解压引擎去读一下ZIP包
                using (var infoReader = new InflateZipFile(filepath))
                {
                    //数清楚里面有多少个文件
                    totalFiles = infoReader.ZipDir.Count;
                    //根据总数创建一个装文件名的数组
                    fileNames = new string[totalFiles];
                    //把ZIP包里所有的文件名都抄到数组里，等下分给各个核心去干活
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
                //用于UI进度条的全局计数器
                int uiProgressCounter = 0;
                //画进度条专用的"控制台锁"
                object consoleLock = new object();

                //【解压版的CacheParallelism】：同样准备一个装工作报告的大口袋
                ConcurrentBag<string> coreWorkloadReports = new ConcurrentBag<string>();

                //【全功率纯血并行循环】开始！
                Parallel.For(
                    0,
                    totalFiles,
                    //让机器上所有的CPU核心全部投入战斗
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },

                    //1.依然是给每个核心发一个初始值为0的专属小本子(L1Cache)
                    () => 0,

                    //2.核心的疯狂解压区(这里连硬盘写入都不用排队，纯血并行！)
                    (i, loopState, localCacheCounter) =>
                    {
                        //拿到当前被分配到的文件名
                        string targetFileName = fileNames[i];

                        try
                        {
                            long currentBytes = 0;
                            //【最牛逼的地方】：每个核心自己掏出一把专属的"解压枪"(实例化独立的引擎)
                            //因为大家各有各的枪，写的是不同的物理文件，所以直接对着硬盘疯狂扫射，彻底不需要lock排队！
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
                            //在自己的小本子上记下一笔，纯正的缓存并行无锁自增！
                            return localCacheCounter + 1;
                        }
                        catch (Exception ioEx)
                        {
                            //如果在解压某个文件时出了错(比如文件被占用)
                            lock (consoleLock)
                            {
                                //把光标移到最前面清除进度条，把警告打印出来
                                Console.Write("\r".PadRight(80) + "\r");
                                Console.WriteLine($"[Warning] Skipped file [{targetFileName}]: {ioEx.Message}");
                            }
                            //出错了就不加分了，直接返回原来的数字
                            return localCacheCounter;
                        }
                    },

                    //3.核心下班，提交最终报告进大口袋
                    (finalLocalCacheCount) =>
                    {
                        //整理报告并上交
                        string report = $"[Core Thread {Environment.CurrentManagedThreadId,2}] processed {finalLocalCacheCount,4} files exclusively.";
                        coreWorkloadReports.Add(report);
                    }
                );

                Console.WriteLine("\n\n--- Cache Parallelism: Core Workload Distribution ---");
                //把大口袋里的工作报告按顺序打印出来，向教授证明你真的榨干了这台机器的每一个核心
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




            //直接把当前程序的CPU优先级拉到最高，防止其他软件来抢资源和污染缓存
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            //准备一个高精度秒表用来跑分
            Stopwatch totalStopwatch = new Stopwatch();
            totalStopwatch.Start();

            //1.测试压缩(如果需要测试的话把前面的注释删掉)
            //compress(filepath, folderToCompress);

            //2.测试解压(这是带着CacheParallel黑科技的完全体)
            //de(filepath, tempunzip);

            //按下秒表停止计时
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

        //---用来在控制台画进度条的工具人函数---
        static void DrawProgressBar(string taskName, long processedBytes,long totalBytes, DateTime startTime, int width = 30)
        {
            //算出当前进度占总数的百分比
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