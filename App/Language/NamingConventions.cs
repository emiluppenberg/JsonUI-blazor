using System.Text.RegularExpressions;

public interface INamingConvention
{
  string GetName(string name);
}

public class PascalCase : INamingConvention
{
  public string GetName(string name)
  {
    return Regex.Replace(name, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper());
  }
}