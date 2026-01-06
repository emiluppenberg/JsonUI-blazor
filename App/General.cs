using System.Data.Common;

public record KvpSelectedArgs(JNode node, bool selected);

public static class General
{
  public const int RowHeightPx = 22;
  public const int ColWidthPx = 160;

  public struct CSharpOptions()
  {
    public bool UsePascalCase { get; set; }
  }

  public enum NamingConvention
  {
    SnakeCaseLower, SnakeCaseUpper, KebabCaseLower, KebabCaseUpper, PascalCase, CamelCase
  }

  public enum CollectionsCSharp
  {
    List, IEnumerable, ICollection, Array
  }

  public enum JNodeType
  {
    Object, Array
  }

  public static Array GetCollectionOptions() => Enum.GetValues<CollectionsCSharp>();

  public static string ConfigureCollection(string datatype, CollectionsCSharp collectionAs)
  {
    switch (collectionAs)
    {
      case CollectionsCSharp.List:
        datatype = datatype.Replace("[]", "");
        datatype = $"List<{datatype}>";
        break;
      case CollectionsCSharp.IEnumerable:
        datatype = datatype.Replace("[]", "");
        datatype = $"IEnumerable<{datatype}>";
        break;
      case CollectionsCSharp.ICollection:
        datatype = datatype.Replace("[]", "");
        datatype = $"ICollection<{datatype}>";
        break;
      case CollectionsCSharp.Array:
        datatype = $"{datatype}";
        break;
    }

    return datatype;
  }

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