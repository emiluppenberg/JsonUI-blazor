public class JsonOptionsCSharp
{
  public INamingConvention NamingConvention { get; set; }
  public bool UseSerializerOptions { get; set; }
}

public class CSharpOptions : ILanguageOptions
{
  public JsonOptionsCSharp? JsonOptions { get; set; }

  public string ConfigureCollection(string datatype, Enum collectionAs)
  {
    throw new NotImplementedException();
  }

  public Array GetCollectionOptions()
  {
    if (JsonOptions.)
      throw new NotImplementedException();
  }
}
