using static General;

public class JNode
{
  public JNodeType Type;
  public string LineageKey { get; }
  public string Name { get; }
  public List<JNodeKvp> KeyValues { get; }
  public List<JNode> Children { get; set; }
  public JNode? Parent { get; set; }
  public bool IsExpanded { get; set; }
  public bool Nullable { get; set; }
  public CollectionType? CollectionAs { get; set; }

  public JNode(string lineageKey, string name, List<JNodeKvp> keyValues, JNode? parent, CSharpOptions options)
  {
    Name = name.Replace("{}", null).Replace("[]", null);
    Type = name.Contains("{}") ? JNodeType.Object : JNodeType.Array;
    LineageKey = lineageKey;
    KeyValues = keyValues;
    Children = new();
    Parent = parent;
    Nullable = options.DefaultNullable;
    CollectionAs = Type == JNodeType.Array ? CollectionType.Array : null;
  }

  public void SetNullable(bool nullable)
  {
    this.Nullable = nullable;
    this.KeyValues.ForEach(x => x.Nullable = nullable);
  }

  public void SetNodeCollectionAs(CollectionType collectionAs)
  {
    if (this.CollectionAs is not null)
    {
      this.CollectionAs = collectionAs;
    }
  }

  public void SetKvpsCollectionAs(CollectionType collectionAs)
  {
    this.KeyValues
      .Where(x => x.CollectionAs is not null)
      .ToList()
      .ForEach(x => x.CollectionAs = collectionAs);
  }

  public bool AnyChildSelected()
  {
    var isSelected = this.KeyValues.Any(x => x.IsSelected);
    if (isSelected) return isSelected;

    var anySelected = this.Children.Select(x => x.AnyChildSelected());
    if (anySelected.Any(x => x == true)) return true;

    return false;
  }
}