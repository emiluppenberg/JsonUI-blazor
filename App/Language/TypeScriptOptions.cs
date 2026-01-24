public interface ITypeScriptTypeOption
{
  public string Name { get; }
  public bool UseZodSchema { get; set; }
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class InterfaceTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "interface";
  public bool UseZodSchema { get; set; }
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class TypeTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "type";
  public bool UseZodSchema { get; set; }
  public bool? UsePrimaryConstructor { get; set; }
  public bool? UseRegularConstructor { get; set; }
}

public class ClassTypeOption : ITypeScriptTypeOption
{
  public string Name { get; } = "class";
  public bool UseZodSchema { get; set; }
  public bool? UsePrimaryConstructor { get; set; } = false;
  public bool? UseRegularConstructor { get; set; } = false;
}

public class TypeScriptOptions : ILanguageOptions
{
  public string Language { get; } = "TypeScript";
  public INamingConvention NamingConvention { get; set; } = new AsIsCase();
  public ICSharpJsonOption? CSharpJsonOptions { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; } = new InterfaceTypeOption();
  public Array GetCollectionOptions() => Enum.GetValues<TypeScriptCollections>();

  public string ParseObject(JNodeClass jnc)
  {
    var typestr = "";

    switch (jnc.TypeOption!.Name)
    {
      case "interface":
        typestr += ParseInterface(jnc);
        break;
      case "type":
        typestr += ParseType(jnc);
        break;
      case "class":
        typestr += ParseClass(jnc);
        break;
    }

    if (jnc.TypeOption!.UseZodSchema)
    {
      switch (jnc.TypeOption!.Name)
      {
        case "interface":
          typestr += ParseInterfaceZodSchema(jnc);
          break;
        case "type":
          typestr += ParseTypeZodSchema(jnc);
          break;
        case "class":
          typestr += ParseClassZodSchema(jnc);
          break;
      }
    }

    return typestr;
  }

  private string ParseInterface(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);
    var typeheader = $"interface {typename}";
    var typestr = $"{typeheader} {{{Environment.NewLine}";
    typestr += ParseProperties(jnc.Kvps, 2, "", ";");
    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  private string ParseType(JNodeClass jnc)
  {
    if (jnc.TypeOption!.UseZodSchema)
    {
      return "";
    }

    var typename = this.NamingConvention.Parse(jnc.Name);
    var typeheader = $"type {typename} =";
    var typestr = $"{typeheader} {{{Environment.NewLine}";

    typestr += ParseProperties(jnc.Kvps, 2, "", ";");
    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  private string ParseClass(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);
    var typeheader = $"class {typename}";
    var typestr = $"{typeheader} {{{Environment.NewLine}";

    if (jnc.TypeOption!.UsePrimaryConstructor.GetValueOrDefault() == true)
    {
      typestr += $"  constructor({Environment.NewLine}";
      typestr += ParseProperties(jnc.Kvps, 4, "public ", ",");
      return typestr + $"  ) {{}}{Environment.NewLine}";
    }

    typestr += ParseProperties(jnc.Kvps, 2, "", ";");

    if (jnc.TypeOption.UseRegularConstructor.GetValueOrDefault() == true)
    {
      typestr += $"{Environment.NewLine}  constructor({Environment.NewLine}";
      typestr += ParseProperties(jnc.Kvps, 4, "", ",");
      typestr += $"  ) {{{Environment.NewLine}";

      for (int i = 0; i < jnc.Kvps.Count; i++)
      {
        var jnKvp = jnc.Kvps[i];
        var propName = this.NamingConvention.Parse(jnKvp.Kvp.Key);
        var propLine = $"      this.{propName} = {propName};{Environment.NewLine}";

        typestr += propLine;
      }
    }

    typestr += $"    }}{Environment.NewLine}";
    return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  private string ParseProperties(List<JNodeKvp> jncKvps, int indent, string access, string endLine)
  {
    var typestr = "";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var kvp = jncKvps[i];
      var datatype = kvp.Kvp.Value;

      datatype = kvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = kvp.CollectionAs is not null ?
        ConfigureCollection(datatype, kvp.Nullable, kvp.CollectionAs!, kvp.CollectionItemNullable!.Value) : datatype;

      datatype = kvp.Nullable && kvp.CollectionAs is null ? $"{datatype} | null" : datatype;

      var propName = this.NamingConvention.Parse(kvp.Kvp.Key);
      var optional = kvp.Optional ? "?" : "";
      var indentation = new string(' ', indent);
      var propLine = $"{indentation}{access}{propName}{optional}: {datatype}{endLine}{Environment.NewLine}";
      typestr += propLine;
    }

    return typestr;
  }

  private string ParseZodProperties(List<JNodeKvp> jncKvps)
  {
    var zodstr = "";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var kvp = jncKvps[i];
      var rawDatatype = "";
      rawDatatype = kvp.CollectionAs is not null ? kvp.Kvp.Value.Replace("[]", "") : kvp.Kvp.Value;
      rawDatatype = kvp.CollectionItemNullable is not null ? kvp.Kvp.Value.Replace("(", "").Replace(")", "") : rawDatatype;


      var datatype = $"z.{rawDatatype}()";
      datatype = kvp.Nested ? $"{this.NamingConvention.Parse(rawDatatype)}Schema" : datatype;

      datatype = kvp.CollectionAs is not null ?
        ConfigureZodCollection(datatype, kvp.Nullable, kvp.CollectionAs, kvp.CollectionItemNullable!.Value) : datatype;

      datatype = kvp.Nullable && kvp.CollectionAs is null ? $"{datatype}.nullable()" : datatype;
      datatype = kvp.Optional ? $"{datatype}.optional()" : datatype;

      var propName = this.NamingConvention.Parse(kvp.Kvp.Key);
      var endLine = i == jncKvps.Count - 1 ? Environment.NewLine : $",{Environment.NewLine}";
      var propLine = $"  {propName}: {datatype}{endLine}";
      zodstr += propLine;
    }

    return zodstr + $"}});{Environment.NewLine}{Environment.NewLine}";
  }

  public string ConfigureZodCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable)
  {
    var _collection = Enum.Parse(typeof(TypeScriptCollections), collection);

    switch (_collection)
    {
      case TypeScriptCollections.Set:
        datatype = $"z.set({datatype}";
        break;
      case TypeScriptCollections.Array:
        datatype = $"z.array({datatype}";
        break;
    }

    datatype = collectionItemNullable ? $"{datatype}.nullable()" : datatype;
    datatype = datatypeNullable ? $"{datatype}).nullable()" : $"{datatype})";
    return datatype;
  }

  private string ParseInterfaceZodSchema(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);
    var zodstr = $"const {typename}Schema: z.ZodType<{typename}> = z.object({{{Environment.NewLine}";
    return zodstr + ParseZodProperties(jnc.Kvps);
  }

  private string ParseTypeZodSchema(JNodeClass jnc)
  {
    var zodstr = "";
    return zodstr;
  }

  private string ParseClassZodSchema(JNodeClass jnc)
  {
    var zodstr = "";
    return zodstr;
  }

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
}