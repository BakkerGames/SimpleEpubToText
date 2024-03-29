﻿using System.Text;
using VersOne.Epub;

namespace SimpleEpubToText;

public class EbookLoader
{
    private readonly string? _ebookPath;
    private EpubBook? book;
    private List<EpubTextContentFile>? contentFiles;
    private readonly Dictionary<string, string> cssStyles = new();

    public List<Chapter>? Chapters;

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
        ExtractCSS(book);
        contentFiles = book.ReadingOrder.ToList();
        Chapters = new List<Chapter>();
        for (int i = 0; i < contentFiles.Count; i++)
        {
            Chapter chapter = new();
            bool foundBody = false;
            bool firstLine = true;
            bool secondLine = false;
            bool inTable = false;
            bool hasTableRows = false;
            int tableDepth = 0;
            bool inComment = false;
            bool inBlockquote = false;
            string imageFile;
            StringBuilder currline = new();
            Stack<string> spanStack = new();

            // split all the items into html tags and raw text
            string[] contentText = contentFiles[i].Content
                                                  .Replace("\r", "")
                                                  .Replace("</span>\n<span", "</span> <span")
                                                  .Replace("\n", " ") // fixes paragraphs across multiple lines
                                                  .Replace(">", ">\n")
                                                  .Replace("<", "\n<")
                                                  .Split('\n');
            foreach (string s in contentText)
            {
                if (s.Length == 0) continue;
                string s2;
                if (s.Trim().StartsWith("<"))
                {
                    s2 = s.Trim();
                }
                else
                {
                    s2 = s;
                }
                if (s2.Contains("&nbsp;"))
                {
                    s2 = s2.Replace("&nbsp;", " ");
                }
                if (s2.Contains("&ensp;"))
                {
                    s2 = s2.Replace("&ensp;", " ");
                }
                if (s2.Contains("&emsp;"))
                {
                    s2 = s2.Replace("&emsp;", " ");
                }
                if (s2.Contains("&#160;")) // no-break space
                {
                    s2 = s2.Replace("&#160;", " ");
                }
                if (s2.Contains($"{(char)160}")) // no-break space
                {
                    s2 = s2.Replace((char)160, ' ');
                }
                if (s2.Contains($"{(char)8194}")) // en-space
                {
                    s2 = s2.Replace((char)8194, ' ');
                }
                if (s2.Contains($"{(char)8195}")) // em-space
                {
                    s2 = s2.Replace((char)8195, ' ');
                }
                if (s2.Length == 0) continue;
                if (s2.Contains('_'))
                {
                    s2 = s2.Replace("_", "&#95;");
                }
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
                        s2 = s2[(pos2 + 3)..];
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
                        s2 = s2[..pos];
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
                    case "blockquote":
                        if (inBlockquote)
                        {
                            // avoid nested blockquotes
                            continue;
                        }
                        if (inTable)
                        {
                            continue;
                        }
                        inBlockquote = true;
                        if (!currline.ToString().EndsWith("_t_"))
                        {
                            currline.Append("_t_");
                        }
                        break;
                    case "/blockquote":
                        if (!inBlockquote)
                        {
                            // avoid nested blockquotes
                            continue;
                        }
                        if (inTable)
                        {
                            continue;
                        }
                        inBlockquote = false;
                        if (!secondLine || currline.Length > 0)
                        {
                            chapter.Paragraphs.Add("_p_" + currline.ToString().Trim());
                            secondLine = false;
                        }
                        currline.Clear();
                        break;
                    case "/p":
                    case "/div":
                    case "br":
                    case "/li":
                    case "hr":
                    case "/ul":
                    case "/ol":
                    case "/h1":
                    case "/h2":
                    case "/h3":
                    case "/h4":
                    case "/h5":
                    case "/h6":
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
                            else if (char.IsDigit(s4[0]) || s4.ToUpper().StartsWith("CHAPTER"))
                            {
                                chapter.Paragraphs.Add("###" + s4);
                                chapter.Paragraphs.Add("");
                                secondLine = true;
                            }
                            else if (s4.Length > 100) // too long for chapter title
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
                            if (!secondLine && (tag == "/h1" || tag == "/h2") && currline.ToString().IndexOf("image") < 0)
                            {
                                Chapters.Add(chapter);
                                chapter = new();
                                chapter.Paragraphs.Add("###" + currline.ToString().Trim());
                                chapter.Paragraphs.Add("");
                                secondLine = true;
                            }
                            else
                            {
                                chapter.Paragraphs.Add("_p_" + currline.ToString().Trim());
                                secondLine = false;
                            }
                        }
                        currline.Clear();
                        break;
                    case "image":
                        imageFile = s2[(s2.IndexOf("href=\"") + 6)..];
                        imageFile = imageFile[..imageFile.IndexOf("\"")];
                        if (imageFile != "cover.jpeg")
                        {
                            currline.Append("_image:");
                            currline.Append(imageFile);
                            currline.Append('_');
                        }
                        break;
                    case "img":
                        imageFile = s2[(s2.IndexOf("src=\"") + 5)..];
                        imageFile = imageFile[..imageFile.IndexOf("\"")];
                        if (imageFile != "cover.jpeg")
                        {
                            currline.Append("_image:");
                            currline.Append(imageFile);
                            currline.Append('_');
                            if (s2.Contains("alt=\"", StringComparison.CurrentCulture))
                            {
                                string altTag = s2[(s2.IndexOf("alt=\"") + 5)..];
                                altTag = altTag[..altTag.IndexOf("\"")].Trim();
                                if (altTag.Length > 0)
                                {
                                    currline.Append("_imagealt:");
                                    currline.Append(altTag);
                                    currline.Append('_');
                                }
                            }
                        }
                        break;
                    case "span":
                        if (s2.Contains("italic"))
                        {
                            spanStack.Push("i");
                            currline.Append("_i1_");
                            break;
                        }
                        if (s2.Contains("bold"))
                        {
                            spanStack.Push("b");
                            currline.Append("_b1_");
                            break;
                        }
                        if (s2.Contains("underline"))
                        {
                            spanStack.Push("u");
                            currline.Append("_u1_");
                            break;
                        }
                        if (s2.Contains("class="))
                        {
                            string className = s2[(s2.IndexOf("class=") + 7)..];
                            className = className[..className.IndexOf("\"")];
                            if (cssStyles.ContainsKey(className))
                            {
                                string classValue = cssStyles[className];
                                if (classValue.Contains("italic"))
                                {
                                    spanStack.Push("i");
                                    currline.Append("_i1_");
                                    break;
                                }
                                if (classValue.Contains("bold"))
                                {
                                    spanStack.Push("b");
                                    currline.Append("_b1_");
                                    break;
                                }
                                if (classValue.Contains("underline"))
                                {
                                    spanStack.Push("u");
                                    currline.Append("_u1_");
                                    break;
                                }
                            }
                        }
                        spanStack.Push("");
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
                            currline.Append(s2[pos..pos2]);
                            currline.Append(": ");
                        }
                        else
                        {
                            currline.Append("* "); // no list value found
                        }
                        break;
                    case "table":
                        inTable = true;
                        if (tableDepth == 0)
                        {
                            hasTableRows = false;
                        }
                        tableDepth++;
                        if (!hasTableRows)
                        {
                            break;
                        }
                        if (currline.Length > 0)
                        {
                            chapter.Paragraphs.Add(currline.ToString());
                            currline.Clear();
                        }
                        chapter.Paragraphs.Add("_p__table1_");
                        break;
                    case "/table":
                        tableDepth--;
                        if (tableDepth <= 0)
                        {
                            tableDepth = 0;
                            inTable = false;
                        }
                        if (!hasTableRows)
                        {
                            break;
                        }
                        if (currline.Length > 0)
                        {
                            chapter.Paragraphs.Add(currline.ToString());
                            currline.Clear();
                        }
                        chapter.Paragraphs.Add("_p__table0_");
                        break;
                    case "tr":
                        if (!inTable)
                        {
                            Console.WriteLine("<tr> outside table error");
                            break;
                        }
                        if (!hasTableRows)
                        {
                            if (currline.Length > 0)
                            {
                                chapter.Paragraphs.Add(currline.ToString());
                                currline.Clear();
                            }
                            for (int trI = 0; trI < tableDepth; trI++)
                            {
                                chapter.Paragraphs.Add("_p__table1_");
                            }
                            hasTableRows = true;
                        }
                        currline.Append("_p__tr1_");
                        break;
                    case "/tr":
                        if (!inTable)
                        {
                            Console.WriteLine("</tr> outside table error");
                            break;
                        }
                        currline.Append("_tr0_");
                        chapter.Paragraphs.Add(currline.ToString());
                        currline.Clear();
                        break;
                    case "caption":
                        if (!inTable)
                        {
                            Console.WriteLine("<caption> outside table error");
                            break;
                        }
                        currline.Append("_p__caption1_");
                        break;
                    case "/caption":
                        if (!inTable)
                        {
                            Console.WriteLine("</caption> outside table error");
                            break;
                        }
                        currline.Append("_caption0_");
                        chapter.Paragraphs.Add(currline.ToString());
                        currline.Clear();
                        break;
                    case "tt":
                    case "code":
                        currline.Append("_code1_");
                        break;
                    case "/tt":
                    case "/code":
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
                        // on-off tag pairs
                        currline.Append('_');
                        if (tag.StartsWith("/"))
                        {
                            currline.Append(tag[1..]);
                            currline.Append('0'); // off
                        }
                        else
                        {
                            currline.Append(tag);
                            currline.Append('1'); // on
                        }
                        currline.Append('_');
                        break;
                    case "th":
                    case "/th":
                    case "td":
                    case "/td":
                        if (!inTable)
                        {
                            Console.WriteLine($"<{tag}> outside table error");
                            break; // ignore all these if not in table
                        }
                        // on-off tag pairs
                        if (currline.Length == 0)
                        {
                            currline.Append("_p_");
                        }
                        currline.Append('_');
                        if (tag.StartsWith("/"))
                        {
                            currline.Append(tag[1..]);
                            currline.Append('0'); // off
                        }
                        else
                        {
                            currline.Append(tag);
                            currline.Append('1'); // on
                        }
                        currline.Append('_');
                        break;
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
                    case "h5":
                    case "h6":
                    case "col":
                    case "colgroup":
                    case "/colgroup":
                    case "section":
                    case "/section":
                    case "big":
                    case "/big":
                    case "nav":
                    case "/nav":
                    case "wbr":
                    case "wbr/":
                        // ignore all these
                        break;
                    case "p":
                        break;
                    default:
                        if (foundBody)
                        {
                            currline.Append('<');
                            currline.Append(tag);
                            currline.Append('>');
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
                    s4 = s4.Trim();
                    if (s4.StartsWith("_"))
                    {
                        chapter.Paragraphs.Add("###");
                        chapter.Paragraphs.Add("");
                        chapter.Paragraphs.Add("_p_" + s4);
                    }
                    else if (s4.Contains('_'))
                    {
                        chapter.Paragraphs.Add("###" + s4[..s4.IndexOf("_")].Trim());
                        chapter.Paragraphs.Add("");
                        chapter.Paragraphs.Add("_p_" + s4[s4.IndexOf("_")..].Trim());
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
                         (chapter.Paragraphs[^1].Length == 0 ||
                          chapter.Paragraphs[^1] == "_p_")
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
                        if (chapter.Paragraphs[para] == "_p_" && chapter.Paragraphs[para - 1] == "_p_")
                        {
                            chapter.Paragraphs.RemoveAt(para);
                        }
                    }
                    Chapters.Add(chapter);
                }
            }
        }
    }

    private void ExtractCSS(EpubBook book)
    {
        Dictionary<string, EpubTextContentFile> css = book.Content.Css;
        cssStyles.Clear();
        foreach (KeyValuePair<string, EpubTextContentFile> kv in css)
        {
            string fixedContent = kv.Value.Content;
            while (fixedContent.Contains("/*"))
            {
                int pos1 = fixedContent.IndexOf("/*");
                int pos2 = fixedContent.IndexOf("*/", pos1);
                if (pos2 > pos1)
                {
                    fixedContent = fixedContent[0..pos1] + fixedContent[(pos2 + 2)..];
                }
                else
                {
                    throw new SystemException("Invalid css");
                }
            }
            if (fixedContent.StartsWith("@charset") && fixedContent.Contains(';'))
            {
                fixedContent = fixedContent[(fixedContent.IndexOf(';') + 1)..];
            }
            string[] styles = fixedContent.Replace("\r", " ").Replace("\n", " ").Split('}');
            foreach (string s in styles)
            {
                if (!string.IsNullOrEmpty(s?.Trim()))
                {
                    string name = s[..s.IndexOf('{')].Replace(".", "").Trim();
                    if (!cssStyles.ContainsKey(name) && s.Contains('{'))
                    {
                        string value = s[(s.IndexOf('{') + 1)..].Trim();
                        string[] names = name.Split(',');
                        foreach (string n in names)
                        {
                            string nTrim = n.Trim();
                            if (cssStyles.ContainsKey(nTrim))
                            {
                                cssStyles[nTrim] = cssStyles[nTrim] + ';' + value;
                            }
                            else
                            {
                                cssStyles.Add(nTrim, value);
                            }
                        }
                    }
                }
            }
        }
    }

    private static string FixText(string value)
    {
        StringBuilder result = new();
        foreach (char c in value)
        {
            switch (c)
            {
                //case '_':
                //    result.Append("&#95;"); // underline
                //    break;
                case '<':
                    result.Append("&lt;");
                    break;
                case '>':
                    result.Append("&gt;");
                    break;
                case (char)132:
                case (char)147:
                case (char)148:
                // case (char)171:
                // case (char)187:
                case (char)8220:
                case (char)8221:
                    result.Append('"');
                    break;
                case (char)96:
                case (char)130:
                case (char)139:
                case (char)145:
                case (char)146:
                case (char)155:
                case (char)8216:
                case (char)8217:
                case (char)8218:
                case (char)8219:
                case (char)8249:
                case (char)8250:
                    result.Append('\'');
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
                case (char)8211:
                    result.Append("&ndash;");
                    break;
                case (char)8212:
                    result.Append("&mdash;");
                    break;
                case (char)133:
                case (char)8230:
                    result.Append("...");
                    break;
                case (char)8224: // dagger
                case (char)8225: // double dagger
                case (char)8226: // bullet
                    result.Append('*');
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
                        result.Append(';');
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

    private static string GetTag(string s2)
    {
        int pos = s2.IndexOf(">");
        int pos2 = s2.IndexOf(" ");
        if (pos2 >= 0)
        {
            return s2[1..pos2];
        }
        return s2[1..pos];
    }

    #endregion

}

public class Chapter
{
    public List<string> Paragraphs = new();
}
