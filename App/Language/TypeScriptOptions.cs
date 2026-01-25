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
  public bool? UsePrimaryConstructor { get; set; } = true;
  public bool? UseRegularConstructor { get; set; } = false;
}

public class TypeScriptOptions : ILanguageOptions
{
  public string Language { get; } = "TypeScript";
  public INamingConvention NamingConvention { get; set; } = new NoCase();
  public IJsonLibrary? JsonLibrary { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; } = new TypeTypeOption();
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

      return typestr + $"  ) {{}}{Environment.NewLine}}}{Environment.NewLine}{Environment.NewLine}";
    }

    if (jnc.TypeOption.UseRegularConstructor.GetValueOrDefault() == true)
    {
      typestr += ParseProperties(jnc.Kvps, 2, "", ";");
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

      typestr += $"    }}{Environment.NewLine}";
      return typestr + $"}}{Environment.NewLine}{Environment.NewLine}";
    }

    return ""; // Should not execute
  }

  private string ParseUnion(string[] rawUnion)
  {
    var union = "";

    for (int i = 0; i < rawUnion.Length; i++)
    {
      if (rawUnion[i] != "null")
      {
        union += i == rawUnion.Length - 1 ? $"{rawUnion[i]}" : $"{rawUnion[i]} | ";
      }
    }

    return union;
  }

  private string ParseProperties(List<JNodeKvp> jncKvps, int indent, string access, string endLine)
  {
    var typestr = "";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var kvp = jncKvps[i];
      var datatype = "";
      datatype = kvp.CollectionAs is not null ? kvp.Kvp.Value.Replace("[]", "") : kvp.Kvp.Value;
      var rawUnion = kvp.IsUnion ? datatype.Replace("(", "").Replace(")", "").Replace(" ", "").Split("|") : null;
      rawUnion = rawUnion is not null ? rawUnion.Where(x => x != "null").ToArray() : null;
      datatype = rawUnion is not null && rawUnion.Length > 1 ? ParseUnion(rawUnion) : datatype;
      datatype = rawUnion is not null && rawUnion.Length == 1 ? rawUnion[0] : datatype;
      datatype = kvp.Nested ? this.NamingConvention.Parse(datatype) : datatype;

      datatype = kvp.CollectionAs is not null ?
        ConfigureCollection(datatype, kvp.CollectionAs!, kvp.IsUnion, kvp.CollectionItemNullable!.Value, kvp.CollectionItemAllowUndefined!.Value) : datatype;

      datatype = kvp.Nullable ? $"{datatype} | null" : datatype;
      datatype = kvp.AllowUndefined ? $"{datatype} | undefined" : datatype;

      var propName = this.NamingConvention.Parse(kvp.Kvp.Key);
      var optional = kvp.Optional ? "?" : "";
      var indentation = new string(' ', indent);
      var propLine = $"{indentation}{access}{propName}{optional}: {datatype}{endLine}{Environment.NewLine}";
      typestr += propLine;
    }

    return typestr;
  }

  public string ConfigureCollection(string datatype, string collection, bool datatypeIsUnion, bool collectionItemNullable, bool collectionItemAllowUndefined)
  {
    var _collection = Enum.Parse(typeof(TypeScriptCollections), collection);

    datatype = collectionItemNullable ? $"{datatype} | null" : datatype;
    datatype = collectionItemAllowUndefined ? $"{datatype} | undefined" : $"{datatype}";
    datatype = collectionItemNullable || collectionItemAllowUndefined || datatypeIsUnion ? $"({datatype})" : datatype;

    switch (_collection)
    {
      case TypeScriptCollections.Set:
        datatype = $"Set<{datatype}>";
        break;
      case TypeScriptCollections.Array:
        datatype = $"{datatype}[]";
        break;
    }

    return datatype;
  }

  private string ParseInterfaceZodSchema(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);
    var zodstr = $"const {typename}Schema = z.object({{{Environment.NewLine}";
    zodstr += ParseZodProperties(jnc.Kvps);
    zodstr += $";{Environment.NewLine}{Environment.NewLine}";
    return zodstr;
  }

  private string ParseTypeZodSchema(JNodeClass jnc)
  {
    var zodstr = $"const {this.NamingConvention.Parse(jnc.Name)}Schema = z.object({{{Environment.NewLine}";
    zodstr += ParseZodProperties(jnc.Kvps);
    zodstr += $";{Environment.NewLine}{Environment.NewLine}";
    zodstr += $"type {this.NamingConvention.Parse(jnc.Name)} = z.infer<typeof {this.NamingConvention.Parse(jnc.Name)}Schema>;{Environment.NewLine}";
    zodstr += Environment.NewLine;
    return zodstr;
  }

  private string ParseClassZodSchema(JNodeClass jnc)
  {
    var typename = this.NamingConvention.Parse(jnc.Name);
    var zodstr = $"const {typename}Schema = z.object({{{Environment.NewLine}";
    zodstr += ParseZodProperties(jnc.Kvps);
    zodstr += $".transform(d => new {typename}(";

    for (int i = 0; i < jnc.Kvps.Count; i++)
    {
      var kvp = jnc.Kvps[i];
      zodstr += i != jnc.Kvps.Count - 1 ? $"d.{kvp.Kvp.Key}, " : $"d.{kvp.Kvp.Key}));{Environment.NewLine}";
    }

    zodstr += Environment.NewLine;
    return zodstr;
  }

  private string ParseZodUnion(string[] rawUnion)
  {
    var union = "z.union([";

    for (int i = 0; i < rawUnion.Length; i++)
    {
      union += i == rawUnion.Length - 1 ? $"z.{rawUnion[i]}()])" : $"z.{rawUnion[i]}(), ";
    }

    return union;
  }

  private string ParseZodProperties(List<JNodeKvp> jncKvps)
  {
    var zodstr = "";

    for (int i = 0; i < jncKvps.Count; i++)
    {
      var kvp = jncKvps[i];
      var rawDatatype = "";
      rawDatatype = kvp.CollectionAs is not null ? kvp.Kvp.Value.Replace("[]", "") : kvp.Kvp.Value;
      rawDatatype = kvp.CollectionItemNullable is not null ? rawDatatype.Replace("(", "").Replace(")", "") : rawDatatype;

      var rawUnion = kvp.IsUnion ? rawDatatype.Replace(" ", "").Split("|") : null;
      rawUnion = rawUnion is not null ? rawUnion.Where(x => x != "null").ToArray() : null;

      var datatype = "";
      datatype = rawUnion is not null && rawUnion.Length > 1 ? ParseZodUnion(rawUnion) : $"z.{rawDatatype}()";
      datatype = rawUnion is not null && rawUnion.Length == 1 ? $"z.{rawUnion[0]}()" : datatype;
      datatype = kvp.Nested ? $"{this.NamingConvention.Parse(rawDatatype)}Schema" : datatype;

      datatype = kvp.CollectionAs is not null ?
        ConfigureZodCollection(datatype, kvp.Nullable, kvp.CollectionAs, kvp.CollectionItemNullable!.Value, kvp.CollectionItemAllowUndefined!.Value) : datatype;

      datatype = kvp.Nullable && kvp.CollectionAs is null ? $"{datatype}.nullable()" : datatype;
      datatype = kvp.Optional || kvp.AllowUndefined ? $"{datatype}.optional()" : datatype;

      var propName = this.NamingConvention.Parse(kvp.Kvp.Key);
      var endLine = i == jncKvps.Count - 1 ? Environment.NewLine : $",{Environment.NewLine}";
      var propLine = $"  {propName}: {datatype}{endLine}";
      zodstr += propLine;
    }

    return zodstr + $"}})";
  }

  public string ConfigureZodCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable, bool collectionItemAllowUndefined)
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
    datatype = collectionItemAllowUndefined ? $"{datatype}.optional()" : datatype;
    datatype = datatypeNullable ? $"{datatype}).nullable()" : $"{datatype})";
    return datatype;
  }
}