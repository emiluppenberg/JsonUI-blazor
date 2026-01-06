public enum JsonPackageType
{
  SystemTextJson, NewtonsoftJson
}

public class JsonOptionsCSharp
{
  public INamingConvention NamingConvention { get; set; } = new PascalCase();
  public JsonPackage JsonPackage { get; set; } = new(JsonPackageType.SystemTextJson);
  public bool UseSerializerOptions { get; set; }
}

public class JsonPackage(JsonPackageType jsonPackageType)
{
  public string PropertyAnnotation { get; set; } = jsonPackageType is JsonPackageType.SystemTextJson ? "JsonPropertyName" : "JsonProperty";
}

public enum CollectionsCSharp
{
  List, IEnumerable, ICollection, Array
}

public class CSharpOptions : ILanguageOptions
{
  public JsonOptionsCSharp? JsonOptions { get; set; }

  public Array GetCollectionOptions() => Enum.GetValues<CollectionsCSharp>();

  public string GetClass(JNodeClass jnc)
  {
    var className = this.JsonOptions is not null ?
      this.JsonOptions.NamingConvention.GetName(jnc.Name) :
      jnc.Name.ToLower();

    var classStr = $"public class {className} {Environment.NewLine}" +
       $"{{{Environment.NewLine}";

    for (int i = 0; i < jnc.Kvps.Count; i++)
    {
      var datatype = jnc.Kvps[i].Kvp.Value;

      datatype = this.JsonOptions is not null && jnc.Kvps[i].Nested ?
        this.JsonOptions.NamingConvention.GetName(datatype) :
        datatype.ToLower();

      datatype = jnc.Kvps[i].Nullable ? $"{datatype}?" : datatype;
      datatype = jnc.Kvps[i].CollectionAs is not null ? ConfigureCollection(datatype, jnc.Kvps[i].CollectionAs!.ToString()) : datatype;
      datatype = jnc.Kvps[i].Nullable && jnc.Kvps[i].CollectionAs is not null ? $"{datatype}?" : datatype;

      var propName = this.JsonOptions is not null ?
        this.JsonOptions.NamingConvention.GetName(jnc.Kvps[i].Kvp.Key) :
        jnc.Kvps[i].Kvp.Key.ToLower();

      var jsonAnnotation = this.JsonOptions is not null ?
        $"  [{this.JsonOptions.JsonPackage.PropertyAnnotation}(\"{jnc.Kvps[i].Kvp.Key}\")]{Environment.NewLine}" :
        "";

      var propLine = $"  public {datatype} {propName} {{ get; set; }}{Environment.NewLine}";
      var newLine = jsonAnnotation.Length > 0 && i != jnc.Kvps.Count - 1 ? Environment.NewLine : "";

      classStr += jsonAnnotation + propLine + newLine;
    }

    return classStr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  public string ConfigureCollection(string datatype, string collectionAs)
  {
    var _collectionAs = Enum.Parse(typeof(CollectionsCSharp), collectionAs);

    switch (_collectionAs)
    {
      case CollectionsCSharp.List:
        datatype = datatype.Replace("[]", "");
        datatype = $"List<{datatype}>";
        break;
      case CollectionsCSharp.IEnumerable:
        datatype = datatype.Replace("[]", "");
        datatype = $"IEnumerable<{datatype}>";
        break;
      case CollectionsCSharp.ICollection:
        datatype = datatype.Replace("[]", "");
        datatype = $"ICollection<{datatype}>";
        break;
      case CollectionsCSharp.Array:
        datatype = $"{datatype}";
        break;
    }

    return datatype;
  }
}
