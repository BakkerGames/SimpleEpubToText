using System.Text;

namespace SimpleEpubToText;

class Program
{
    private static int foundCount = 0;
    private static int changedCount = 0;
    private static int errorCount = 0;
    private static int maxFiles = -1; // all

    static int Main(string[] args)
    {
        try
        {
            string? fromPath = null;
            string? toPath = null;
            bool quickFlag = false;
            bool forceFlag = false;
            bool bareFormat = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("/") || args[i].StartsWith("--"))
                {
                    if (args[i].StartsWith("/max=") || args[i].StartsWith("--max="))
                    {
                        maxFiles = int.Parse(args[i][(args[i].IndexOf('=') + 1)..]);
                    }
                    if (args[i] == "/force" || args[i] == "--force")
                    {
                        forceFlag = true;
                    }
                    if (args[i] == "/quick" || args[i] == "--quick")
                    {
                        quickFlag = true;
                    }
                    if (args[i] == "/bare" || args[i] == "--bare")
                    {
                        bareFormat = true;
                    }
                }
                else if (fromPath == null)
                {
                    fromPath = args[i];
                }
                else if (toPath == null)
                {
                    toPath = args[i];
                }
                else
                {
                    Console.WriteLine($"Unknown argument {args[i]}");
                    return 1;
                }
            }
            if (fromPath == null)
            {
                Console.WriteLine("From path not specified");
                return 1;
            }
            if (toPath == null)
            {
                toPath = fromPath;
            }
            Console.WriteLine($"SimpleEpubToText: \"{fromPath}\" to \"{toPath}\"");
            ConvertAllEpub(fromPath, toPath, forceFlag, quickFlag, bareFormat);
            Console.WriteLine("\r      ");
            Console.WriteLine($"Files found:   {foundCount}");
            Console.WriteLine($"Files changed: {changedCount}");
            if (errorCount > 0)
            {
                Console.WriteLine($"Errors:        {errorCount}");
            }
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

    private static void ConvertAllEpub(string fromFolder, string toFolder, bool forceFlag, bool quickFlag, bool bareFormat)
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
            try
            {
                if (maxFiles == 0)
                {
                    break;
                }
                foundCount++;
                if (DoConversion(fromFolder, toFolder, Path.GetFileName(filepath), forceFlag, quickFlag, bareFormat))
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
            catch (Exception ex)
            {
                Console.WriteLine($"\rError converting \"{filepath}\"\r\n- {ex.Message}");
                errorCount++;
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
            ConvertAllEpub(Path.Combine(fromFolder, subdir), Path.Combine(toFolder, subdir), forceFlag, quickFlag, bareFormat);
        }
    }

    private static bool DoConversion(string fromFolder, string toFolder, string inFilename, bool forceFlag, bool quickFlag, bool bareFormat)
    {
        string outFilename = inFilename.Replace(".epub", "").Replace("_nodrm", "") + ".txt";
        string inFileFullPath = Path.Combine(fromFolder, inFilename);
        string outFileFullPath = Path.Combine(toFolder, outFilename);

        if (File.Exists(outFileFullPath) && quickFlag)
        {
            // check if outfile is newer than infile
            FileInfo inFI = new(inFileFullPath);
            FileInfo outFI = new(outFileFullPath);
            if (inFI.LastWriteTimeUtc < outFI.LastWriteTimeUtc)
            {
                return false;
            }
        }

        EbookLoader ebook;
        try
        {
            ebook = new EbookLoader(inFileFullPath);
        }
        catch (Exception)
        {
            // cannot load ebook
            Console.WriteLine($"\rError: {inFilename}");
            return false;
        }

        StringBuilder s = new();
        bool firstChapter = true;
        if (ebook != null && ebook.Chapters != null)
        {
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
        }
        string outFileText = EbookReformat.ReformatEbook(s.ToString(), bareFormat);
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
