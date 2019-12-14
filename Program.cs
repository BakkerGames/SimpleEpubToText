using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleEpubToText
{
    class Program
    {
        private static int foundCount = 0;
        private static int changedCount = 0;

        static int Main(string[] args)
        {
            try
            {
                if (args is null || args.Count() < 2)
                {
                    Console.WriteLine("Syntax: <from_path> <to_path>");
                    return 1;
                }
                Console.WriteLine("SimpleEpubToText: {0} {1}", args[0], args[1]);
                ConvertAllEpub(args[0], args[1]);
                Console.WriteLine();
                Console.WriteLine($"Files changed: {changedCount}");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 2;
            }
            finally
            {
#if DEBUG
                Console.Write("Press enter to continue...");
                Console.ReadLine();
#endif
            }
        }

        private static void ConvertAllEpub(string fromFolder, string toFolder)
        {
            if (!Directory.Exists(toFolder))
            {
                Directory.CreateDirectory(toFolder);
            }
            foreach (string filepath in Directory.EnumerateFiles(fromFolder, "*.epub", SearchOption.TopDirectoryOnly))
            {
                foundCount++;
                if (DoConversion(fromFolder, toFolder, Path.GetFileName(filepath)))
                {
                    changedCount++;
                    Console.Write("\r");
                    Console.Write(Path.GetFileName(filepath));
                    Console.WriteLine();
                }
                else
                {
                    Console.Write($"\r{foundCount}");
                }
            }
            foreach (string ds in Directory.EnumerateDirectories(fromFolder))
            {
                string subdir = Path.GetFileName(ds);
                if (subdir.StartsWith("."))
                {
                    continue;
                }
                ConvertAllEpub(Path.Combine(fromFolder, subdir), Path.Combine(toFolder, subdir));
            }
        }

        private static bool DoConversion(string fromFolder, string toFolder, string filename)
        {
            EbookLoader ebook = new EbookLoader(Path.Combine(fromFolder, filename));
            string outFilename = filename.Replace(".epub", ".txt");
            StringBuilder s = new StringBuilder();
            bool firstChapter = true;
            foreach (Chapter c in ebook.Chapters)
            {
                if (!firstChapter)
                {
                    s.AppendLine();
                    s.AppendLine();
                }
                firstChapter = false;
                s.Append("###");
                foreach (string p in c.Paragraphs)
                {
                    s.AppendLine(p);
                }
            }
            string outFileFullPath = Path.Combine(toFolder, outFilename);
            if (File.Exists(outFileFullPath))
            {
                string oldFile = File.ReadAllText(outFileFullPath);
                if (oldFile == s.ToString())
                {
                    return false;
                }
            }
            File.WriteAllText(outFileFullPath, s.ToString());
            throw new SystemException();
            //return true;
        }
    }
}
