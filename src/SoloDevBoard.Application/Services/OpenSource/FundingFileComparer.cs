using System.Security.Cryptography;
using System.Text;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Compares repository funding file text with the canonical standard.</summary>
public static class FundingFileComparer
{
    public const int MaxCompareBytes = 64 * 1024;

    public static string ComputeContentHash(string content)
    {
        var normalised = NormaliseForHash(content);
        var bytes = Encoding.UTF8.GetBytes(normalised);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static FundingCompareResult Compare(string? repositoryContent, string standardContent)
    {
        if (repositoryContent is null)
        {
            return new FundingCompareResult(false, FundingCompareOutcome.Missing, null);
        }

        var repositoryBytes = Encoding.UTF8.GetByteCount(repositoryContent);
        if (repositoryBytes > MaxCompareBytes)
        {
            return new FundingCompareResult(false, FundingCompareOutcome.TooLarge, "Funding file exceeds 64 KiB.");
        }

        if (!AreLineEndingsUniform(repositoryContent))
        {
            return new FundingCompareResult(true, FundingCompareOutcome.Differs, "Mixed line endings.");
        }

        if (repositoryContent.Length > 0 && repositoryContent[0] == '\uFEFF')
        {
            return new FundingCompareResult(true, FundingCompareOutcome.Differs, "File has a byte-order mark.");
        }

        var normalisedRepository = NormaliseContent(repositoryContent);
        var normalisedStandard = NormaliseContent(standardContent);
        if (string.Equals(normalisedRepository, normalisedStandard, StringComparison.Ordinal))
        {
            return new FundingCompareResult(true, FundingCompareOutcome.Matches, null);
        }

        return new FundingCompareResult(true, FundingCompareOutcome.Differs, null);
    }

    private static string NormaliseForHash(string content)
    {
        var normalised = NormaliseContent(content);
        return normalised;
    }

    private static string NormaliseContent(string content)
    {
        var withUnixNewlines = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        return TrimAtMostOneTrailingNewline(withUnixNewlines);
    }

    private static string TrimAtMostOneTrailingNewline(string content)
    {
        if (content.EndsWith("\n\n", StringComparison.Ordinal))
        {
            return content;
        }

        if (content.EndsWith('\n'))
        {
            return content[..^1];
        }

        return content;
    }

    private static bool AreLineEndingsUniform(string content)
    {
        var hasCrLf = content.Contains("\r\n", StringComparison.Ordinal);
        var hasLfWithoutCr = content.Contains('\n')
            && content.Replace("\r\n", string.Empty, StringComparison.Ordinal).Contains('\n');
        return !(hasCrLf && hasLfWithoutCr);
    }
}

public enum FundingCompareOutcome
{
    Missing,
    Matches,
    Differs,
    TooLarge,
}

public sealed record FundingCompareResult(bool FilePresent, FundingCompareOutcome Outcome, string? Reason);