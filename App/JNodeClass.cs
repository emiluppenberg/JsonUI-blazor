using System.Text.RegularExpressions;
using static General;

public record JNodeClass(string name, List<JNodeKvp> kvps)
{
  public string Name { get; set; } = name;
  public List<JNodeKvp> Kvps { get; } = kvps;
}