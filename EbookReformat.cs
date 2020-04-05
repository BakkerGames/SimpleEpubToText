using System.Text;

namespace SimpleEpubToText
{
    public class EbookReformat
    {
        public static string ReformatEbook(string value)
        {
            StringBuilder result = new StringBuilder();
            string[] lines = value.Replace("\r", "").Split('\n');
            bool blankLines = false;
            foreach (string currline in lines)
            {
                string s = ConvertLine(currline);
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
                if (s.Replace ("_p_", "").Replace("_t_", "").Replace("[image]","") == "")
                {
                    return "";
                }
            }
            if (s.StartsWith("###") && s.Contains("[image]"))
            {
                s = s.Replace("[image]", "").TrimEnd();
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
