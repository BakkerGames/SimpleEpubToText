﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                bool inTable = false;
                bool inComment = false;
                string imageFile;
                StringBuilder currline = new StringBuilder();
                Stack<string> spanStack = new Stack<string>();

                // split all the items into html tags and raw text
                string[] contentText = contentFiles[i].Content
                                                      .Replace("\r", "")
                                                      .Replace("</span>\n<span", "</span> <span")
                                                      .Replace("\n", "")
                                                      .Replace(">", ">\n")
                                                      .Replace("<", "\n<")
                                                      .Split('\n');
                foreach (string s in contentText)
                {
                    string s2;
                    if (s.Trim().StartsWith("<"))
                    {
                        s2 = s.Trim();
                    }
                    else
                    {
                        s2 = s;
                    }
                    if (s2.Length == 0) continue;
                    if (inComment)
                    {
                        if (s2.Contains("-->"))
                        {
                            inComment = false;
                            int pos2 = s2.IndexOf("-->");
                            if (pos2 + 3 >= s2.Length)
                            {
                                continue;
                            }
                            s2 = s2.Substring(pos2 + 3);
                            if (s2.Length == 0) continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (s2.Contains("<!--"))
                    {
                        inComment = true;
                        int pos = s2.IndexOf("<!--");
                        int pos2 = s2.IndexOf("-->", pos);
                        if (pos2 > pos)
                        {
                            s2 = s2.Remove(pos, pos2 - pos + 3);
                            inComment = false;
                        }
                        else
                        {
                            s2 = s2.Substring(0, pos);
                        }
                        if (s2.Length == 0) continue;
                    }
                    if (!s2.StartsWith("<"))
                    {
                        if (foundBody)
                        {
                            string s3 = FixText(s2);
                            if (currline.ToString().EndsWith("_t_") && s3.StartsWith(" "))
                            {
                                // change spaces to another _t_
                                currline.Append("_t_");
                                currline.Append(s2.TrimStart());
                            }
                            else
                            {
                                currline.Append(s3);
                            }
                        }
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
                        case "/p":
                        case "/div":
                        case "br":
                        case "/li":
                        case "hr":
                        case "/ul":
                        case "/ol":
                        case "/blockquote":
                        case "/h1":
                        case "/h2":
                        case "/h3":
                        case "/h4":
                            if (inTable)
                            {
                                continue;
                            }
                            if (firstLine)
                            {
                                string s4 = currline.ToString();
                                s4 = s4.Replace("_b1_", "");
                                s4 = s4.Replace("_b0_", "");
                                s4 = s4.Replace("_i1_", "");
                                s4 = s4.Replace("_i0_", "");
                                s4 = s4.Replace("_u1_", "");
                                s4 = s4.Replace("_u0_", "");
                                s4 = s4.Replace("_t_", "");
                                s4 = s4.Replace("_p_", "");
                                s4 = s4.Trim();
                                if (s4.Length == 0)
                                {
                                    continue;
                                }
                                if (s4.StartsWith("_"))
                                {
                                    chapter.Paragraphs.Add("###");
                                    chapter.Paragraphs.Add("");
                                    chapter.Paragraphs.Add("_p_" + s4);
                                    secondLine = false;
                                }
                                else
                                {
                                    chapter.Paragraphs.Add("###" + s4);
                                    chapter.Paragraphs.Add("");
                                    secondLine = true;
                                }
                                firstLine = false;
                            }
                            else if (tag == "hr")
                            {
                                if (currline.Length > 0)
                                {
                                    chapter.Paragraphs.Add(currline.ToString().TrimEnd());
                                }
                                chapter.Paragraphs.Add("_p_---");
                                secondLine = false;
                            }
                            else if (tag == "/li" && currline.Length > 0)
                            {
                                chapter.Paragraphs.Add("_p__t_" + currline.ToString().Trim());
                                secondLine = false;
                            }
                            else if (!secondLine || currline.Length > 0)
                            {
                                chapter.Paragraphs.Add("_p_" + currline.ToString().Trim());
                                secondLine = false;
                            }
                            currline.Clear();
                            break;
                        case "image":
                            imageFile = s2.Substring(s2.IndexOf("href=\"") + 6);
                            imageFile = imageFile.Substring(0, imageFile.IndexOf("\""));
                            if (imageFile != "cover.jpeg")
                            {
                                currline.Append("_image:");
                                currline.Append(imageFile);
                                currline.Append("_");
                            }
                            break;
                        case "img":
                            imageFile = s2.Substring(s2.IndexOf("src=\"") + 5);
                            imageFile = imageFile.Substring(0, imageFile.IndexOf("\""));
                            if (imageFile != "cover.jpeg")
                            {
                                currline.Append("_image:");
                                currline.Append(imageFile);
                                currline.Append("_");
                            }
                            break;
                        case "blockquote":
                            if (inTable)
                            {
                                continue;
                            }
                            if (!currline.ToString().EndsWith("_t_"))
                            {
                                currline.Append("_t_");
                            }
                            break;
                        case "span":
                            if (s2.Contains("\"bold\""))
                            {
                                spanStack.Push("b");
                                currline.Append("_b1_");
                            }
                            else if (s2.Contains("\"italic\""))
                            {
                                spanStack.Push("i");
                                currline.Append("_i1_");
                            }
                            else if (s2.Contains("\"underline\""))
                            {
                                spanStack.Push("u");
                                currline.Append("_u1_");
                            }
                            else
                            {
                                spanStack.Push("");
                            }
                            break;
                        case "/span":
                            string spanPop = spanStack.Pop();
                            if (spanPop == "b")
                            {
                                currline.Append("_b0_");
                            }
                            else if (spanPop == "i")
                            {
                                currline.Append("_i0_");
                            }
                            else if (spanPop == "u")
                            {
                                currline.Append("_u0_");
                            }
                            break;
                        case "li":
                            int pos = s2.IndexOf("value=\"");
                            if (pos >= 0)
                            {
                                pos += 7;
                                int pos2 = s2.IndexOf("\"", pos);
                                currline.Append(s2.Substring(pos, pos2 - pos));
                                currline.Append(": ");
                            }
                            else
                            {
                                currline.Append("* "); // no list value found
                            }
                            break;
                        case "table":
                            inTable = true;
                            if (currline.Length > 0)
                            {
                                chapter.Paragraphs.Add(currline.ToString());
                                currline.Clear();
                            }
                            chapter.Paragraphs.Add("_p__table1_");
                            break;
                        case "/table":
                            inTable = false;
                            if (currline.Length > 0)
                            {
                                chapter.Paragraphs.Add(currline.ToString());
                                currline.Clear();
                            }
                            chapter.Paragraphs.Add("_p__table0_");
                            break;
                        case "tr":
                            currline.Append("_p__tr1_");
                            break;
                        case "/tr":
                            currline.Append("_tr0_");
                            chapter.Paragraphs.Add(currline.ToString());
                            currline.Clear();
                            break;
                        case "caption":
                            currline.Append("_p__caption1_");
                            break;
                        case "/caption":
                            currline.Append("_caption0_");
                            chapter.Paragraphs.Add(currline.ToString());
                            currline.Clear();
                            break;
                        case "tt":
                            currline.Append("_code1_");
                            break;
                        case "/tt":
                            currline.Append("_code0_");
                            break;
                        case "em":
                            currline.Append("_i1_");
                            break;
                        case "/em":
                            currline.Append("_i0_");
                            break;
                        case "strong":
                            currline.Append("_b1_");
                            break;
                        case "/strong":
                            currline.Append("_b0_");
                            break;
                        case "i":
                        case "/i":
                        case "b":
                        case "/b":
                        case "s":
                        case "/s":
                        case "u":
                        case "/u":
                        case "sup":
                        case "/sup":
                        case "sub":
                        case "/sub":
                        case "small":
                        case "/small":
                        case "th":
                        case "/th":
                        case "td":
                        case "/td":
                            // on-off tag pairs
                            currline.Append("_");
                            if (tag.StartsWith("/"))
                            {
                                currline.Append(tag.Substring(1));
                                currline.Append("0"); // off
                            }
                            else
                            {
                                currline.Append(tag);
                                currline.Append("1"); // on
                            }
                            currline.Append("_");
                            break;
                        case "p":
                        case "div":
                        case "a":
                        case "/a":
                        case "ul":
                        case "ol":
                        case "svg":
                        case "/svg":
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "col":
                        case "colgroup":
                        case "/colgroup":
                        case "section":
                        case "/section":
                            // ignore all these
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
                if (currline.ToString().Trim().Length > 0)
                {
                    if (firstLine)
                    {
                        string s4 = currline.ToString().Trim();
                        s4 = s4.Replace("_b1_", "");
                        s4 = s4.Replace("_b0_", "");
                        s4 = s4.Replace("_i1_", "");
                        s4 = s4.Replace("_i0_", "");
                        s4 = s4.Replace("_u1_", "");
                        s4 = s4.Replace("_u0_", "");
                        s4 = s4.Replace("_t_", "");
                        s4 = s4.Replace("_p_", "");
                        s4 = s4.Trim();
                        if (s4.StartsWith("_"))
                        {
                            chapter.Paragraphs.Add("###");
                            chapter.Paragraphs.Add("");
                            chapter.Paragraphs.Add("_p_" + s4);
                        }
                        else if (s4.Contains("_"))
                        {
                            chapter.Paragraphs.Add("###" + s4.Substring(0, s4.IndexOf("_")).Trim());
                            chapter.Paragraphs.Add("");
                            chapter.Paragraphs.Add("_p_" + s4.Substring(s4.IndexOf("_")).Trim());
                        }
                        else
                        {
                            chapter.Paragraphs.Add("###" + s4);
                            chapter.Paragraphs.Add("");
                        }
                    }
                    else
                    {
                        chapter.Paragraphs.Add(currline.ToString().Trim());
                    }
                }
                if (chapter.Paragraphs.Count > 0 &&
                    !chapter.Paragraphs[0].ToLower().Contains("contents"))
                {
                    while (chapter.Paragraphs.Count >= 3 &&
                           chapter.Paragraphs[2] == "_p_")
                    {
                        chapter.Paragraphs.RemoveAt(2);
                    }
                    while (chapter.Paragraphs.Count > 0 &&
                             (chapter.Paragraphs[chapter.Paragraphs.Count - 1].Length == 0 ||
                              chapter.Paragraphs[chapter.Paragraphs.Count - 1] == "_p_")
                          )
                    {
                        chapter.Paragraphs.RemoveAt(chapter.Paragraphs.Count - 1);
                    }
                    if (chapter.Paragraphs.Count > 0)
                    {
                        if (chapter.Paragraphs[0].Contains("_b"))
                        {
                            chapter.Paragraphs[0] = chapter.Paragraphs[0].Replace("_b1_", "").Replace("_b0_", "");
                        }
                        if (chapter.Paragraphs[0].Contains("_i"))
                        {
                            // could be _image_, but will be ignored
                            chapter.Paragraphs[0] = chapter.Paragraphs[0].Replace("_i1_", "").Replace("_i0_", "");
                        }
                        if (chapter.Paragraphs[0].Contains("_u"))
                        {
                            chapter.Paragraphs[0] = chapter.Paragraphs[0].Replace("_u1_", "").Replace("_u0_", "");
                        }
                        for (int para = chapter.Paragraphs.Count - 1; para > 1; para--)
                        {
                            if (chapter.Paragraphs[para] == "_p_" && chapter.Paragraphs[para-1] == "_p_")
                            {
                                chapter.Paragraphs.RemoveAt(para);
                            }
                        }
                        Chapters.Add(chapter);
                    }
                }
            }
        }

        private string FixText(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '_':
                        result.Append("&#95;"); // underline
                        break;
                    case '<':
                        result.Append("&lt;");
                        break;
                    case '>':
                        result.Append("&gt;");
                        break;
                    case '“':
                    case '”':
                        result.Append("\"");
                        break;
                    case '‘':
                    case '’':
                    case '`':
                        result.Append("'");
                        break;
                    case '\t':
                    case (char)160: // non-breaking space
                    case (char)8201: // thin space
                    case (char)8202: // hair space
                        result.Append(' ');
                        break;
                    case (char)8203: // zero width space
                    case (char)8204: // zero width non-joiner
                    case (char)8205: // zero width joiner
                        break;
                    case (char)194: // non-breaking space
                        result.Append('.');
                        break;
                    case (char)8211:
                        result.Append("&ndash;");
                        break;
                    case (char)8212:
                        result.Append("&mdash;");
                        break;
                    case (char)8230:
                        result.Append("...");
                        break;
                    case (char)8224: // dagger
                    case (char)8225: // double dagger
                    case (char)8226: // bullet
                        result.Append("*");
                        break;
                    default:
                        if (c > 32767)
                        {
                            break;
                        }
                        if (c < 32 || c > 255 ||
                            c == 127 || c == 129 || c == 141 || c == 143 || c == 144 || c == 157
                            )
                        {
                            result.Append("&#x");
                            result.Append(((int)c).ToString("x4"));
                            result.Append(";");
                        }
                        else
                        {
                            result.Append(c);
                        }
                        break;
                }
            }
            return result.ToString();
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
