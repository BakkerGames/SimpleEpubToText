using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                bool secondLine = false;
                StringBuilder currline = new StringBuilder();

                // split all the items into html tags and raw text
                string[] contentText = contentFiles[i].Content
                                                      .Replace("\r", "")
                                                      .Replace("\n", "")
                                                      .Replace(">", ">\n")
                                                      .Replace("<", "\n<")
                                                      .Split('\n');
                foreach (string s in contentText)
                {
                    string s2 = s.Trim();
                    if (s2.Length == 0) continue;
                    if (!s2.StartsWith("<"))
                    {
                        if (foundBody)
                            currline.Append(s2);
                        continue;
                    }
                    string tag = GetTag(s2);
                    switch (tag)
                    {
                        case "body":
                            foundBody = true;
                            break;
                        case "/body":
                            foundBody = false;
                            break;
                        case "p":
                            break;
                        case "/p":
                        case "br":
                        case "/li":
                        case "hr":
                            if (firstLine)
                            {
                                chapter.Paragraphs.Add("###" + currline.ToString());
                                chapter.Paragraphs.Add("");
                                firstLine = false;
                                secondLine = true;
                            }
                            else if (tag == "hr")
                            {
                                if (currline.Length > 0)
                                {
                                    chapter.Paragraphs.Add(currline.ToString());
                                }
                                chapter.Paragraphs.Add("_p_---");
                                secondLine = false;
                            }
                            else if (tag == "/li" && currline.Length > 0)
                            {
                                chapter.Paragraphs.Add("_p__t_* " + currline.ToString());
                                secondLine = false;
                            }
                            else if (!secondLine || currline.Length > 0)
                            {
                                chapter.Paragraphs.Add("_p_" + currline.ToString());
                                secondLine = false;
                            }
                            currline.Clear();
                            break;
                        case "span":
                        case "/span":
                        case "div":
                        case "/div":
                        case "a":
                        case "/a":
                        case "ul":
                        case "/ul":
                        case "li":
                        case "svg":
                        case "/svg":
                            break;
                        default:
                            if (foundBody)
                            {
                                currline.Append("<");
                                currline.Append(tag);
                                currline.Append(">");
                            }
                            break;
                    }
                }
                if (currline.Length > 0)
                {
                    chapter.Paragraphs.Add(currline.ToString());
                }
                if (chapter.Paragraphs.Count > 0 &&
                    chapter.Paragraphs[0].ToLower() != "###table of contents" &&
                    chapter.Paragraphs[0].ToLower() != "###contents")
                {
                    while (chapter.Paragraphs[chapter.Paragraphs.Count - 1] == "_p_")
                    {
                        chapter.Paragraphs.RemoveAt(chapter.Paragraphs.Count - 1);
                    }
                    Chapters.Add(chapter);
                }
            }
        }

        private string GetTag(string s2)
        {
            int pos = s2.IndexOf(">");
            int pos2 = s2.IndexOf(" ");
            if (pos2 >= 0)
            {
                return s2.Substring(1, pos2 - 1);
            }
            return s2.Substring(1, pos - 1);
        }

        //private void EpubToChapters2(string ebookPath)
        //{
        //    book = EpubReader.ReadBook(ebookPath);
        //    contentFiles = book.ReadingOrder.ToList();
        //    Chapters = new List<Chapter>();
        //    for (int i = 0; i < contentFiles.Count; i++)
        //    {
        //        Chapter chapter = new Chapter();
        //        bool foundBody = false;
        //        bool firstLine = true;
        //        bool secondLine = false;
        //        bool joinLines = false;
        //        string[] contentText = contentFiles[i].Content
        //                                              .Replace("\r", "")
        //                                              .Replace("\n", "")
        //                                              .Replace("</p>", "</p>\n")
        //                                              .Split('\n');
        //        foreach (string s in contentText)
        //        {
        //            string s1 = s.Trim();
        //            if (!foundBody)
        //            {
        //                int pos = s1.IndexOf("<body>");
        //                if (pos < 0)
        //                {
        //                    pos = s1.IndexOf("<body ");
        //                }
        //                if (pos >= 0)
        //                {
        //                    s1 = s1.Substring(pos); // remove everything before <body>
        //                    foundBody = true;
        //                }
        //            }
        //            if (foundBody)
        //            {
        //                s1 = s1.Replace("<p>", "\n_p_");
        //                s1 = Regex.Replace(s1, "<p [^>]*>", "\n_p_");
        //                s1 = Regex.Replace(s1, "<br[^>]*>", "_br_");
        //                s1 = s1.Replace("</ul>", "\n_p_");
        //                // images
        //                if (s1.Contains("<img"))
        //                {
        //                    s1 = Regex.Replace(s1, "<img [^>]*src *= *\"([^\"]*)\"[^>]*/>", "_image:$1_");
        //                    s1 = Regex.Replace(s1, "<image[^>]*href *= *\"([^\"]*)\"[^>]*/>", "_image:$1_");
        //                }
        //                // misc types
        //                s1 = s1.Replace("<li>", "_br__t_* ");
        //                s1 = Regex.Replace(s1, "<li [^>]*>", "_br__t_* ");
        //                s1 = s1.Replace("<hr>", "_br__hr_");
        //                s1 = Regex.Replace(s1, "<hr [^>]*>", "_br__hr_");
        //                s1 = s1.Replace("<blockquote>", "_br__t_");
        //                s1 = Regex.Replace(s1, "<blockquote [^>]*>", "_br__t_");
        //                // italics
        //                s1 = s1.Replace("<i>", "_i1_");
        //                s1 = Regex.Replace(s1, "<i [^>]*>", "_i1_");
        //                s1 = s1.Replace("</i>", "_i0_");
        //                s1 = s1.Replace("<em>", "_i1_");
        //                s1 = Regex.Replace(s1, "<em [^>]*>", "_i1_");
        //                s1 = s1.Replace("</em>", "_i0_");
        //                while (s1.Contains("<span class=\"italic\">"))
        //                {
        //                    int pos = s1.IndexOf("<span class=\"italic\">");
        //                    s1 = s1.Substring(0, pos) + "_i1_" + s1.Substring(pos + 21);
        //                    int pos2 = s1.IndexOf("</span>", pos);
        //                    int pos3 = s1.IndexOf("<span ", pos);
        //                    while (pos3 >= 0 && pos3 < pos2)
        //                    {
        //                        pos2 = s1.IndexOf("</span>", pos2 + 1);
        //                        pos3 = s1.IndexOf("<span ", pos3 + 1);
        //                    }
        //                    s1 = s1.Substring(0, pos2) + "_i0_" + s1.Substring(pos2 + 7);
        //                }
        //                // bold
        //                s1 = s1.Replace("<b>", "_b1_");
        //                s1 = Regex.Replace(s1, "<b [^>]*>", "_b1_");
        //                s1 = s1.Replace("</b>", "_b0_");
        //                while (s1.Contains("<span class=\"bold\">"))
        //                {
        //                    int pos = s1.IndexOf("<span class=\"bold\">");
        //                    s1 = s1.Substring(0, pos) + "_b1_" + s1.Substring(pos + 19);
        //                    int pos2 = s1.IndexOf("</span>", pos);
        //                    int pos3 = s1.IndexOf("<span ", pos);
        //                    while (pos3 >= 0 && pos3 < pos2)
        //                    {
        //                        pos2 = s1.IndexOf("</span>", pos2 + 1);
        //                        pos3 = s1.IndexOf("<span ", pos3 + 1);
        //                    }
        //                    s1 = s1.Substring(0, pos2) + "_b0_" + s1.Substring(pos2 + 7);
        //                }
        //                // underline
        //                s1 = s1.Replace("<u>", "_u1_");
        //                s1 = Regex.Replace(s1, "<u [^>]*>", "_u1_");
        //                s1 = s1.Replace("</u>", "_u0_");
        //                while (s1.Contains("<span class=\"underline\">"))
        //                {
        //                    int pos = s1.IndexOf("<span class=\"underline\">");
        //                    s1 = s1.Substring(0, pos) + "_u1_" + s1.Substring(pos + 24);
        //                    int pos2 = s1.IndexOf("</span>", pos);
        //                    int pos3 = s1.IndexOf("<span ", pos);
        //                    while (pos3 >= 0 && pos3 < pos2)
        //                    {
        //                        pos2 = s1.IndexOf("</span>", pos2 + 1);
        //                        pos3 = s1.IndexOf("<span ", pos3 + 1);
        //                    }
        //                    s1 = s1.Substring(0, pos2) + "_u0_" + s1.Substring(pos2 + 7);
        //                }
        //                // quotes
        //                s1 = s1.Replace("“", "\"");
        //                s1 = s1.Replace("”", "\"");
        //                s1 = s1.Replace("‘", "'");
        //                s1 = s1.Replace("’", "'");
        //                s1 = s1.Replace("`", "'");
        //                // sup and sub
        //                s1 = s1.Replace("<sup>", "_sup1_");
        //                s1 = Regex.Replace(s1, "<sup [^>]*>", "_sup1_");
        //                s1 = s1.Replace("</sup>", "_sup0_");
        //                s1 = s1.Replace("<sub>", "_sub1_");
        //                s1 = Regex.Replace(s1, "<sub [^>]*>", "_sub1_");
        //                s1 = s1.Replace("</sub>", "_sub0_");
        //                // clean up
        //                s1 = Regex.Replace(s1, "<[^>]*>", "").Trim();
        //                s1 = Regex.Replace(s1, "   *", " ");
        //                s1 = s1.Replace("_p_ ", "_p_");
        //                s1 = s1.Replace("_br_ ", "_br_");
        //                s1 = s1.Replace(" _br_", "_br_");
        //                s1 = Regex.Replace(s1, "_t_  *", "_t_");
        //                s1 = s1.Trim();
        //                // ignore blank first lines
        //                if (firstLine && s1.Length == 0)
        //                {
        //                    continue;
        //                }
        //                if (joinLines)
        //                {
        //                    if (!s1.StartsWith("_p_") && !s1.StartsWith("_image:"))
        //                    {
        //                        s1 = "_p_" + s1;
        //                    }
        //                    joinLines = false;
        //                }
        //                if (s1.EndsWith("_br_"))
        //                {
        //                    joinLines = true;
        //                    s1 = s1.Substring(0, s1.Length - 4);
        //                }
        //                s1 = s1.Replace("_i0__i1_", "");
        //                s1 = s1.Replace("_b0__b1_", "");
        //                s1 = s1.Replace("_br_", "\n_p_");
        //                string[] split1 = s1.Split('\n');
        //                foreach (string currLine in split1)
        //                {
        //                    string s2 = currLine.Trim();
        //                    if (s2.Length == 0)
        //                    {
        //                        continue;
        //                    }
        //                    if (firstLine && s2.StartsWith("_p_"))
        //                    {
        //                        s2 = s2.Substring(3).TrimStart();
        //                    }
        //                    if (secondLine)
        //                    {
        //                        if (s2 == "_p_")
        //                        {
        //                            continue;
        //                        }
        //                        secondLine = false;
        //                    }
        //                    if (s2.StartsWith("_b1_") && s2.EndsWith("_b0_"))
        //                    {
        //                        s2 = s2.Substring(4, s2.Length - 8);
        //                    }
        //                    if (s2.EndsWith("_t_"))
        //                    {
        //                        s2 = s2.Substring(0, s2.Length - 3);
        //                    }
        //                    if (firstLine && s2.StartsWith("_image"))
        //                    {
        //                        chapter.Paragraphs.Add(":Image");
        //                        chapter.Paragraphs.Add("");
        //                        firstLine = false;
        //                        secondLine = false;
        //                        s2 = "_p_" + s2;
        //                    }
        //                    if (firstLine && s2.Trim() == "")
        //                    {
        //                        s2 = ":Chapter";
        //                    }
        //                    chapter.Paragraphs.Add(s2);
        //                    if (firstLine)
        //                    {
        //                        chapter.Paragraphs.Add("");
        //                        firstLine = false;
        //                        secondLine = true;
        //                    }
        //                }
        //            }
        //        }
        //        // remove trailing blank and _p_ lines
        //        while (chapter.Paragraphs.Count > 0 &&
        //                (chapter.Paragraphs[chapter.Paragraphs.Count - 1].Length == 0 ||
        //                chapter.Paragraphs[chapter.Paragraphs.Count - 1] == "_p_")
        //              )
        //        {
        //            chapter.Paragraphs.RemoveAt(chapter.Paragraphs.Count - 1);
        //        }
        //        // merge any lines which were split in source
        //        for (int i2 = chapter.Paragraphs.Count - 1; i2 > 2; i2--)
        //        {
        //            if (!chapter.Paragraphs[i2].StartsWith("_p_"))
        //            {
        //                chapter.Paragraphs[i2 - 1] += " " + chapter.Paragraphs[i2];
        //                chapter.Paragraphs[i2] = "";
        //            }
        //        }
        //        // add to chapter list
        //        if (chapter.Paragraphs.Count > 0)
        //        {
        //            if (chapter.Paragraphs[0].ToLower() != "table of contents" &&
        //                chapter.Paragraphs[0].ToLower() != "contents")
        //            {
        //                Chapters.Add(chapter);
        //            }
        //        }
        //    }
        //}

        #endregion
    }

    public class Chapter
    {
        public List<string> Paragraphs = new List<string>();
    }

}
