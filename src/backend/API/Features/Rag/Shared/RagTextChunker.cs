using System.Text;

namespace API.Features.Rag.Shared;

public static class RagTextChunker
{
    public static IEnumerable<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var normalized = NormalizeWhitespace(text);

        if (normalized.Length <= chunkSize)
        {
            yield return normalized;
            yield break;
        }

        var start = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            var chunk = normalized.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
                yield return chunk;

            if (start + length >= normalized.Length)
                break;

            start += Math.Max(length - chunkOverlap, 1);
        }
    }

    public static string NormalizeWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        var previousWasWhitespace = false;

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(ch);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }
}