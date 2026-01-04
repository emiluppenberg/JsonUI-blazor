public record KvpSelectedArgs(JNode node, bool selected);

public static class General
{
  public const int RowHeightPx = 22;
  public const int ColWidthPx = 160;

  public struct CSharpOptions()
  {
    public bool UsePascalCase { get; set; }
    public bool DefaultNullable { get; set; }
    public CollectionType DefaultCollection { get; set; }
  }

  public enum CollectionType
  {
    List, IEnumerable, ICollection, Array
  }

  public enum JNodeType
  {
    Object, Array
  }

  public static Array GetCollectionOptions() => Enum.GetValues<CollectionType>();

  public static string ConfigureCollection(string datatype, CollectionType collectionAs)
  {
    switch (collectionAs)
    {
      case CollectionType.List:
        datatype = datatype.Replace("[]", "");
        datatype = $"List<{datatype}>";
        break;
      case CollectionType.IEnumerable:
        datatype = datatype.Replace("[]", "");
        datatype = $"IEnumerable<{datatype}>";
        break;
      case CollectionType.ICollection:
        datatype = datatype.Replace("[]", "");
        datatype = $"ICollection<{datatype}>";
        break;
      case CollectionType.Array:
        datatype = $"{datatype}";
        break;
    }

    return datatype;
  }
}