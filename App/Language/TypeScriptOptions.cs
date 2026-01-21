public interface ITypeOption
{
  public string Name { get; }
  public bool UseRawType { get; set; }
}

public class InterfaceTypeOption : ITypeOption
{
  public string Name { get; } = "Interface";
  public bool UseRawType { get; set; }
}

public class TypeScriptOptions : ILanguageOptions
{
  public string Language { get; } = "TypeScript";

  public INamingConvention NamingConvention { get; set; } = new AsIsCase();

  public ICSharpJsonOption? CSharpJsonOptions { get; set; }

  public ITypeOption? TypeOption { get; set; } = new InterfaceTypeOption();

  public string ConfigureCollection(string datatype, string collection)
  {
    var _collection = Enum.Parse(typeof(TypeScriptCollections), collection);
    var isNullable = datatype.Contains(" | null");

    switch (_collection)
    {
      case TypeScriptCollections.Set:
        datatype = datatype.Replace("[]", "");
        datatype = isNullable ? $"Set<{datatype}> | null" : $"Set<{datatype}>";
        break;
      case TypeScriptCollections.Array:
        datatype = datatype.Replace("[]", "");
        datatype = datatype.Replace(" | null", "").Contains("|") ? $"({datatype})[]" : $"{datatype}[]";
        datatype = isNullable ? $"{datatype} | null" : datatype;
        break;
    }

    return datatype;
  }

  public string ParseObject(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);

    var typestr = $"{this.TypeOption!.Name.ToLower()} {typename} {{{Environment.NewLine}";

    for (int i = 0; i < jnc.Kvps.Count; i++)
    {
      var datatype = jnc.Kvps[i].Kvp.Value;

      datatype = jnc.Kvps[i].Nested ?
        this.NamingConvention.Parse(datatype) :
        datatype;

      datatype = jnc.Kvps[i].Nullable ? $"{datatype} | null" : datatype;
      datatype = jnc.Kvps[i].CollectionAs is not null ? ConfigureCollection(datatype, jnc.Kvps[i].CollectionAs!) : datatype;
      // datatype = jnc.Kvps[i].Nullable && jnc.Kvps[i].CollectionAs is not null && jnc.Kvps[i].CollectionAs is not "Array" ? $"{datatype}?" : datatype;

      var propName = this.NamingConvention.Parse(jnc.Kvps[i].Kvp.Key);

      var propLine = $"  {propName}: {datatype};{Environment.NewLine}";

      typestr += propLine;
    }

    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  public Array GetCollectionOptions() => Enum.GetValues<TypeScriptCollections>();
}