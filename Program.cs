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
        private static int maxFiles = -1; // all

        static int Main(string[] args)
        {
            try
            {
                bool quickFlag = false;
                bool forceFlag = false;
                if (args is null || args.Count() < 2)
                {
                    Console.WriteLine("Syntax: <from_path> <to_path>");
                    return 1;
                }
                Console.WriteLine("SimpleEpubToText: \"{0}\" to \"{1}\"", args[0], args[1]);
                if (args.Count() >= 3)
                {
                    for (int i = 2; i < args.Count(); i++)
                    {
                        if (args[i].StartsWith("/max="))
                        {
                            maxFiles = int.Parse(args[i].Substring(5));
                        }
                        if (args[i] == "/force")
                        {
                            forceFlag = true;
                        }
                        if (args[i] == "/quick")
                        {
                            quickFlag = true;
                        }
                    }
                }
                ConvertAllEpub(args[0], args[1], forceFlag, quickFlag);
                Console.WriteLine("\r      ");
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

        private static void ConvertAllEpub(string fromFolder, string toFolder, bool forceFlag, bool quickFlag)
        {
            if (maxFiles == 0)
            {
                return;
            }
            if (!Directory.Exists(toFolder))
            {
                Directory.CreateDirectory(toFolder);
            }
            foreach (string filepath in Directory.EnumerateFiles(fromFolder, "*.epub", SearchOption.TopDirectoryOnly))
            {
                if (maxFiles == 0)
                {
                    break;
                }
                foundCount++;
                if (DoConversion(fromFolder, toFolder, Path.GetFileName(filepath), forceFlag, quickFlag))
                {
                    changedCount++;
                    Console.Write("\r");
                    Console.Write(Path.GetFileName(filepath).Replace("_nodrm", ""));
                    Console.WriteLine();
                }
                else
                {
                    Console.Write($"\r{foundCount}");
                }
                if (maxFiles > 0)
                {
                    maxFiles--;
                }
            }
            foreach (string ds in Directory.EnumerateDirectories(fromFolder))
            {
                if (maxFiles == 0)
                {
                    break;
                }
                string subdir = Path.GetFileName(ds);
                if (subdir.StartsWith("."))
                {
                    continue;
                }
                ConvertAllEpub(Path.Combine(fromFolder, subdir), Path.Combine(toFolder, subdir), forceFlag, quickFlag);
            }
        }

        private static bool DoConversion(string fromFolder, string toFolder, string inFilename, bool forceFlag, bool quickFlag)
        {
            string outFilename = inFilename.Replace(".epub", "").Replace("_nodrm", "").Replace(".", "_") + ".txt";
            string inFileFullPath = Path.Combine(fromFolder, inFilename);
            string outFileFullPath = Path.Combine(toFolder, outFilename);

            if (File.Exists(outFileFullPath) && quickFlag)
            {
                // check if outfile is newer than infile
                FileInfo inFI = new FileInfo(inFileFullPath);
                FileInfo outFI = new FileInfo(outFileFullPath);
                if (inFI.LastWriteTimeUtc < outFI.LastWriteTimeUtc)
                {
                    return false;
                }
            }

            EbookLoader ebook = new EbookLoader(inFileFullPath);
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
                foreach (string p in c.Paragraphs)
                {
                    s.AppendLine(p);
                }
            }
            string outFileText = EbookReformat.ReformatEbook(s.ToString());
            if (string.IsNullOrEmpty(outFileText))
            {
                // books with only pictures (Calvin and Hobbes)
                return false;
            }
            if (File.Exists(outFileFullPath) && !forceFlag)
            {
                string oldFileText = File.ReadAllText(outFileFullPath);
                if (oldFileText == outFileText)
                {
                    return false;
                }
            }
            File.WriteAllText(outFileFullPath, outFileText);
            return true;
        }
    }
}
