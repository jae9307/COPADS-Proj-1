using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Diagnostics;

namespace DirectoryCounter {
    class Counter {
        private int numFolders;
        private int numFiles;
        private int numBytes;
        private readonly object updateLock = new object();

        private Counter()
        {
            this.numFolders = 0;
            this.numFiles = 0;
            this.numBytes = 0;
        }

        private static void helpMessage()
        {
            System.Console.Write(@"Usage: du[-s][-d][-b] < path >
                                Summarize disk usage of the set of FILES, recursively for directories.
                                1
                                You MUST specify one of the parameters, -s, -d, or - b
                                - s Run in single threaded mode
                                - d Run in parallel mode(uses all available processors)
                                - b Run in both parallel and single threaded mode.
                                Runs parallel followed by sequential mode");
        }
        
        private void parallelCount(string path) {
            try 
            {
                string[] dirs = Directory.GetDirectories(path);
                Parallel.ForEach(dirs, dir => {
                    parallelCount(dir);
                });

                string[] files = Directory.GetFiles(path);
                Parallel.ForEach(files, file => {
                    lock(updateLock)
                    {
                        this.numFiles++;
                        this.numBytes += file.Length;
                    }
                
                });
                lock(updateLock)
                {
                    this.numFolders++;
                }
            }
            catch (System.UnauthorizedAccessException) { }
            catch (System.AggregateException) { }
            catch (System.IO.DirectoryNotFoundException) { }
        }

        private void sequentialCount(string path) {
            try
            {
                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                {
                    parallelCount(dir);
                }

                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    this.numFiles++;
                    this.numBytes += file.Length;
                }
                this.numFolders++;
            }
            catch (System.UnauthorizedAccessException) { }
            catch (System.AggregateException) { }
            catch (System.IO.DirectoryNotFoundException) { }
        }

        private static void runParallel(string path) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Counter parallel = new Counter();
            parallel.parallelCount(path);

            stopwatch.Stop();
            double timeElapsed = stopwatch.Elapsed.TotalSeconds;

            System.Console.WriteLine($"Parallel Calculated in: {timeElapsed}");
            System.Console.WriteLine($"{parallel.numFolders} folders, {parallel.numFiles} files, {parallel.numBytes} bytes");
        }

        private static void runSequential(string path)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Counter sequential = new Counter();
            sequential.sequentialCount(path);

            stopwatch.Stop();
            double timeElapsed = stopwatch.Elapsed.TotalSeconds;

            System.Console.WriteLine($"Sequential Calculated in: {timeElapsed}");
            System.Console.WriteLine($"{sequential.numFolders} folders, {sequential.numFiles} files, {sequential.numBytes} bytes");
        }
        
        public static void Main(string[] args) {
            string mode = args[0];
            string path = args[1];
            // string root = Directory.GetDirectoryRoot(path);
            if (mode.Equals("-d"))
            {
                runParallel(path);
            }
            else if (mode.Equals("-s")) {
                runSequential(path);
            }
            else if (mode.Equals("-b")) {
                runParallel(path);
                runSequential(path);
            }
            else
            {
                Console.WriteLine("Fail!");
            }
        }
    }
}