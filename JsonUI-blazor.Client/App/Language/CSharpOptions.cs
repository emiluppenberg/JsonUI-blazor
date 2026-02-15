public interface IJsonLibrary
{
  public string Name { get; }
  public string Using { get; }
  public string NameAnnotation { get; }
  public Dictionary<string, List<string>> Annotations { get; }
}

public class SystemTextJsonLibrary : IJsonLibrary
{
  public string Name => "System.Text.Json";
  public string Using => "System.Text.Json.Serialization";
  public string NameAnnotation => "JsonPropertyName";
  public Dictionary<string, List<string>> Annotations => new()
  {
    ["JsonPropertyName"] = new()
    {
      "Default"
    },

    ["JsonIgnore"] = new()
    {
      "Default",
      "Condition = JsonIgnoreCondition.Always",
      "Condition = JsonIgnoreCondition.WhenWritingNull",
      "Condition = JsonIgnoreCondition.WhenWritingDefault",
      "Condition = JsonIgnoreCondition.WhenWriting"
    },

    ["JsonInclude"] = new()
    {
      "Default"
    },

    ["JsonConverter"] = new()
    {
      "typeof(YourConverter)"
    },

    ["JsonNumberHandling"] = new()
    {
      "JsonNumberHandling.Strict",
      "JsonNumberHandling.AllowReadingFromString",
      "JsonNumberHandling.WriteAsString",
      "JsonNumberHandling.AllowNamedFloatingPointLiterals"
    },

    ["JsonPropertyOrder"] = new()
    {
      "YourNumber"
    },

    ["JsonRequired"] = new()
    {
      "Default"
    },

    ["JsonExtensionData"] = new()
    {
      "Default"
    },

    // ["JsonDerivedType"] = new()
    // {
    //   @"typeof(DerivedType), ""discriminator"""
    // },

    // ["JsonPolymorphic"] = new()
    // {
    //   @"TypeDiscriminatorPropertyName = ""$type"""
    // }
  };
}

public class NewtonsoftJsonLibrary : IJsonLibrary
{
  public string Name => "Newtonsoft.Json";
  public string Using => "Newtonsoft.Json";
  public string NameAnnotation => "JsonProperty";
  public Dictionary<string, List<string>> Annotations => new()
  {
    ["JsonProperty"] = new()
    {
      "Default",
      "Required = Required.Always",
      "Required = Required.DisallowNull",
      "Required = Required.AllowNull",
      "Required = Required.Default",
      "NullValueHandling = NullValueHandling.Ignore",
      "NullValueHandling = NullValueHandling.Include",
      "DefaultValueHandling = DefaultValueHandling.Ignore",
      "DefaultValueHandling = DefaultValueHandling.Include",
      "DefaultValueHandling = DefaultValueHandling.Populate",
      "DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate",
      "Order = 0"
    },

    ["JsonIgnore"] = new()
    {
      "Default"
    },

    ["JsonRequired"] = new()
    {
      "Default"
    },

    ["JsonConverter"] = new()
    {
      "typeof(YourConverter)"
    },

    ["JsonExtensionData"] = new()
    {
      "Default"
    },

    ["JsonObject"] = new()
    {
      "MemberSerialization.OptIn",
      "MemberSerialization.OptOut",
      "MemberSerialization.Fields"
    },

    ["JsonConstructor"] = new()
    {
      "Default"
    },

    ["DefaultValue"] = new()
    {
      "YourValue",
    }
  };
}


public class CSharpOptions : ILanguageOptions
{
  public string Language { get; } = "C#";
  public INamingConvention NamingConvention { get; set; } = new NoCase();
  public IJsonLibrary? JsonLibrary { get; set; } = new SystemTextJsonLibrary();
  public ITypeScriptTypeOption? TypeOption { get; set; }

  public Array GetCollectionOptions() => Enum.GetValues<CSharpCollections>();

  public string ParseObject(JNodeClass jnc)
  {
    var className = this.NamingConvention.Parse(jnc.Name);
    var classStr = $"public class {className}{Environment.NewLine}{{{Environment.NewLine}";
    var propertyStrings = new List<string>();

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
      var propNamePlural = kvp.CollectionAs is not null && propName.EndsWith("s") ? "es" : "s";
      propName = kvp.CollectionAs is not null ? $"{propName}{propNamePlural}" : propName;

      var fieldName = this.NamingConvention.ParseField(propName);
      var field = kvp.HasField ? $"  private {datatype} {fieldName};{Environment.NewLine}" : null;
      var startBracket = kvp.HasField ? $"{Environment.NewLine}  {{{Environment.NewLine}" : $"{{ ";
      var endBracket = kvp.HasField ? $"  }}" : $"}}";
      var access = kvp.IsPrivate ? "private" : "public";
      var get = kvp.HasGet ? "get; " : "";
      var set = kvp.HasSet ? "set; " : "";
      get = kvp.HasField && kvp.HasGet ? $"    get {{ return {fieldName}; }}{Environment.NewLine}" : get;
      set = kvp.HasField && kvp.HasSet ? $"    set {{ {fieldName} = value; }}{Environment.NewLine}" : set;

      if (kvp.CollectionAs is not null && !kvp.JsonLibraryAnnotations!.TryGetValue(this.JsonLibrary!.NameAnnotation, out _))
      {
        kvp.JsonLibraryAnnotations.Add(this.JsonLibrary!.NameAnnotation, new() { "Default" });
      }

      var jsonAnnotation = ParseAnnotations(kvp);
      var propLine = $"{field}{jsonAnnotation}  {access} {datatype} {propName} {startBracket}{get}{set}{endBracket}{Environment.NewLine}";
      propertyStrings.Add(propLine);
    }

    var oneliners = propertyStrings.Where(x => x.Count(x => x == '\n' || x == '\r') < 2).ToList();
    var multiliners = propertyStrings.Where(x => x.Count(x => x == '\n' || x == '\r') > 1).ToList();
    classStr += String.Join($"", oneliners);
    classStr += oneliners.Count() > 0 && multiliners.Count() > 0 ? Environment.NewLine : "";
    classStr += multiliners.Count() > 0 ? String.Join(Environment.NewLine, multiliners) : "";
    return classStr + $"}}{Environment.NewLine}{Environment.NewLine}";
  }

  private string ParseAnnotations(JNodeKvp kvp)
  {
    if (kvp.JsonLibraryAnnotations!.Count == 0)
    {
      if (kvp.MapFrom is not null)
      {
        kvp.JsonLibraryAnnotations.Add(this.JsonLibrary!.NameAnnotation, new() { "Default" });
      }
      else
      {
        return "";
      }
    }

    var mapFrom = kvp.MapFrom is not null ? kvp.MapFrom : kvp.Kvp.Key;
    var jsonAnnotation = "";

    foreach (var annotationKvp in kvp.JsonLibraryAnnotations)
    {
      if (annotationKvp.Key == this.JsonLibrary!.NameAnnotation)
      {
        jsonAnnotation += $"  [{this.JsonLibrary!.NameAnnotation}(\"{mapFrom}\")]{Environment.NewLine}";
        continue;
      }

      var annotations = annotationKvp.Value!;
      var hasDefault = annotationKvp.Value.Contains("Default"); // Default cannot be selected together with other options
      jsonAnnotation += hasDefault ?
      $"  [{annotationKvp.Key}]{Environment.NewLine}" : $"  [{annotationKvp.Key}({annotations[0]})]{Environment.NewLine}";
    }

    return jsonAnnotation;
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
