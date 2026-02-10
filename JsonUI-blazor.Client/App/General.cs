using System.Data.Common;

public record KvpSelectedArgs(JNode node, bool selected);

public enum JNodeType
{
  Object, Array
}

public enum TokenType
{
  Bearer, BasicUsernamePassword, BasicBase64
}

public class CustomHeader
{
  public string Key { get; set; } = "";
  public string Value { get; set; } = "";
  public bool IsBase64 { get; set; }
}

public static class General
{
  public const string StorageKey = "cookie-consent";
  public const int RowHeightPx = 28;
  public const int ColWidthPx = 160;

  public static Array GetTokenTypes() => Enum.GetValues<TokenType>();

  public static (bool, JNode?) JNodeIsSnakeCaseLower(JNode node)
  {
    if (!node.Name.Contains('_'))
    {
      return (false, node);
    }

    if (!node.Name.All(x => x == '_' || char.IsAsciiLetterLower(x)))
    {
      return (false, node);
    }

    return (true, null);
  }

  public static (bool, JNode?) JNodeIsSnakeCaseUpper(JNode node)
  {
    if (!node.Name.Contains('_'))
    {
      return (false, node);
    }

    if (!node.Name.All(x => x == '_' || char.IsAsciiLetterUpper(x)))
    {
      return (false, node);
    }

    return (true, null);
  }

  public static (bool, JNode?) JNodeIsKebabCaseLower(JNode node)
  {
    if (!node.Name.Contains('-'))
    {
      return (false, node);
    }

    if (!node.Name.All(x => x == '-' || char.IsAsciiLetterLower(x)))
    {
      return (false, node);
    }

    return (true, null);
  }

  public static (bool, JNode?) JNodeIsKebabCaseUpper(JNode node)
  {
    if (!node.Name.Contains('-'))
    {
      return (false, node);
    }

    if (!node.Name.All(x => x == '-' || char.IsAsciiLetterUpper(x)))
    {
      return (false, node);
    }

    return (true, null);
  }

  public static (bool, JNode?) JNodeIsCamelCase(JNode node)
  {
    if (!node.Name.All(x => char.IsAsciiLetter(x)))
    {
      return (false, node);
    }

    return (true, null);
  }
}