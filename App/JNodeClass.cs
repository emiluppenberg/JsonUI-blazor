using System.Text.RegularExpressions;
using static General;

public record JNodeClass(string name, List<JNodeKvp> kvps)
{
  public string Name { get; set; } = name;
  public List<JNodeKvp> Kvps { get; } = kvps;

  public string GetClassName(CSharpOptions options)
  {
    var name = this.Name;

    name = options.UsePascalCase ?
      Regex.Replace(name, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper()) :
      name.ToLower();

    return $"public class {name} {Environment.NewLine}" +
       $"{{{Environment.NewLine}";
  }

  public string GetProperty(int i, CSharpOptions options)
  {
    var datatype = this.Kvps[i].GetKvpDatatype(options);
    var propName = this.Kvps[i].GetKvpName(options);
    var jsonAnnotation = $"  [JsonProperty(\"{this.Kvps[i].Kvp.Key}\")]{Environment.NewLine}";

    var propLine = $"  public {datatype} {propName} {{ get; set; }}{Environment.NewLine}";
    var newLine = options.UsePascalCase && i != this.Kvps.Count - 1 ? Environment.NewLine : "";

    return options.UsePascalCase ?
      jsonAnnotation + propLine + newLine :
      propLine;
  }
}