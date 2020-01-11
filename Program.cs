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
                if (args is null || args.Count() < 2)
                {
                    Console.WriteLine("Syntax: <from_path> <to_path>");
                    return 1;
                }
                Console.WriteLine("SimpleEpubToText: \"{0}\" to \"{1}\"", args[0], args[1]);
                if (args.Count() >= 3)
                {
                    if (args[2].StartsWith("/max="))
                    {
                        maxFiles = int.Parse(args[2].Substring(5));
                    }
                }
                ConvertAllEpub(args[0], args[1]);
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

        private static void ConvertAllEpub(string fromFolder, string toFolder)
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
                if (DoConversion(fromFolder, toFolder, Path.GetFileName(filepath)))
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
                ConvertAllEpub(Path.Combine(fromFolder, subdir), Path.Combine(toFolder, subdir));
            }
        }

        private static bool DoConversion(string fromFolder, string toFolder, string filename)
        {
            EbookLoader ebook = new EbookLoader(Path.Combine(fromFolder, filename));
            string outFilename = filename.Replace(".epub", "").Replace("_nodrm", "").Replace(".", "_") + ".txt";
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
            string outFileText = ReformatEbook(s.ToString());
            string outFileFullPath = Path.Combine(toFolder, outFilename);
            if (File.Exists(outFileFullPath))
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

        private static string ReformatEbook(string value)
        {
            StringBuilder result = new StringBuilder();
            string[] lines = value.Replace("\r", "").Split('\n');
            int blankLines = 0;
            foreach (string currline in lines)
            {
                string s = ConvertLine(currline);
                // check for blanklines
                if (s == "")
                {
                    if (blankLines < 2) // max of 2 lines
                    {
                        blankLines++;
                    }
                }
                else
                {
                    while (blankLines > 0)
                    {
                        result.AppendLine();
                        blankLines--;
                    }
                    result.AppendLine(s);
                }
            }
            return result.ToString();
        }

        private static string ConvertLine(string s)
        {
            StringBuilder result = new StringBuilder();
            bool inTag = false;
            bool inCode = false;
            bool lastSpace = false;
            string tag = "";
            foreach (char c in s)
            {
                if (c == ' ')
                {
                    if (lastSpace && !inCode && !inTag)
                    {
                        continue;
                    }
                    lastSpace = true;
                }
                else
                {
                    lastSpace = false;
                }
                if (c == '_')
                {
                    if (inTag)
                    {
                        if (tag == "code1")
                        {
                            inCode = true;
                        }
                        else if (tag == "code0")
                        {
                            inCode = false;
                        }
                        string newTag = ProcessTag(tag);
                        if (newTag == "" && result.Length > 0)
                        {
                            if (result[result.Length - 1] == ' ')
                            {
                                lastSpace = true;
                            }
                        }
                        result.Append(newTag);
                        inTag = false;
                    }
                    else
                    {
                        inTag = true;
                        tag = "";
                    }
                }
                else if (inTag)
                {
                    tag += c;
                }
                else
                {
                    result.Append(c);
                }
            }
            string newResult = result.ToString();
            if (newResult == "###")
            {
                newResult = ":Chapter"; // non-display chapter titles
            }
            else if (newResult.StartsWith("###"))
            {
                newResult = newResult.Substring(3);
            }
            if (newResult.Contains("</i> <i>"))
            {
                newResult = newResult.Replace(" </i> <i> ", " ");
                newResult = newResult.Replace(" </i> <i>", " ");
                newResult = newResult.Replace("</i> <i> ", " ");
                newResult = newResult.Replace("</i> <i>", " ");
            }
            if (newResult.Contains("</i><i>"))
            {
                newResult = newResult.Replace(" </i><i> ", " ");
                newResult = newResult.Replace("</i><i>", "");
            }
            if (newResult.Contains("&mdash;"))
            {
                newResult = newResult.Replace("&mdash;", "—");
                while (newResult.Contains("—-"))
                {
                    newResult = newResult.Replace("—-", "—");
                }
                while (newResult.Contains("-—"))
                {
                    newResult = newResult.Replace("-—", "—");
                }
                //while (newResult.Contains("———"))
                //{
                //    newResult = newResult.Replace("———", "——");
                //}
            }
            if (newResult.Contains("&ndash;"))
            {
                newResult = newResult.Replace("&ndash;", "-");
            }
            if (newResult.Contains("&#32;"))
            {
                newResult = newResult.Replace("&#32;", " ");
            }
            if (newResult.Contains("&#95;"))
            {
                newResult = newResult.Replace("&#95;", "_");
            }
            while (newResult.EndsWith(" ") || newResult.EndsWith("\t"))
            {
                newResult = newResult.Substring(0, newResult.Length - 1);
            }
            if (newResult.EndsWith(" </i>"))
            {
                newResult = newResult.Substring(0, newResult.Length - 4).TrimEnd() + "</i>";
            }
            while (newResult.Contains("\t "))
            {
                newResult = newResult.Replace("\t ", "\t");
            }
            return newResult;
        }

        private static string ProcessTag(string tag)
        {
            switch (tag)
            {
                case "p":
                    return "\t";
                case "t":
                    return "\t";
                case "i1":
                    return "<i>";
                case "i0":
                    return "</i>";
                case "caption1":
                    return "<caption>";
                case "caption0":
                    return "</caption>";
                case "code1":
                    return "<code>";
                case "code0":
                    return "</code>";
                case "sub1":
                    return "<sub>";
                case "sub0":
                    return "</sub>";
                case "sup1":
                    return "<sup>";
                case "sup0":
                    return "</sup>";
                case "table1":
                    return "<table>";
                case "table0":
                    return "</table>";
                case "tr1":
                    return "<tr>";
                case "tr0":
                    return "</tr>";
                case "th1":
                    return "<th>";
                case "th0":
                    return "</th>";
                case "td1":
                    return "<td>";
                case "td0":
                    return "</td>";
            }
            if (tag.StartsWith("image:"))
            {
                return $"<image={tag.Substring(6)}>";
            }
            return ""; // remove tag
        }
    }
}


//// do these after others
//// cleanup simple issues
//while (s.EndsWith("\t") || s.EndsWith(" "))
//{
//    s = s.Substring(0, s.Length - 1);
//}
//// check for blanklines
//if (s == "")
//{
//    if (blanklines < 2) // max of 2 lines
//    {
//        blanklines++;
//    }
//}
//else
//{
//    while (blanklines > 0)
//    {
//        result.AppendLine();
//        blanklines--;
//    }
//    result.AppendLine(s);
//}
