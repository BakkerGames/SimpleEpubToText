using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VersOne.Epub;

namespace SimpleEpubToText
{
    public class EbookLoader
    {
        private readonly string _ebookPath;
        private EpubBook book;
        private List<EpubTextContentFile> contentFiles;

        public List<Chapter> Chapters;

        public EbookLoader(string ebookPath)
        {
            if (!File.Exists(ebookPath))
            {
                throw new ArgumentException($"File not found: {ebookPath}");
            }
            _ebookPath = ebookPath;
            EpubToChapters(_ebookPath);
        }

        #region Private Routines

        private void EpubToChapters(string ebookPath)
        {
            book = EpubReader.ReadBook(ebookPath);
            contentFiles = book.ReadingOrder.ToList();
            Chapters = new List<Chapter>();
            for (int i = 0; i < contentFiles.Count; i++)
            {
                Chapter chapter = new Chapter();
                bool foundBody = false;
                bool firstLine = true;
                string[] contentText = contentFiles[i].Content
                                                      .Replace("\r\n", "\n")
                                                      .Replace("</p>", "\n")
                                                      .Split('\n');
                foreach (string s in contentText)
                {
                    string s1 = s;
                    if (!foundBody)
                    {
                        int pos = s1.IndexOf("<body>");
                        if (pos < 0)
                        {
                            pos = s1.IndexOf("<body ");
                        }
                        if (pos >= 0)
                        {
                            s1 = s1.Substring(pos); // remove everything before <body>
                            foundBody = true;
                        }
                    }
                    if (foundBody)
                    {
                        s1 = s1.Replace("<p>", "\n_p_");
                        s1 = Regex.Replace(s1, "<p [^>]*>", "\n_p_");
                        s1 = Regex.Replace(s1, "<br[^>]*>", "_br_");
                        s1 = Regex.Replace(s1, "<image[^>]*href *= *\"([a-z.]*)\" */>", "_image:$1_");
                        s1 = s1.Replace("<i>", "_i1_");
                        s1 = Regex.Replace(s1, "<i [^>]*>", "_i1_");
                        s1 = s1.Replace("</i>", "_i0_");
                        s1 = s1.Replace("<em>", "_i1_");
                        s1 = Regex.Replace(s1, "<em [^>]*>", "_i1_");
                        s1 = s1.Replace("</em>", "_i0_");
                        s1 = Regex.Replace(s1, "<[^>]*>", "").Trim();
                        s1 = Regex.Replace(s1, "   *", " ");
                        s1 = s1.Replace("_p_ ", "_p_");
                        s1 = s1.Replace("_br_ ", "_br_");
                        s1 = s1.Replace(" _br_", "_br_");
                        s1 = s1.Trim();
                        string[] split1 = s1.Split('\n');
                        foreach (string currLine in split1)
                        {
                            string s2 = currLine.Trim();
                            chapter.Paragraphs.Add(s2);
                        }
                        //if (s1.Length > 0)
                        //{
                        //    if (s1.StartsWith("_p_"))
                        //    {
                        //        s1 = s1.Substring(3);
                        //    }
                        //    else if (!firstLine || s1.Length > 0)
                        //    {
                        //        s1 = "\t" + s1;
                        //    }
                        //    while (s1.Contains("\n"))
                        //    {
                        //        string s2 = s1.Substring(0, s1.IndexOf("\n")).TrimEnd();
                        //        chapter.Paragraphs.Add(s2);
                        //        if (firstLine)
                        //        {
                        //            chapter.Paragraphs.Add("");
                        //        }
                        //        firstLine = false;
                        //        s1 = s1.Substring(s1.IndexOf("\n") + 1).Trim();
                        //        if (s1.Length > 0)
                        //        {
                        //            s1 = "\t" + s1;
                        //        }
                        //    }
                        //    chapter.Paragraphs.Add(s1);
                        //    if (firstLine)
                        //    {
                        //        chapter.Paragraphs.Add("");
                        //    }
                        //    firstLine = false;
                        //}
                    }
                }
                if (chapter.Paragraphs.Count > 0)
                {
                    Chapters.Add(chapter);
                }
            }
        }

        #endregion
    }

    public class Chapter
    {
        public List<string> Paragraphs = new List<string>();
    }

}
