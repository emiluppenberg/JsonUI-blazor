public interface ITypeOptions
{
  public string Name { get; }
  public bool UseRawType { get; set; }
}

public class InterfaceTypeOption : ITypeOptions
{
  public string Name { get; } = "Interface";
  public bool UseRawType { get; set; }
}

public class TypeScriptOptions : ILanguageOptions
{
  public string Language { get; } = "TypeScript";

  public INamingConvention NamingConvention { get; set; } = new LowerSnakeCase();

  public ICSharpJsonOptions? CSharpJsonOptions { get; set; }

  public ITypeOptions? TypeOptions { get; set; }

  public string ConfigureCollection(string datatype, string collectionAs)
  {
    throw new NotImplementedException();
  }

  public string ParseObject(JNodeClass jnc)
  {
    throw new NotImplementedException();
  }

  public Array GetCollectionOptions() => Enum.GetValues<TypeScriptCollections>();
}