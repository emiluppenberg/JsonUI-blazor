using System.Text.RegularExpressions;

public enum CSharpCollections
{
  List, IEnumerable, ICollection, Array
}

public enum TypeScriptCollections
{
  Array, Set
}

public interface ILanguageOptions
{
  public string Language { get; }
  public bool? UseRaw { get; set; }
  public INamingConvention NamingConvention { get; set; }
  public ICSharpJsonOption? CSharpJsonOptions { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; }

  string ParseObject(JNodeClass jnc);
  Array GetCollectionOptions();
  string ConfigureCollection(string datatype, bool datatypeNullable, string collection, bool collectionItemNullable);
}

public static class Language
{
  public static List<INamingConvention> GetNamingConventions()
  {
    return new List<INamingConvention> { new AsIsCase(), new LowerSnakeCase(), new UpperSnakeCase(), new PascalCase() };
  }

  public static List<ICSharpJsonOption> GetCSharpJsonOptions()
  {
    return new List<ICSharpJsonOption> { new SystemTextJsonOption(), new NewtonsoftJsonOption() };
  }

  public static List<ITypeScriptTypeOption> GetTypeOptions()
  {
    return new List<ITypeScriptTypeOption> { new InterfaceTypeOption(), new TypeTypeOption(), new ClassTypeOption() };
  }
}

public interface INamingConvention
{
  public string Name { get; }
  public string Parse(string name);
}

public class AsIsCase : INamingConvention
{
  public string Name => "No naming convention";
  public string Parse(string name) => name;
}

public class LowerSnakeCase : INamingConvention
{
  public string Name => nameof(LowerSnakeCase);

  public string Parse(string name)
  {
    var parsed = Regex.Replace(
      name,
      @"(?<!^)(?=[A-Z])|[_\-\s\.\/:]+",
      "_"
    ).ToLowerInvariant();

    return parsed.Replace("__", "_");
  }
}

public class UpperSnakeCase : INamingConvention
{
  public string Name => nameof(UpperSnakeCase);

  public string Parse(string name)
  {
    var parsed = Regex.Replace(
      name,
      @"(?<!^)(?=[A-Z])|[_\-\s\.\/:]+",
      "_"
    ).ToUpperInvariant();

    return parsed.Replace("__", "_");
  }
}

public class PascalCase : INamingConvention
{
  public string Name => nameof(PascalCase);

  public string Parse(string name)
  {
    // return Regex.Replace(name, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper());
    return Regex.Replace(
      name.ToLowerInvariant(),
      @"(?:^|[_\-\s\.\/:])(\p{Ll})",
      m => m.Groups[1].Value.ToUpperInvariant()
    );
  }
}