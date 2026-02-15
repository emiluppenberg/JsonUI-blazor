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
  public INamingConvention NamingConvention { get; set; }
  public IJsonLibrary? JsonLibrary { get; set; }
  public ITypeScriptTypeOption? TypeOption { get; set; }

  string ParseObject(JNodeClass jnc);
  Array GetCollectionOptions();
}

public static class Language
{
  public static List<INamingConvention> GetNamingConventions()
  {
    return new List<INamingConvention> { new NoCase(), new LowerSnakeCase(), new UpperSnakeCase(), new PascalCase() };
  }

  public static List<IJsonLibrary> GetJsonLibraries()
  {
    return new List<IJsonLibrary> { new SystemTextJsonLibrary(), new NewtonsoftJsonLibrary() };
  }

  public static List<ITypeScriptTypeOption> GetTypeOptions()
  {
    return new List<ITypeScriptTypeOption> { new TypeTypeOption(), new InterfaceTypeOption(), new ClassTypeOption() };
  }
}

public interface INamingConvention
{
  public string Name { get; }
  public string Parse(string name);
  public string ParseField(string name);
}

public class NoCase : INamingConvention
{
  public string Name => "No naming convention";
  public string Parse(string name) => name;
  public string ParseField(string name) => $"_{name}";
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

  public string ParseField(string name) => $"_{name}";
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

  public string ParseField(string name) => $"_{name}";
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

  public string ParseField(string name) => "_" + name.Substring(0, 1).ToLowerInvariant() + name.Substring(1, name.Length - 1);
}