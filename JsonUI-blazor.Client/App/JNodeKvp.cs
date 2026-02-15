using System.Text.RegularExpressions;
using static General;

public class JNodeKvp
{
  public KeyValuePair<string, string> Kvp { get; set; }
  public bool IsSelected { get; set; }
  public bool Nullable { get; set; }
  public bool Optional { get; set; }
  public bool AllowUndefined { get; set; }
  public bool Nested { get; set; }
  public bool IsUnion { get; set; }
  public bool DataNullable { get; set; }
  public Dictionary<string, List<string>>? JsonLibraryAnnotations { get; set; }
  public string? MapFrom { get; set; }
  public string? CollectionAs { get; set; }
  public bool? CollectionItemNullable { get; set; }
  public bool? CollectionItemAllowUndefined { get; set; }
  public bool IsPrivate { get; set; }
  public bool HasField { get; set; }
  public bool HasGet { get; set; } = true;
  public bool HasSet { get; set; } = true;

  public JNodeKvp(KeyValuePair<string, string> kvp, ILanguageOptions langOptions)
  {
    Kvp = kvp;
    Nullable = kvp.Value.Contains("object") || kvp.Value.Contains("null");
    CollectionAs = kvp.Value.Contains("[]") ? langOptions.GetCollectionOptions().GetValue(0)!.ToString() : null;
    CollectionItemNullable = kvp.Value.Contains("[]") ? false : null;
    CollectionItemAllowUndefined = kvp.Value.Contains("[]") && langOptions.Language == "TypeScript" ? false : null;
    IsUnion = kvp.Value.Contains("|") ? true : false;
    DataNullable = kvp.Value.Contains("object") || kvp.Value.Contains("null");
    JsonLibraryAnnotations = langOptions.Language == "C#" ? new() : null;
  }

  public JNodeKvp(JNode node)
  {
    var datatype = node.Type == JNodeType.Array ? $"{node.Name}[]" : node.Name;
    Kvp = new KeyValuePair<string, string>(node.Name, datatype);
    Nested = node.Parent is not null;
    Nullable = node.Nullable;
    Optional = node.Optional;
    AllowUndefined = node.AllowUndefined;
    CollectionAs = node.CollectionAs;
    CollectionItemNullable = node.CollectionItemNullable;
    CollectionItemAllowUndefined = node.CollectionItemAllowUndefined;
    JsonLibraryAnnotations = node.JsonLibraryAnnotations;
    IsPrivate = node.IsPrivate;
    HasField = node.HasField;
    HasGet = node.HasGet;
    HasSet = node.HasSet;
  }

  public bool IsArray() => CollectionAs is not null;
}