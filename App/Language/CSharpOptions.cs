public interface ICSharpJsonOptions
{
  public string Name { get; }
  public string Using { get; }
  public string PropertyAnnotation { get; }
}

public class SystemTextJsonOption : ICSharpJsonOptions
{
  public string Name => "System.Text.Json";
  public string Using => "System.Text.Json.Serialization";
  public string PropertyAnnotation => "JsonPropertyName";
}

public class NewtonsoftJsonOption : ICSharpJsonOptions
{
  public string Name => "Newtonsoft.Json";
  public string Using => "Newtonsoft.Json";
  public string PropertyAnnotation => "JsonProperty";
}

public class CSharpOptions : ILanguageOptions
{
  public string Language { get; } = "C#";

  public INamingConvention NamingConvention { get; set; } = new LowerSnakeCase();

  public ICSharpJsonOptions? CSharpJsonOptions { get; set; }

  public Array GetCollectionOptions() => Enum.GetValues<CSharpCollections>();

  public string ParseObject(JNodeClass jnc)
  {
    var className = this.NamingConvention.Parse(jnc.Name);

    var classStr = $"public class {className} {Environment.NewLine}" +
       $"{{{Environment.NewLine}";

    for (int i = 0; i < jnc.Kvps.Count; i++)
    {
      var datatype = jnc.Kvps[i].Kvp.Value;

      datatype = jnc.Kvps[i].Nested ?
        this.NamingConvention.Parse(datatype) :
        datatype;

      datatype = datatype.Contains("DateTime") ? datatype : datatype.ToLower();

      datatype = jnc.Kvps[i].Nullable ? $"{datatype}?" : datatype;
      datatype = jnc.Kvps[i].CollectionAs is not null ? ConfigureCollection(datatype, jnc.Kvps[i].CollectionAs!) : datatype;
      datatype = jnc.Kvps[i].Nullable && jnc.Kvps[i].CollectionAs is not null && jnc.Kvps[i].CollectionAs is not "Array" ? $"{datatype}?" : datatype;

      var propName = this.NamingConvention.Parse(jnc.Kvps[i].Kvp.Key);

      var jsonAnnotation = this.CSharpJsonOptions is not null && this.NamingConvention.Name is not "None" ?
        $"  [{this.CSharpJsonOptions.PropertyAnnotation}(\"{jnc.Kvps[i].Kvp.Key}\")]{Environment.NewLine}" :
        "";

      var propLine = $"  public {datatype} {propName} {{ get; set; }}{Environment.NewLine}";
      var newLine = jsonAnnotation.Length > 0 && i != jnc.Kvps.Count - 1 ? Environment.NewLine : "";

      classStr += jsonAnnotation + propLine + newLine;
    }

    return classStr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  public string ConfigureCollection(string datatype, string collectionAs)
  {
    var _collectionAs = Enum.Parse(typeof(CSharpCollections), collectionAs);

    switch (_collectionAs)
    {
      case CSharpCollections.List:
        datatype = datatype.Replace("[]", "");
        datatype = $"List<{datatype}>";
        break;
      case CSharpCollections.IEnumerable:
        datatype = datatype.Replace("[]", "");
        datatype = $"IEnumerable<{datatype}>";
        break;
      case CSharpCollections.ICollection:
        datatype = datatype.Replace("[]", "");
        datatype = $"ICollection<{datatype}>";
        break;
      case CSharpCollections.Array:
        var split = datatype.Contains('?') ?
        new[]
        {
          datatype.Substring(0, datatype.IndexOf('[')),
          datatype.Substring(datatype.IndexOf('['))
        } :
        null;
        datatype = split is not null ? $"{split[0]}?{split[1]}" : $"{datatype}";
        break;
    }

    return datatype;
  }
}
