using System.Text;

namespace SimpleEpubToText
{
    public class EbookReformat
    {
        public static string ReformatEbook(string value, bool bareFormat)
        {
            StringBuilder result = new StringBuilder();
            string[] lines = value.Replace("\r", "").Replace("<br/>", "\n\t").Split('\n');
            bool blankLines = false;
            foreach (string currline in lines)
            {
                string s = ConvertLine(ExpandHexChars(currline));
                // check for blanklines
                if (s == "")
                {
                    blankLines = true;
                    continue;
                }
                if (blankLines)
                {
                    result.AppendLine();
                    if (!s.StartsWith("\t"))
                    {
                        result.AppendLine();
                    }
                }
                if (bareFormat)
                {
                    s = ConvertToBareFormat(s);
                }
                result.AppendLine(s);
                blankLines = false;
            }
            return result.ToString();
        }

        private static string ConvertLine(string s)
        {
            while (s.Contains("_image:"))
            {
                int pos0 = s.IndexOf("_image:");
                int pos1 = s.IndexOf("_", pos0 + 1);
                s = s.Substring(0, pos0) + "[image]" + s.Substring(pos1 + 1);
                if (s.Replace("_p_", "").Replace("_t_", "").Replace("[image]", "").Replace(" ", "") == "")
                {
                    //return "";
                }
            }
            while (s.Contains("_imagealt:"))
            {
                int pos0 = s.IndexOf("_imagealt:");
                int pos1 = s.IndexOf("_", pos0 + 1);
                s = s.Substring(0, pos0) + "[" + s.Substring(pos0 + 10, pos1 - pos0 - 10) + "]" + s.Substring(pos1 + 1);
                s = s.Replace("[image]", ""); // don't need [image] now
            }
            if (s.StartsWith("###") && s.Contains("[image]"))
            {
                //s = s.Replace("[image]", "").TrimEnd();
            }
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
                return "";
                // newResult = ":Chapter"; // non-display chapter titles
            }
            else if (newResult.StartsWith("###"))
            {
                newResult = newResult.Substring(3);
            }
            if (newResult.Contains("<code> </code>"))
            {
                newResult = newResult.Replace("<code> </code>", " ");
            }
            if (newResult.Contains("</i> <i>"))
            {
                newResult = newResult.Replace("</i> <i>", " ");
            }
            if (newResult.Contains("</i><i>"))
            {
                newResult = newResult.Replace("</i><i>", " ");
            }
            if (newResult.Contains("<i> </i>"))
            {
                newResult = newResult.Replace("<i> </i>", " ");
            }
            if (newResult.Contains("<i></i>"))
            {
                newResult = newResult.Replace("<i></i>", " ");
            }
            if (newResult.Contains(". ..."))
            {
                newResult = newResult.Replace(". ...", "....");
            }
            if (newResult.Contains("... ."))
            {
                newResult = newResult.Replace("... .", "....");
            }
            if (newResult.Contains("&mdash;"))
            {
                newResult = newResult.Replace("&mdash;", "—");
            }
            if (newResult.Contains("&ndash;"))
            {
                newResult = newResult.Replace("&ndash;", "-");
            }
            while (newResult.Contains("—-"))
            {
                newResult = newResult.Replace("—-", "—");
            }
            while (newResult.Contains("-—"))
            {
                newResult = newResult.Replace("-—", "—");
            }
            if (newResult.Contains("—"))
            {
                // don't add spaces anymore
                //newResult = newResult.Replace("—", " — ");
            }
            while (newResult.EndsWith(" ") || newResult.EndsWith("\t"))
            {
                newResult = newResult.Substring(0, newResult.Length - 1);
            }
            if (newResult.EndsWith(" </i>"))
            {
                newResult = newResult.Substring(0, newResult.Length - 4).TrimEnd() + "</i>";
            }
            if (newResult.Contains("<i> "))
            {
                newResult = newResult.Replace("<i> ", " <i>");
            }
            if (newResult.Contains(" </i>"))
            {
                newResult = newResult.Replace(" </i>", "</i> ");
            }
            while (newResult.Contains("\t "))
            {
                newResult = newResult.Replace("\t ", "\t");
            }
            while (newResult.Contains("  ") && !newResult.Contains("<code>"))
            {
                newResult = newResult.Replace("  ", " ");
            }
            if (newResult.Contains(" <i> "))
            {
                newResult = newResult.Replace(" <i> ", " <i>");
            }
            if (newResult.Contains(" </i> "))
            {
                newResult = newResult.Replace(" </i> ", "</i> ");
            }
            if (newResult == "\t")
            {
                newResult = "";
            }
            // these two must be the very last checks
            if (newResult.Contains("&#95;"))
            {
                newResult = newResult.Replace("&#95;", "_");
            }
            if (newResult.Contains("&amp;"))
            {
                newResult = newResult.Replace("&amp;", "&");
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

        private static string ExpandHexChars(string s)
        {
            if (!s.Contains("&#")) return s;
            StringBuilder result = new StringBuilder();
            int posStart = -1;
            int posEnd = -1;
            int baseValue = 10;
            while (s.IndexOf("&#", posEnd + 1) > 0)
            {
                posStart = s.IndexOf("&#", posEnd + 1);
                result.Append(s.Substring(posEnd + 1, posStart - posEnd - 1));
                posEnd = s.IndexOf(";", posStart);
                if (s[posStart + 2] == 'x')
                {
                    posStart++;
                    baseValue = 16;
                }
                else
                {
                    baseValue = 10;
                }
                int charValue = 0;
                for (int i = posStart + 2; i < posEnd; i++)
                {
                    int value = s.Substring(i, 1).ToUpper()[0];
                    if (value > '9')
                    {
                        if (baseValue == 10)
                        {
                            throw new System.Exception($"Invalid hex format: {s}");
                        }
                        value = value - 'A' + 10;
                    }
                    else
                    {
                        value -= '0';
                    }
                    charValue = (charValue * baseValue) + value;
                }
                // adjust some chars to simpler ones
                switch (charValue)
                {
                    case 133: // ellipsis
                    case 8230:
                        result.Append("...");
                        charValue = 0;
                        break;
                    case 160: // non-breaking space
                    case 8201: // thin space
                    case 8202: // hair space
                        charValue = 32; // space
                        break;
                    case 173: // soft hyphen
                        charValue = 0;
                        break;
                    case 8203: // zero width space
                    case 8204: // zero width non-joiner
                    case 8205: // zero width joiner
                        charValue = 0;
                        break;
                    case 132:
                    case 147:
                    case 148:
                    case 171:
                    case 187:
                    case 8220:
                    case 8221:
                        charValue = 34;
                        break;
                    case 96:
                    case 130:
                    case 139:
                    case 145:
                    case 146:
                    case 155:
                    case 8216:
                    case 8217:
                    case 8218:
                    case 8219:
                    case 8249:
                    case 8250:
                        charValue = 39;
                        break;
                }
                // add the character
                if (charValue != 0)
                {
                    if (charValue == 95)
                        result.Append("&#95;");
                    else
                        result.Append((char)charValue);
                }
            }
            result.Append(s.Substring(posEnd + 1));
            return result.ToString();
        }

        private static string ConvertToBareFormat(string s)
        {
            if (s.Contains("<i>"))
            {
                s = s.Replace("<i>", "_");
            }
            if (s.Contains("</i>"))
            {
                s = s.Replace("</i>", "_");
            }
            if (s.Contains("<b>"))
            {
                s = s.Replace("<b>", "*");
            }
            if (s.Contains("</b>"))
            {
                s = s.Replace("</b>", "*");
            }
            if (s.Contains("<code>"))
            {
                s = s.Replace("<code>", "```");
            }
            if (s.Contains("</code>"))
            {
                s = s.Replace("</code>", "```");
            }
            if (s.Contains("<sup>"))
            {
                s = s.Replace("<sup>", "[");
            }
            if (s.Contains("</sup>"))
            {
                s = s.Replace("</sup>", "]");
            }
            if (s.Contains("—"))
            {
                s = s.Replace("—", "---");
            }
            int pos1 = s.IndexOf("<");
            int pos2 = s.IndexOf(">");
            while (pos1 >= 0 && pos2 > pos1)
            {
                s = s.Substring(0, pos1) + s.Substring(pos2 + 1);
                pos1 = s.IndexOf("<");
                pos2 = s.IndexOf(">");
            }
            if (s.StartsWith("\t```") && s.EndsWith("```") && s.Length > 7 && s.IndexOf("```", 4) == s.Length - 3)
            {
                s = s.Substring(0, s.Length - 3).TrimEnd();
            }
            return s;
        }
    }
}
