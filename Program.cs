using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wspolbiezne_3
{
    internal class Program
    {
        private static long _totalWords = 0;
        private static readonly ConcurrentBag<(string file, long count, int threadId)> _results = new();

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0)
            {
                Console.WriteLine("Użycie: wspolbiezne_3 <ścieżka_do_folderu_z_txt> [ścieżka_wyjścia]");
                return 1;
            }

            string root = args[0];
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"Błąd: folder nie istnieje → {root}");
                return 2;
            }

            string outDir = args.Length >= 2 ? args[1] : Path.Combine(Environment.CurrentDirectory, "wyniki");
            if (WordCounter.IsSameDirectory(root, outDir))
            {
                Console.WriteLine("Błąd: katalog wyników musi być inny niż katalog wejściowy.");
                return 3;
            }
            Directory.CreateDirectory(outDir);

            IEnumerable<string> files = WordCounter.EnumerateInputFiles(root, outDir);
            var partitioner = Partitioner.Create(files, EnumerablePartitionerOptions.NoBuffering);
            var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            var sw = System.Diagnostics.Stopwatch.StartNew();

            Parallel.ForEach(partitioner, po, file =>
            {
                long count = WordCounter.CountWordsStream(file);
                int id = Thread.CurrentThread.ManagedThreadId;

                _results.Add((file, count, id));
                Interlocked.Add(ref _totalWords, count);
            });

            sw.Stop();

            string logPath = Path.Combine(outDir, "log.txt");
            string csvPath = Path.Combine(outDir, "wyniki.csv");

            using (var log = new StreamWriter(logPath, false, new UTF8Encoding(false)))
            {
                foreach (var item in _results)
                    log.WriteLine($"{item.threadId} -> {Path.GetFileName(item.file)}: {item.count} słów");
                log.WriteLine($"Łączna liczba słów: {_totalWords}");
            }

            using (var csv = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
            {
                csv.WriteLine("plik,liczba_slow");
                foreach (var item in _results.OrderBy(r => r.file, StringComparer.OrdinalIgnoreCase))
                    csv.WriteLine($"{QuoteCsv(item.file)},{item.count}");
            }

            Console.WriteLine($"Łączna liczba słów: {_totalWords}");
            Console.WriteLine($"Zapisano: {logPath}");
            Console.WriteLine($"Zapisano: {csvPath}");
            Console.WriteLine($"Czas: {sw.Elapsed}");

            return 0;
        }

        private static string QuoteCsv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    }

    internal static class WordCounter
    {
        private static StringComparison PathComparison => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        internal static bool IsSameDirectory(string first, string second) => string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
            PathComparison);

        internal static IEnumerable<string> EnumerateInputFiles(string root, string outDir)
        {
            if (IsSameDirectory(root, outDir))
                throw new ArgumentException("Katalog wyników musi być inny niż katalog wejściowy.", nameof(outDir));

            string rootPath = WithTrailingSeparator(Path.GetFullPath(root));
            string outputPath = WithTrailingSeparator(Path.GetFullPath(outDir));
            bool outputIsInsideRoot = outputPath.StartsWith(rootPath, PathComparison);

            return Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
                .Where(file => !outputIsInsideRoot || !Path.GetFullPath(file).StartsWith(outputPath, PathComparison));
        }

        private static string WithTrailingSeparator(string path) => Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;

        internal static long CountWordsStream(string path)
        {
            long count = 0;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            char[] buffer = new char[1024 * 1024];
            int read;
            bool insideWord = false;
            while ((read = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    if (char.IsWhiteSpace(buffer[i]))
                        insideWord = false;
                    else if (!insideWord)
                    {
                        count++;
                        insideWord = true;
                    }
                }
            }
            return count;
        }
    }
}
