using static General;

public class JNode
{
  public JNodeType Type;
  public string LineageKey { get; }
  public string ParentKey { get; }
  public string Name { get; }
  public List<JNodeKvp> KeyValues { get; }
  public List<JNode> Children { get; set; }
  public JNode? Parent { get; set; }
  public bool IsExpanded { get; set; }
  public bool Nullable { get; set; }
  public bool Optional { get; set; }
  public bool AllowUndefined { get; set; }
  public string? CollectionAs { get; set; }
  public bool? CollectionItemNullable { get; set; }
  public bool? CollectionItemAllowUndefined { get; set; }
  public int TotalNestedSelected { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; }

  public JNode(string lineageKey, string parentKey, string name, List<JNodeKvp> keyValues, ILanguageOptions langOptions)
  {
    Type = lineageKey.EndsWith("{}") ? JNodeType.Object : JNodeType.Array;
    Name = name.Replace("{}", null).Replace("[]", null);
    LineageKey = lineageKey;
    ParentKey = parentKey;
    KeyValues = keyValues;
    Children = new();
    CollectionAs = Type == JNodeType.Array ? langOptions.GetCollectionOptions().GetValue(0)!.ToString() : null;
    CollectionItemNullable = Type == JNodeType.Array ? false : null;
    CollectionItemAllowUndefined = Type == JNodeType.Array && langOptions.Language == "TypeScript" ? false : null;
    TypeOption = langOptions.Language == "TypeScript" ? langOptions.TypeOption : null;
  }

  public void SetNullable(bool nullable)
  {
    this.Nullable = nullable;
    this.KeyValues.ForEach(x => x.Nullable = nullable);
  }

  public void SetNodeCollectionAs(string collectionAs)
  {
    if (this.CollectionAs is not null)
    {
      this.CollectionAs = collectionAs;
    }
  }

  public void SetKvpsCollectionAs(string collectionAs)
  {
    this.KeyValues
      .Where(x => x.CollectionAs is not null)
      .ToList()
      .ForEach(x => x.CollectionAs = collectionAs);
  }
}