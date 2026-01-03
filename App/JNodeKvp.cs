using System.Text.RegularExpressions;
using static General;

public class JNodeKvp
{
  public KeyValuePair<string, string> Kvp { get; set; }
  public bool IsSelected { get; set; }
  public bool Nullable { get; set; }
  public bool Nested { get; set; } = false;
  public CollectionType? CollectionAs { get; set; }

  public JNodeKvp(KeyValuePair<string, string> kvp)
  {
    Kvp = kvp;
    CollectionAs = kvp.Value.Contains("[]") ? CollectionType.Array : null;
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

    datatype = options.UsePascalCase && Nested ?
      Regex.Replace(datatype, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper()) :
      datatype.ToLower();

    datatype = Nullable ? $"{datatype}?" : datatype;
    datatype = CollectionAs is not null ? ConfigureCollection(datatype, CollectionAs.Value) : datatype;
    datatype = Nullable && CollectionAs is not null ? $"{datatype}?" : datatype;

    return datatype;
  }

  public string GetKvpName(CSharpOptions options)
  {
    return options.UsePascalCase ?
      Regex.Replace(this.Kvp.Key, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper()) :
      this.Kvp.Key.ToLower();
  }

  public bool IsArray() => CollectionAs is not null;
}