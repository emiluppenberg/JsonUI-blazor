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

  public string ConfigureCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable)
  {
    var _collection = Enum.Parse(typeof(TypeScriptCollections), collection);

    switch (_collection)
    {
      case TypeScriptCollections.Set:
        datatype = collectionItemNullable ?
        $"({datatype.Replace("(", "").Replace(")", "").Replace("[]", "")} | null)" :
        datatype;

        datatype = datatypeNullable ? $"Set<{datatype}> | null" : $"Set<{datatype}>";
        break;
      case TypeScriptCollections.Array:
        datatype = collectionItemNullable ?
        $"({datatype.Replace("(", "").Replace(")", "").Replace("[]", "")} | null)[]" :
        datatype;

        datatype = datatypeNullable ? $"{datatype} | null" : datatype;
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
      var jnKvp = jnc.Kvps[i];
      var datatype = jnKvp.Kvp.Value;

      datatype = jnKvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = jnKvp.CollectionAs is not null ?
        ConfigureCollection(datatype, jnKvp.Nullable, jnKvp.CollectionAs!, jnKvp.CollectionItemNullable!.Value) : datatype;

      datatype = jnKvp.Nullable && jnKvp.CollectionAs is null ? $"{datatype} | null" : datatype;

      var propName = this.NamingConvention.Parse(jnKvp.Kvp.Key);
      var optional = jnKvp.Optional ? "?" : "";
      var propLine = $"  {propName}{optional}: {datatype};{Environment.NewLine}";

      typestr += propLine;
    }

    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  public Array GetCollectionOptions() => Enum.GetValues<TypeScriptCollections>();
}