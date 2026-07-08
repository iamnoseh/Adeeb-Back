using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Adeeb.Api.Documentation.Rendering;

public sealed class SafeMarkdownRenderer
{
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

    public string Render(string markdown)
    {
        var html = new StringBuilder();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var inCode = false;
        var inList = false;
        var code = new StringBuilder();
        var codeLanguage = "";

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inList)
                {
                    html.AppendLine("</ul>");
                    inList = false;
                }

                if (!inCode)
                {
                    inCode = true;
                    codeLanguage = WebUtility.HtmlEncode(line[3..].Trim());
                    code.Clear();
                }
                else
                {
                    html.Append("<div class=\"code-block\"><button class=\"copy-code\" type=\"button\">Copy</button><pre><code");
                    if (!string.IsNullOrWhiteSpace(codeLanguage))
                    {
                        html.Append($" class=\"language-{codeLanguage}\"");
                    }
                    html.Append('>');
                    html.Append(WebUtility.HtmlEncode(code.ToString().TrimEnd()));
                    html.AppendLine("</code></pre></div>");
                    inCode = false;
                }

                continue;
            }

            if (inCode)
            {
                code.AppendLine(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (inList)
                {
                    html.AppendLine("</ul>");
                    inList = false;
                }
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (!inList)
                {
                    html.AppendLine("<ul>");
                    inList = true;
                }
                html.Append("<li>");
                html.Append(RenderInline(line[2..]));
                html.AppendLine("</li>");
                continue;
            }

            if (inList)
            {
                html.AppendLine("</ul>");
                inList = false;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                AppendHeading(html, 3, line[4..]);
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AppendHeading(html, 2, line[3..]);
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                AppendHeading(html, 1, line[2..]);
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                html.Append("<blockquote>");
                html.Append(RenderInline(line[2..]));
                html.AppendLine("</blockquote>");
            }
            else
            {
                html.Append("<p>");
                html.Append(RenderInline(line));
                html.AppendLine("</p>");
            }
        }

        if (inList)
        {
            html.AppendLine("</ul>");
        }

        return html.ToString();
    }

    private static void AppendHeading(StringBuilder html, int level, string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        var anchor = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        html.Append($"<h{level} id=\"{anchor}\">");
        html.Append(encoded);
        html.Append($"<a class=\"anchor\" href=\"#{anchor}\">#</a></h{level}>");
    }

    private static string RenderInline(string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        encoded = Regex.Replace(encoded, "`([^`]+)`", "<code>$1</code>");
        return LinkRegex.Replace(encoded, match =>
        {
            var label = match.Groups[1].Value;
            var href = match.Groups[2].Value;
            if (!href.StartsWith("/", StringComparison.Ordinal) && !href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                href = "#";
            }
            return $"<a href=\"{WebUtility.HtmlEncode(href)}\">{label}</a>";
        });
    }
}
