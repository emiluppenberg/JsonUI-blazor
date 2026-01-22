public interface ITypeScriptTypeOption
{
  public string Name { get; }
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class InterfaceTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "interface";
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class TypeTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "type";
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class ClassTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "class";
  public bool? UsePrimaryConstructor { get; set; } = false;
  public bool? UseRegularConstructor { get; set; } = false;
}

public class TypeScriptOptions : ILanguageOptions
{
  public string Language { get; } = "TypeScript";
  public bool? UseRaw { get; set; } = false;
  public INamingConvention NamingConvention { get; set; } = new AsIsCase();
  public ICSharpJsonOption? CSharpJsonOptions { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; } = new InterfaceTypeOption();
  public Array GetCollectionOptions() => Enum.GetValues<TypeScriptCollections>();

  public string ConfigureCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable)
  {
    var _collection = Enum.Parse(typeof(TypeScriptCollections), collection);

    switch (_collection)
    {
      case TypeScriptCollections.Set:
        datatype = collectionItemNullable ?
        $"({datatype.Replace("(", "").Replace(")", "")} | null)" :
        datatype;

        datatype = datatypeNullable ? $"Set<{datatype.Replace("[]", "")}> | null" : $"Set<{datatype.Replace("[]", "")}>";
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
    string typeheader = "";

    if (jnc.TypeOption!.Name == "interface") typeheader = $"interface {typename}";
    if (jnc.TypeOption!.Name == "type") typeheader = $"type {typename} =";
    if (jnc.TypeOption!.Name == "class") typeheader = $"class {typename}";

    var typestr = $"{typeheader} {{{Environment.NewLine}";

    if (jnc.TypeOption.UsePrimaryConstructor.GetValueOrDefault() == true)
    {
      typestr = ParseClassPrimaryConstructor(jnc.Kvps, typestr);

      return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
    }

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

    if (jnc.TypeOption.UseRegularConstructor.GetValueOrDefault() == true)
    {
      typestr = ParseClassRegularConstructor(jnc.Kvps, typestr);
    }

    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  private string ParseClassRegularConstructor(List<JNodeKvp> jncKvps, string typestr)
  {
    typestr += $"{Environment.NewLine}  constructor({Environment.NewLine}";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var jnKvp = jncKvps[i];
      var datatype = jnKvp.Kvp.Value;

      datatype = jnKvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = jnKvp.CollectionAs is not null ?
        ConfigureCollection(datatype, jnKvp.Nullable, jnKvp.CollectionAs!, jnKvp.CollectionItemNullable!.Value) : datatype;

      datatype = jnKvp.Nullable && jnKvp.CollectionAs is null ? $"{datatype} | null" : datatype;

      var propName = this.NamingConvention.Parse(jnKvp.Kvp.Key);
      var optional = jnKvp.Optional ? "?" : "";
      var propLine = $"    {propName}{optional}: {datatype},{Environment.NewLine}";

      typestr += propLine;
    }

    typestr += $"  ) {{{Environment.NewLine}";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var jnKvp = jncKvps[i];
      var propName = this.NamingConvention.Parse(jnKvp.Kvp.Key);
      var propLine = $"      this.{propName} = {propName};{Environment.NewLine}";

      typestr += propLine;
    }

    return typestr + $"  }} {{{Environment.NewLine}";
  }

  private string ParseClassPrimaryConstructor(List<JNodeKvp> jncKvps, string typestr)
  {
    typestr += $"  constructor({Environment.NewLine}";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var jnKvp = jncKvps[i];
      var datatype = jnKvp.Kvp.Value;

      datatype = jnKvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = jnKvp.CollectionAs is not null ?
        ConfigureCollection(datatype, jnKvp.Nullable, jnKvp.CollectionAs!, jnKvp.CollectionItemNullable!.Value) : datatype;

      datatype = jnKvp.Nullable && jnKvp.CollectionAs is null ? $"{datatype} | null" : datatype;

      var propName = this.NamingConvention.Parse(jnKvp.Kvp.Key);
      var optional = jnKvp.Optional ? "?" : "";
      var propLine = $"    public {propName}{optional}: {datatype},{Environment.NewLine}";

      typestr += propLine;
    }

    return typestr + $"  ) {{}}{Environment.NewLine}";
  }
}