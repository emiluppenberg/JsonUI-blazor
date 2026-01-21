using System.Text.RegularExpressions;
using static General;

public class JNodeKvp
{
  public KeyValuePair<string, string> Kvp { get; set; }
  public bool IsSelected { get; set; }
  public bool Nullable { get; set; }
  public bool Optional { get; set; }
  public bool Nested { get; set; } = false;
  public string? CollectionAs { get; set; }
  public bool? CollectionItemNullable { get; set; }

  public JNodeKvp(KeyValuePair<string, string> kvp, ILanguageOptions langOptions)
  {
    Kvp = kvp;
    CollectionAs = kvp.Value.Contains("[]") ? langOptions.GetCollectionOptions().GetValue(0)!.ToString() : null;
    CollectionItemNullable = kvp.Value.Contains("[]") ? false : null;
  }

  public JNodeKvp(JNode node)
  {
    var datatype = node.Type == JNodeType.Array ? $"{node.Name}[]" : node.Name;
    Kvp = new KeyValuePair<string, string>(node.Name, datatype);
    Nested = node.Parent is not null;
    Nullable = node.Nullable;
    CollectionAs = node.CollectionAs;
    CollectionItemNullable = node.CollectionItemNullable;
  }

  public bool IsArray() => CollectionAs is not null;
}