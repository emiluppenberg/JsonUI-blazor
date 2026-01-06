using System.Text.RegularExpressions;
using static General;

public class JNodeKvp
{
  public KeyValuePair<string, string> Kvp { get; set; }
  public bool IsSelected { get; set; }
  public bool Nullable { get; set; }
  public bool Nested { get; set; } = false;
  public CollectionsCSharp? CollectionAs { get; set; }

  public JNodeKvp(KeyValuePair<string, string> kvp)
  {
    Kvp = kvp;
    CollectionAs = kvp.Value.Contains("[]") ? CollectionsCSharp.Array : null;
  }

  public JNodeKvp(JNode node)
  {
    var datatype = node.Type == JNodeType.Array ? $"{node.Name}[]" : node.Name;
    Kvp = new KeyValuePair<string, string>(node.Name, datatype);
    Nested = node.Parent is not null;
    Nullable = node.Nullable;
    CollectionAs = node.CollectionAs;
  }

  public string GetKvpDatatype(CSharpOptions options)
  {
    var datatype = this.Kvp.Value;

    datatype = options.JsonOptions is not null && Nested ?
      options.JsonOptions.NamingConvention.GetName(datatype) :
      datatype.ToLower();

    datatype = Nullable ? $"{datatype}?" : datatype;
    datatype = CollectionAs is not null ? ConfigureCollection(datatype, CollectionAs.Value) : datatype;
    datatype = Nullable && CollectionAs is not null ? $"{datatype}?" : datatype;

    return datatype;
  }

  public string GetKvpName(CSharpOptions options)
  {
    return options.JsonOptions is not null ?
      options.JsonOptions.NamingConvention.GetName(this.Kvp.Key) :
      this.Kvp.Key.ToLower();
  }

  public bool IsArray() => CollectionAs is not null;
}