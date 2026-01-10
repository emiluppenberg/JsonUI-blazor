using System.Text.RegularExpressions;

public enum CSharpCollections
{
  List, IEnumerable, ICollection, Array
}

public interface ILanguageOptions
{
  public INamingConvention NamingConvention { get; set; }
  public ICSharpJsonOptions? CSharpJsonOptions { get; set; }

  string GetClass(JNodeClass jnc);
  Array GetCollectionOptions();
  string ConfigureCollection(string datatype, string collectionAs);
}

public static class Language
{
  public static List<INamingConvention> GetNamingConventions()
  {
    return new List<INamingConvention> { new None(), new PascalCase() };
  }

  public static List<ICSharpJsonOptions> GetCSharpJsonOptions()
  {
    return new List<ICSharpJsonOptions> { new SystemTextJsonOption(), new NewtonsoftJsonOption() };
  }
}

public interface INamingConvention
{
  public string Name { get; }
  public string ToCSharp(string name);
}

public class None : INamingConvention
{
  public string Name => nameof(None);
  public string ToCSharp(string name) => name;
}

public class PascalCase : INamingConvention
{
  public string Name => nameof(PascalCase);

  public string ToCSharp(string name)
  {
    return Regex.Replace(name, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper());
  }
}