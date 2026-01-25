public interface ICSharpJsonOption
{
  public string Name { get; }
  public string Using { get; }
  public string PropertyAnnotation { get; }
}

public class SystemTextJsonOption : ICSharpJsonOption
{
  public string Name => "System.Text.Json";
  public string Using => "System.Text.Json.Serialization";
  public string PropertyAnnotation => "JsonPropertyName";
}

public class NewtonsoftJsonOption : ICSharpJsonOption
{
  public string Name => "Newtonsoft.Json";
  public string Using => "Newtonsoft.Json";
  public string PropertyAnnotation => "JsonProperty";
}

public class CSharpOptions : ILanguageOptions
{
  public string Language { get; } = "C#";
  public INamingConvention NamingConvention { get; set; } = new AsIsCase();
  public ICSharpJsonOption? CSharpJsonOptions { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; }

  public Array GetCollectionOptions() => Enum.GetValues<CSharpCollections>();

  public string ParseObject(JNodeClass jnc)
  {
    var className = this.NamingConvention.Parse(jnc.Name);

    var classStr = $"public class {className}{Environment.NewLine}{{{Environment.NewLine}";

    for (int i = 0; i < jnc.Kvps.Count; i++)
    {
      var kvp = jnc.Kvps[i];
      var datatype = kvp.Kvp.Value;

      datatype = datatype.Contains("DateTime") ? datatype : datatype.ToLower();

      datatype = kvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = kvp.CollectionAs is not null ?
        ConfigureCollection(datatype, kvp.Nullable, kvp.CollectionAs!, kvp.CollectionItemNullable!.Value) :
        datatype;

      datatype = kvp.Nullable ? $"{datatype}?" : datatype;

      var propName = this.NamingConvention.Parse(kvp.Kvp.Key);
      var jsonAnnotation = "";
      var mapFrom = kvp.MapFrom is not null ? kvp.MapFrom : kvp.Kvp.Key;
      jsonAnnotation = this.CSharpJsonOptions is not null && this.NamingConvention.Name is not "AsIs" ?
      $"  [{this.CSharpJsonOptions.PropertyAnnotation}(\"{mapFrom}\")]{Environment.NewLine}" : "";
      jsonAnnotation = this.CSharpJsonOptions is null && kvp.MapFrom is not null ?
      $"  // Use System.Text.Json/Newtonsoft.Json to map this property{Environment.NewLine}" : jsonAnnotation;

      var propLine = $"  public {datatype} {propName} {{ get; set; }}{Environment.NewLine}";
      var newLine = jsonAnnotation.Length > 0 && i != jnc.Kvps.Count - 1 ? Environment.NewLine : "";

      classStr += jsonAnnotation + propLine + newLine;
    }

    return classStr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  public string ConfigureCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable)
  {
    var _collection = Enum.Parse(typeof(CSharpCollections), collection);

    switch (_collection)
    {
      case CSharpCollections.List:
        datatype = datatype.Replace("[]", "");
        datatype = collectionItemNullable ? $"List<{datatype}?>" : $"List<{datatype}>";
        break;
      case CSharpCollections.IEnumerable:
        datatype = datatype.Replace("[]", "");
        datatype = collectionItemNullable ? $"IEnumerable<{datatype}?>" : $"IEnumerable<{datatype}>";
        break;
      case CSharpCollections.ICollection:
        datatype = datatype.Replace("[]", "");
        datatype = collectionItemNullable ? $"ICollection<{datatype}?>" : $"ICollection<{datatype}>";
        break;
      case CSharpCollections.Array:
        datatype = datatype.Replace("[]", "");
        datatype = collectionItemNullable ? $"{datatype}?[]" : $"{datatype}[]";
        break;
    }

    return datatype;
  }
}
