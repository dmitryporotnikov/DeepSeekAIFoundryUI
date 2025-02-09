using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DeepSeekAIFoundryUI.Utilities
{
    /// <summary>
    /// Provides formatting logic for AI outputs, including handling of hidden or 'think' text and basic markdown.
    /// </summary>
    public class ChatOutputFormatter
    {
        private bool _inThinkMode;
        private string _bufferedChunk = string.Empty;

        /// <summary>
        /// Appends a chunk of text to the RichTextBox while handling any special 'think' tags.
        /// </summary>
        public void AppendChunkWithThink(string chunk, RichTextBox rtb)
        {
            chunk = _bufferedChunk + chunk;
            _bufferedChunk = string.Empty;
            int index = 0;

            while (index < chunk.Length)
            {
                if (!_inThinkMode)
                {
                    int thinkStart = chunk.IndexOf("<think>", index, System.StringComparison.OrdinalIgnoreCase);
                    if (thinkStart == -1)
                    {
                        int partialIndex = GetPartialTagIndex(chunk, index, "<think>");
                        if (partialIndex >= 0)
                        {
                            AppendMarkdownAsText(chunk.Substring(index, partialIndex - index), rtb);
                            _bufferedChunk = chunk.Substring(partialIndex);
                        }
                        else
                        {
                            AppendMarkdownAsText(chunk.Substring(index), rtb);
                        }
                        break;
                    }
                    else
                    {
                        AppendMarkdownAsText(chunk.Substring(index, thinkStart - index), rtb);
                        _inThinkMode = true;
                        index = thinkStart + "<think>".Length;
                    }
                }
                else
                {
                    int thinkEnd = chunk.IndexOf("</think>", index, System.StringComparison.OrdinalIgnoreCase);
                    if (thinkEnd == -1)
                    {
                        int partialIndex = GetPartialTagIndex(chunk, index, "</think>");
                        if (partialIndex >= 0)
                        {
                            AppendFormattedText(chunk.Substring(index, partialIndex - index), Brushes.Gray, rtb);
                            _bufferedChunk = chunk.Substring(partialIndex);
                        }
                        else
                        {
                            AppendFormattedText(chunk.Substring(index), Brushes.Gray, rtb);
                        }
                        break;
                    }
                    else
                    {
                        AppendFormattedText(chunk.Substring(index, thinkEnd - index), Brushes.Gray, rtb);
                        _inThinkMode = false;
                        index = thinkEnd + "</think>".Length;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a special tag is only partially present at the end of the current chunk.
        /// </summary>
        private int GetPartialTagIndex(string text, int startIndex, string tag)
        {
            for (int i = 1; i < tag.Length; i++)
            {
                if (text.Length - i >= startIndex
                    && tag.StartsWith(text.Substring(text.Length - i),
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return text.Length - i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses and appends Markdown-style text, including custom placeholders for math blocks.
        /// </summary>
        public void AppendMarkdownAsText(string text, RichTextBox rtb)
        {
            var mathRegex = new Regex(@"\\\[(.*?)\\\]", RegexOptions.Singleline);
            int lastIndex = 0;
            foreach (Match mathMatch in mathRegex.Matches(text))
            {
                if (mathMatch.Index > lastIndex)
                {
                    ProcessBoldText(text.Substring(lastIndex, mathMatch.Index - lastIndex), rtb);
                }

                var mathContent = mathMatch.Groups[1].Value.Trim();
                var paragraph = new Paragraph(new Run(mathContent))
                {
                    TextAlignment = System.Windows.TextAlignment.Center,
                    FontStyle = System.Windows.FontStyles.Italic,
                    Foreground = Brushes.DarkCyan
                };
                rtb.Document.Blocks.Add(paragraph);
                lastIndex = mathMatch.Index + mathMatch.Length;
            }
            if (lastIndex < text.Length)
            {
                ProcessBoldText(text.Substring(lastIndex), rtb);
            }
        }

        /// <summary>
        /// Detects bold syntax and transforms it to bold Run elements.
        /// </summary>
        private void ProcessBoldText(string text, RichTextBox rtb)
        {
            var boldRegex = new Regex(@"\*\*(.+?)\*\*");
            int lastIndex = 0;
            foreach (Match boldMatch in boldRegex.Matches(text))
            {
                if (boldMatch.Index > lastIndex)
                {
                    AppendText(text.Substring(lastIndex, boldMatch.Index - lastIndex), rtb);
                }

                var boldRun = new Run(boldMatch.Groups[1].Value)
                {
                    FontWeight = System.Windows.FontWeights.Bold
                };
                AppendRun(boldRun, rtb);
                lastIndex = boldMatch.Index + boldMatch.Length;
            }
            if (lastIndex < text.Length)
            {
                AppendText(text.Substring(lastIndex), rtb);
            }
        }

        /// <summary>
        /// Appends plain text to the RichTextBox.
        /// </summary>
        public void AppendText(string text, RichTextBox rtb)
        {
            var run = new Run(text);
            AppendRun(run, rtb);
        }

        /// <summary>
        /// Appends colored text to the RichTextBox.
        /// </summary>
        public void AppendFormattedText(string text, Brush color, RichTextBox rtb)
        {
            var run = new Run(text) { Foreground = color };
            AppendRun(run, rtb);
        }

        /// <summary>
        /// Appends a Run element to the last paragraph or creates a new paragraph if necessary.
        /// </summary>
        public void AppendRun(Run run, RichTextBox rtb)
        {
            if (rtb.Document.Blocks.LastBlock is Paragraph paragraph)
            {
                paragraph.Inlines.Add(run);
            }
            else
            {
                rtb.Document.Blocks.Add(new Paragraph(run));
            }
            rtb.ScrollToEnd();
        }
    }
}
