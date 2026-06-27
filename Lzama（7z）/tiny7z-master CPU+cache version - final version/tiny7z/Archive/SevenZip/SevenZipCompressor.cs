using pdj.tiny7z.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static pdj.tiny7z.Archive.SevenZipStreamsCompressor;

namespace pdj.tiny7z.Archive
{
    /// <summary>
    /// 7zip compressor class to compress files into an archive.
    /// </summary>
    public class SevenZipCompressor : ICompressor
    {
        #region Public Properties
        public IReadOnlyList<ArchiveFile> Files
        {
            get; private set;
        }

        public ProgressDelegate ProgressDelegate
        {
            get; set;
        }

        public bool CompressHeader
        {
            get; set;
        }

        public bool PreserveDirectoryStructure
        {
            get; set;
        }

        public bool Solid
        {
            get; set;
        }
        #endregion Public Properties

        #region Public Methods
        public void Dispose() // IDisposable
        {
            if (this.stream != null && this.header != null)
            {
                Finalize();
            }
            this.stream = null;
            this.header = null;
        }

        public ICompressor AddDirectory(string inputDirectory, string archiveDirectory = null, bool recursive = true)
        {
            Trace.TraceInformation($"Adding files from directory `{inputDirectory}`.");
            Trace.Indent();
            try
            {
                inputDirectory = new Uri(inputDirectory).LocalPath;
                if (!Directory.Exists(inputDirectory))
                    throw new ArgumentException($"Input directory `{inputDirectory}` does not exist.");
                archiveDirectory = (archiveDirectory ?? "").Replace('\\', '/').Trim('/').Trim() + '/';

                List<SevenZipArchiveFile> addedFiles = new List<SevenZipArchiveFile>();
                if (PreserveDirectoryStructure && recursive)
                {
                    foreach (var dir in new DirectoryInfo(inputDirectory).EnumerateDirectories("*.*", SearchOption.AllDirectories))
                    {
                        string dirName = (archiveDirectory + dir.FullName.Substring(inputDirectory.Length).Replace('\\', '/').TrimStart('/')).TrimStart('/');
                        Trace.TraceInformation("Adding: " + dirName);
                        addedFiles.Add(new SevenZipArchiveFile()
                        {
                            Name = dirName,
                            Size = 0,
                            Time = dir.LastWriteTimeUtc,
                            Attributes = (UInt32)dir.Attributes,
                            IsEmpty = true,
                            IsDirectory = true,
                            IsDeleted = false,
                        });
                    }
                }

                foreach (var file in new DirectoryInfo(inputDirectory).EnumerateFiles("*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    string fileName = (archiveDirectory + (PreserveDirectoryStructure ? file.FullName.Substring(inputDirectory.Length).Replace('\\', '/').TrimStart('/') : Path.GetFileName(file.FullName))).TrimStart('/');
                    Trace.TraceInformation("Adding: " + fileName);
                    addedFiles.Add(new SevenZipArchiveFile()
                    {
                        Name = fileName,
                        Size = (UInt64)file.Length,
                        Time = (DateTime)file.LastWriteTimeUtc,
                        Attributes = (UInt32)file.Attributes,
                        IsEmpty = (file.Length == 0),
                        IsDirectory = false,
                        IsDeleted = false,
                        Source = new MultiFileStream.Source(file.FullName)
                    });
                }
                _Files.AddRange(addedFiles);
            }
            finally
            {
                Trace.Unindent();
                Trace.TraceInformation("Done adding files.");
            }

            return this;
        }

        public ICompressor AddFile(string inputFileName, string archiveFileName = null)
        {
            Trace.TraceInformation($"Adding file `{inputFileName}`.");
            if (!File.Exists(inputFileName))
                throw new ArgumentException($"File `{Path.GetFileName(inputFileName)}` does not exist.");
            if (archiveFileName == null)
                archiveFileName = Path.GetFileName(inputFileName);
            archiveFileName = archiveFileName.Substring(Path.GetPathRoot(archiveFileName).Length).Replace('\\', '/');

            var fileInfo = new FileInfo(inputFileName);
            _Files.Add(new SevenZipArchiveFile()
            {
                Name = archiveFileName,
                Size = (UInt64)fileInfo.Length,
                Time = (DateTime)fileInfo.LastWriteTimeUtc,
                Attributes = (UInt32)fileInfo.Attributes,
                IsEmpty = (fileInfo.Length == 0),
                IsDirectory = false,
                IsDeleted = false,
                Source = new MultiFileStream.Source(inputFileName)
            });

            return this;
        }

        public ICompressor AddFile(Stream stream, string archiveFileName, DateTime? time = null)
        {
            Trace.TraceInformation($"Adding file `{archiveFileName}` from stream.");
            archiveFileName = archiveFileName.Substring(Path.GetPathRoot(archiveFileName).Length).Replace('\\', '/');

            _Files.Add(new SevenZipArchiveFile()
            {
                Name = archiveFileName,
                Size = (UInt64)stream.Length,
                Time = time,
                Attributes = 0,
                IsEmpty = (stream.Length == 0),
                IsDirectory = false,
                IsDeleted = false,
                Source = new MultiFileStream.Source(stream)
            });

            return this;
        }

        public ICompressor Finalize()
        {
            if (this.stream == null || this.header == null)
                throw new SevenZipException("Compressor object has already been finalized.");

            Trace.TraceInformation($"Compressing files.");
            Trace.Indent();
            try
            {
                //Console.WriteLine("F1");
                buildFilesInfo();
                //Console.WriteLine("F2");
                // index files
                var streamToFileIndex = new Dictionary<ulong, ulong>();
                ulong streamIndex = 0;
                for (long i = 0; i < _Files.LongCount(); ++i)
                {
                    if (!_Files[(int)i].IsEmpty)
                    {
                        _Files[(int)i].UnPackIndex = streamIndex;
                        streamToFileIndex[streamIndex++] = (ulong)i;
                    }
                }

                // progress object
                SevenZipProgressProvider szpp = null;
                if (ProgressDelegate != null)
                    szpp = new SevenZipProgressProvider(_Files, new ulong[0], ProgressDelegate);

                // compress files
                this.header.RawHeader.MainStreamsInfo = new SevenZipHeader.StreamsInfo();
                //Console.WriteLine("F3");
                if (Solid && streamIndex > 1)
                {
                    compressFilesSolid(streamIndex, streamToFileIndex, szpp);
                }
                else
                {
                    compressFilesNonSolid(streamIndex, streamToFileIndex, szpp);
                }
                //Console.WriteLine("F4");

                // write headers
                writeHeaders();
            }
            finally
            {
                Trace.Unindent();
                Trace.TraceInformation("Done compressing files.");
            }

            this.stream = null;
            this.header = null;
            return this;
        }
        #endregion Public Methods

        #region Internal Constructors
        internal SevenZipCompressor(Stream stream, SevenZipHeader header)
        {
            this.stream = stream;
            this.header = header;

            if (stream == null || !stream.CanWrite)
                throw new ArgumentNullException("Stream isn't suitable for compression.");

            if (header == null)
                throw new ArgumentNullException("Header has not been prepared properly.");

            // init file list
            _Files = new List<SevenZipArchiveFile>();
            Files = _Files;

            // default values
            ProgressDelegate = null;
            CompressHeader = true;
            PreserveDirectoryStructure = true;
            Solid = true;
        }
        #endregion Internal Constructors

        #region Private Fields
        private Stream stream;
        private SevenZipHeader header;
        private List<SevenZipArchiveFile> _Files;
        #endregion Private Fields

        #region Private Methods
        private void compressFilesSolid(ulong numStreams, Dictionary<ulong, ulong> streamToFileIndex, SevenZipProgressProvider progressProvider)
        {
            var sc = new SevenZipStreamsCompressor(stream);
            sc.Method = Compression.Registry.Method.LZMA;

            Trace.TraceInformation($"Compressing `{numStreams} files` into a solid block...");

            // actual compression using a sequence file stream and stream compressor
            var inputStream = new MultiFileStream(
                FileAccess.Read,
                streamToFileIndex.Select(sfi => _Files[(int)sfi.Value].Source).ToArray());
            SevenZipStreamsCompressor.PackedStream cs = sc.Compress(inputStream, progressProvider);

            // build headers
            var streamsInfo = this.header.RawHeader.MainStreamsInfo;
            streamsInfo.PackInfo = new SevenZipHeader.PackInfo()
            {
                NumPackStreams = cs.NumStreams,
                PackPos = 0,
                Sizes = cs.Sizes,
                Digests = new SevenZipHeader.Digests(cs.NumStreams)
                {
                    CRCs = cs.CRCs
                }
            };
            streamsInfo.UnPackInfo = new SevenZipHeader.UnPackInfo()
            {
                NumFolders = 1,
                Folders = new SevenZipHeader.Folder[1]
                {
                    cs.Folder
                }
            };
            streamsInfo.UnPackInfo.Folders[0].UnPackCRC = null;
            streamsInfo.SubStreamsInfo = new SevenZipHeader.SubStreamsInfo(streamsInfo.UnPackInfo)
            {
                NumUnPackStreamsInFolders = new UInt64[1]
                {
                    numStreams
                },
                NumUnPackStreamsTotal = numStreams,
                UnPackSizes = new List<UInt64>((int)numStreams),
                Digests = new SevenZipHeader.Digests(numStreams)
            };
            for (ulong i = 0; i < numStreams; ++i)
            {
                streamsInfo.SubStreamsInfo.UnPackSizes.Add((UInt64)inputStream.Sizes[i]);
                streamsInfo.SubStreamsInfo.Digests.CRCs[i] = inputStream.CRCs[i];
            }
        }

        private void compressFilesNonSolid(ulong numStreams, Dictionary<ulong, ulong> streamToFileIndex, SevenZipProgressProvider progressProvider)
        {
            var sc = new SevenZipStreamsCompressor(stream);
            sc.Method = Compression.Registry.Method.LZMA;
            //Console.WriteLine("NS1");
            // actual compression (into a single packed stream per file)
            //SevenZipStreamsCompressor.PackedStream[] css = new SevenZipStreamsCompressor.PackedStream[numStreams];
            PackedStream[] css = new PackedStream[numStreams];
            //Console.WriteLine("NS2");
            ulong numPackStreams = 0;
            long completedBytes = 0;
            object progressLock = new object();
            ulong totalBytes = 0;

            for (ulong i = 0; i < numStreams; i++)
            {
                SevenZipArchiveFile file =
                    _Files[(int)streamToFileIndex[i]];

                totalBytes += (ulong)file.Size;
            }
            long lastShown = 0;
            Stopwatch progressTimer = Stopwatch.StartNew();
            Parallel.For( 0, (int)numStreams, new ParallelOptions{MaxDegreeOfParallelism = 4}, i =>
            {
                
                var localCompressor = new SevenZipStreamsCompressor(new MemoryStream());

                localCompressor.Method = Compression.Registry.Method.LZMA;

                SevenZipArchiveFile file = _Files[(int)streamToFileIndex[(ulong)i]];

                try
                {
                    using (Stream source = file.Source.Get(FileAccess.Read))
                    {
                        css[i] = localCompressor.Compress(source, null);
                    }
                    Interlocked.Add(ref completedBytes,(long)file.Size);
                    long current =Interlocked.Read(ref completedBytes);
                    if (current - lastShown > 2 * 1024 * 1024)
                    {
                        lock (progressLock)
                        {
                            lastShown = current;
                            double percent = (double)current * 100.0 / totalBytes;
                            TimeSpan elapsed = progressTimer.Elapsed;
                            Console.Write(
                               $"\rProgress: {percent:F1}% " +
                               $"({current / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB) " +
                               $"Elapsed:{elapsed:mm\\:ss}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED FILE INDEX={i}");
                    Console.WriteLine(file.Name);
                    Console.WriteLine(ex);

                    throw;
                }
            });
           

            
            long totalCompressed = 0;

            for (ulong i = 0; i < numStreams; i++)
            {
                totalCompressed += css[i].CompressedData.Length;
            }

           
            for (ulong i = 0; i < numStreams; i++)
            {
                numPackStreams += css[i].NumStreams;
            }
            
            for (ulong i = 0; i < numStreams; i++)
            {
                stream.Write(css[i].CompressedData,0, css[i].CompressedData.Length);
            }
          

            //Console.WriteLine("NS4");
            // build headers
            var streamsInfo = this.header.RawHeader.MainStreamsInfo;
            streamsInfo.PackInfo = new SevenZipHeader.PackInfo()
            {
                NumPackStreams = numPackStreams,
                PackPos = 0,
                Sizes = new UInt64[numPackStreams],
                Digests = new SevenZipHeader.Digests(numPackStreams)
            };
            streamsInfo.UnPackInfo = new SevenZipHeader.UnPackInfo()
            {
                NumFolders = numStreams,
                Folders = new SevenZipHeader.Folder[numStreams]
            };

            streamsInfo.SubStreamsInfo = new SevenZipHeader.SubStreamsInfo(streamsInfo.UnPackInfo)
            {
                NumUnPackStreamsInFolders = Enumerable.Repeat((UInt64)1, (int)numStreams).ToArray(),
                NumUnPackStreamsTotal = numStreams,
                UnPackSizes = new List<UInt64>((int)numStreams),
                Digests = new SevenZipHeader.Digests(numStreams)
            };

            for (ulong i = 0, k = 0; i < numStreams; ++i)
            {
                for (ulong j = 0; j < css[i].NumStreams; ++j, ++k)
                {
                    streamsInfo.PackInfo.Sizes[k] = css[i].Sizes[j];
                    streamsInfo.PackInfo.Digests.CRCs[k] = css[i].CRCs[j];
                }
                streamsInfo.SubStreamsInfo.UnPackSizes.Add((UInt64)css[i].Folder.GetUnPackSize());
                streamsInfo.SubStreamsInfo.Digests.CRCs[i] = css[i].Folder.UnPackCRC;
                css[i].Folder.UnPackCRC = null;
                streamsInfo.UnPackInfo.Folders[i] = css[i].Folder;
            }
        }

        private void writeHeaders()
        {
            // current position is defined as end of packed streams and beginning of header
            long endOfPackedStreamsPosition = this.stream.Position;

            // write headers in temporary stream
            var headerStream = new MemoryStream();
            header.Write(headerStream);

            // go through compressing again for headers
            if (CompressHeader)
            {
                // get compressor and default codec
                var sc = new SevenZipStreamsCompressor(stream);
                sc.Method = Compression.Registry.Method.LZMA;

                // compress
                headerStream.Position = 0;
                SevenZipStreamsCompressor.PackedStream cs = sc.Compress(headerStream, null);

                // create encoded header
                SevenZipHeader.StreamsInfo headerStreamsInfo = new SevenZipHeader.StreamsInfo()
                {
                    PackInfo = new SevenZipHeader.PackInfo()
                    {
                        NumPackStreams = cs.NumStreams,
                        PackPos = (UInt64)(endOfPackedStreamsPosition - Marshal.SizeOf(typeof(SevenZipArchive.SignatureHeader))),
                        Sizes = cs.Sizes,
                        Digests = new SevenZipHeader.Digests(1)
                        {
                            CRCs = cs.CRCs
                        }
                    },
                    UnPackInfo = new SevenZipHeader.UnPackInfo()
                    {
                        Folders = new SevenZipHeader.Folder[1] { cs.Folder },
                        NumFolders = 1
                    }
                };

                // bait and switch
                header.RawHeader = null;
                header.EncodedHeader = headerStreamsInfo;

                // write new header in headerStream
                headerStream.Dispose();
                headerStream = new MemoryStream();
                header.Write(headerStream);

                // update new end of packed position for encoded header
                endOfPackedStreamsPosition = this.stream.Position;
            }

            // create start header and calculate header crc
            headerStream.Position = 0;
            var startHeader = new SevenZipArchive.StartHeader()
            {
                NextHeaderOffset = (UInt64)(endOfPackedStreamsPosition - Marshal.SizeOf(typeof(SevenZipArchive.SignatureHeader))),
                NextHeaderSize = (UInt64)headerStream.Length,
                NextHeaderCRC = new CRC().Calculate(headerStream).Result
            };

            // write headers at the end of output stream
            headerStream.Position = 0;
            headerStream.CopyTo(stream);
            headerStream.Dispose();

            // regenerate signature header with positions and crcs
            var signatureHeader = new SevenZipArchive.SignatureHeader()
            {
                Signature = SevenZipArchive.kSignature.ToArray(),
                ArchiveVersion = new SevenZipArchive.ArchiveVersion()
                {
                    Major = 0,
                    Minor = 2,
                },
                StartHeaderCRC = new CRC().Calculate(startHeader.GetByteArray()).Result,
                StartHeader = startHeader
            };

            // write start header and flush all pending writes
            stream.Position = 0;
            stream.Write(signatureHeader.GetByteArray(), 0, Marshal.SizeOf(signatureHeader));
            stream.Flush();
        }

        private void buildFilesInfo()
        {
            // scan files for empty streams and files (directories and zero-length files)

            bool[] emptyStreams = new bool[_Files.LongCount()];
            UInt64 numEmptyStreams = 0;
            bool[] emptyFiles = new bool[_Files.LongCount()];
            UInt64 numEmptyFiles = 0;
            for (long i = 0; i < _Files.LongCount(); ++i)
            {
                var file = _Files[(int)i];
                if (file.IsEmpty)
                {
                    emptyStreams[i] = true;
                    emptyFiles[numEmptyStreams++] = !file.IsDirectory;

                    numEmptyFiles += (file.IsDirectory ? (ulong)0 : 1);
                }
                else
                {
                    emptyStreams[i] = false;
                }
            }
            Array.Resize(ref emptyFiles, (int)numEmptyStreams);

            // add properties to file property headers

            var propertyEmptyStream = new SevenZipHeader.PropertyEmptyStream((ulong)_Files.LongCount())
            {
                IsEmptyStream = emptyStreams,
                NumEmptyStreams = numEmptyStreams
            };
            var propertyEmptyFile = new SevenZipHeader.PropertyEmptyFile((ulong)_Files.LongCount(), numEmptyStreams)
            {
                IsEmptyFile = emptyFiles
            };
            var propertyName = new SevenZipHeader.PropertyName((ulong)_Files.LongCount())
            {
                Names = new string[_Files.LongCount()]
            };
            var propertyTime = new SevenZipHeader.PropertyTime(SevenZipHeader.PropertyID.kMTime, (ulong)_Files.LongCount())
            {
                Times = new DateTime?[_Files.LongCount()]
            };
            var propertyAttr = new SevenZipHeader.PropertyAttributes((ulong)_Files.LongCount())
            {
                Attributes = new UInt32?[_Files.LongCount()]
            };
            for (long i = 0; i < _Files.LongCount(); ++i)
            {
                propertyName.Names[i] = _Files[(int)i].Name;
                propertyTime.Times[i] = _Files[(int)i].Time;
                propertyAttr.Attributes[i] = _Files[(int)i].Attributes;
            }

            // create header and add FilesInfo elements

            var header = this.header.RawHeader;
            header.FilesInfo = new SevenZipHeader.FilesInfo()
            {
                NumFiles = (ulong)_Files.LongCount(),
                NumEmptyStreams = numEmptyStreams
            };
            if (numEmptyStreams > 0)
                header.FilesInfo.Properties.Add(propertyEmptyStream);
            if (numEmptyFiles > 0)
                header.FilesInfo.Properties.Add(propertyEmptyFile);
            header.FilesInfo.Properties.AddRange(new SevenZipHeader.FileProperty[] { propertyName, propertyTime, propertyAttr });
        }
        #endregion Private Methods
    }
}
