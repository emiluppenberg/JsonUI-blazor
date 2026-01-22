using System.Text.RegularExpressions;
using static General;

public record JNodeClass
{
  public string Name { get; set; } = "";
  public List<JNodeKvp> Kvps { get; } = new();

  // TypeScript
  public ITypeScriptTypeOption? TypeOption { get; set; }

  public JNodeClass(JNode node, List<JNodeKvp> kvps)
  {
    TypeOption = node.TypeOption;
    Name = node.Name;
    Kvps = kvps;
  }
}