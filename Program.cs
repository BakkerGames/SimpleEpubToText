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
            int blanklines = 0;
            foreach (string currline in lines)
            {
                int pos;
                int pos2;
                string s = currline;
                if (s == "###")
                {
                    s = ":Chapter"; // non-display chapter titles
                }
                s = s.Replace("###", ""); // remove chapter markers
                s = s.Replace("_i1_", "<i>").Replace("_i0_", "</i>"); // adjust italic to html
                s = s.Replace("_b1_", "").Replace("_b0_", ""); // remove bold
                s = s.Replace("_u1_", "").Replace("_u0_", ""); // remove underline
                s = s.Replace("_s1_", "").Replace("_s0_", ""); // remove small
                s = s.Replace("_small1_", "").Replace("_small0_", ""); // remove small
                s = s.Replace("_sup1_", "<sup>").Replace("_sup0_", "</sup>"); // adjust superpos to html
                s = s.Replace("_sub1_", "<sub>").Replace("_sub0_", "</sub>"); // adjust subpos to html
                s = s.Replace("_p_", "\t"); // beginning of paragraphs
                s = s.Replace("_t_", "\t"); // blockquote
                if (s.Contains("_t"))
                {
                    s = s.Replace("_table1_", "<table>");
                    s = s.Replace("_table0_", "</table>");
                    s = s.Replace("_tr1_", "<tr>");
                    s = s.Replace("_tr0_", "</tr>");
                    s = s.Replace("_th1_", "<th>");
                    s = s.Replace("_th0_", "</th>");
                    s = s.Replace("_td1_", "<td>");
                    s = s.Replace("_td0_", "</td>");
                }
                s = s.Replace("_code1_", "<code>");
                s = s.Replace("_code0_", "</code>");
                s = s.Replace("&mdash;", "—").Replace("—-", "—").Replace("-—", "—");
                s = s.Replace("&ndash;", "-");
                pos = s.IndexOf("_image:");
                while (pos >= 0)
                {
                    s = s.Substring(0, pos) + "<image=" + s.Substring(pos + 7);
                    pos2 = s.IndexOf("_", pos);
                    if (pos2 >= 0)
                    {
                        s = s.Substring(0, pos2) + ">" + s.Substring(pos2 + 1);
                    }
                    pos = s.IndexOf("_image:");
                }
                // cleanup simple issues
                if (s.EndsWith(" </i>"))
                {
                    s = s.Substring(0, s.Length - 4).TrimEnd() + "</i>";
                }
                while (s.Contains("  ") && !s.Contains("_code"))
                {
                    s = s.Replace("  ", " ");
                }
                if (s.Contains("</i> <i>"))
                {
                    s = s.Replace("</i> <i>", " ");
                }
                if (s.Contains("</i><i>"))
                {
                    s = s.Replace("</i><i>", "");
                }
                while (s.Contains("  ") && !s.Contains("_code"))
                {
                    s = s.Replace("  ", " ");
                }
                while (s.Contains("\t "))
                {
                    s = s.Replace("\t ", "\t");
                }
                while (s.EndsWith("\t") || s.EndsWith(" "))
                {
                    s = s.Substring(0, s.Length - 1);
                }
                // check for blanklines
                if (s == "")
                {
                    if (blanklines < 2) // max of 2 lines
                    {
                        blanklines++;
                    }
                }
                else
                {
                    while (blanklines > 0)
                    {
                        result.AppendLine();
                        blanklines--;
                    }
                    result.AppendLine(s);
                }
            }
            return result.ToString();
        }
    }
}
