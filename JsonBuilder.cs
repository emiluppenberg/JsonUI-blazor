using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public enum JNodeType
{
  Object, Array
}

public record JNodeKvp(KeyValuePair<string, string> kvp)
{
  public KeyValuePair<string, string> Kvp { get; set; } = kvp;
  public bool IsSelected { get; set; }
}

public class JNode
{
  public JNodeType Type;
  public string LineageKey { get; }
  public string Name { get; }
  public List<JNodeKvp> KeyValues { get; }
  public List<JNode> Children { get; set; }
  public JNode? Parent { get; set; }
  public bool IsExpanded { get; set; }

  public JNode(string lineageKey, string name, List<JNodeKvp> keyValues, JNode? parent)
  {
    Name = name.Replace("{}", null).Replace("[]", null);
    Type = name.Contains("{}") ? JNodeType.Object : JNodeType.Array;
    LineageKey = lineageKey;
    KeyValues = keyValues;
    Children = new();
    Parent = parent;
  }
}

public record JNodeClass(string name, List<KeyValuePair<string, string>> kvps)
{
  public string Name { get; set; } = name;
  public List<KeyValuePair<string, string>> Kvps { get; } = kvps;

  public string GetClassName(CSharpOptions options)
  {
    var name = this.Name;
    name = options.UsePascalCase ?
      Regex.Replace(name, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper()) :
      name.ToLower();

    return $"public class {name} {Environment.NewLine}" +
       $"{{{Environment.NewLine}";
  }

  public string GetProperty(int i, CSharpOptions options)
  {
    var datatype = this.Kvps[i].Value;
    var propName = this.Kvps[i].Key;
    var jsonAnnotation = $"  [JsonProperty(\"{propName}\")]{Environment.NewLine}";

    var isNestedObject = datatype.Replace("[]", "") == propName;
    var isArray = datatype.Contains("[]");

    propName = options.UsePascalCase ?
      Regex.Replace(propName, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper()) :
      this.Kvps[i].Key.ToLower();

    datatype = isNestedObject ?
      propName :
      datatype;

    datatype = isArray ? this.ConfigureCollection(datatype, options) : datatype;

    var propLine = $"  public {datatype} {propName} {{ get; set; }}{Environment.NewLine}";
    var newLine = options.UsePascalCase && i != this.Kvps.Count - 1 ? Environment.NewLine : "";

    return options.UsePascalCase ?
      jsonAnnotation + propLine + newLine :
      propLine;
  }

  private string ConfigureCollection(string arrayType, CSharpOptions options)
  {
    var isPrimitive = arrayType.Contains("[]");
    var collection = isPrimitive ? options.PrimitiveArrayAs : options.ObjectArrayAs;
    arrayType = isPrimitive ? arrayType.Replace("[]", "") : arrayType;

    switch (collection)
    {
      case CollectionAs.List:
        arrayType = $"List<{arrayType}>";
        break;
      case CollectionAs.IEnumerable:
        arrayType = $"IEnumerable<{arrayType}>";
        break;
      case CollectionAs.ICollection:
        arrayType = $"ICollection<{arrayType}>";
        break;
      case CollectionAs.Array:
        arrayType = $"{arrayType}[]";
        break;
    }

    return arrayType;
  }
}

public enum CollectionAs
{
  List, IEnumerable, ICollection, Array
}

public struct CSharpOptions()
{
  public bool UsePascalCase { get; set; }
  public CollectionAs PrimitiveArrayAs { get; set; }
  public CollectionAs ObjectArrayAs { get; set; }
}

public static class JNodeBuilder
{
  public static List<JNode>? CreateFromJson(string rawContent)
  {
    var model = new Dictionary<string, Dictionary<string, string>>();
    var modelCurrentParents = new List<string>() { "Base" };
    var arrayCurrentParents = new List<KeyValuePair<string, List<string>>>();

    var isArray = false;
    var bytes = Encoding.UTF8.GetBytes(rawContent);
    var reader = new Utf8JsonReader(bytes);

    try
    {
      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject && !isArray)
        {
          modelCurrentParents.RemoveAt(modelCurrentParents.Count() - 1);
          continue;
        }

        if (reader.TokenType == JsonTokenType.EndObject && isArray)
        {
          var last = arrayCurrentParents.LastOrDefault();
          var lastLength = last.Value.Count() - 1;

          if (lastLength > last.Value.IndexOf(last.Key))
          {
            last.Value.RemoveAt(lastLength);
          }

          continue;
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
          arrayCurrentParents.RemoveAt(arrayCurrentParents.Count() - 1);

          if (arrayCurrentParents.Count() == 0)
          {
            isArray = false;
          }

          continue;
        }

        if (reader.TokenType == JsonTokenType.PropertyName)
        {
          var currentProperty = reader.GetString()!;

          reader.Read();

          if (reader.TokenType == JsonTokenType.StartArray)
          {
            var lineage = new List<string>(!isArray ? modelCurrentParents : arrayCurrentParents.Last().Value) { currentProperty };

            arrayCurrentParents.Add(new(currentProperty, lineage));
            isArray = true;
            continue;
          }

          if (reader.TokenType == JsonTokenType.StartObject && !isArray)
          {
            modelCurrentParents.Add(currentProperty);
            continue;
          }

          if (reader.TokenType == JsonTokenType.StartObject && isArray)
          {
            arrayCurrentParents.LastOrDefault().Value.Add(currentProperty);
            continue;
          }

          var value = "null";

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = "int";
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = "bool";
          }

          string? currentObject = null;

          if (!isArray)
          {
            currentObject = modelCurrentParents.Count() > 1 ?
              string.Join("{}-", modelCurrentParents) + "{}" :
              modelCurrentParents.Last() + "{}";
          }

          if (isArray)
          {
            currentObject = BuildArrayObjectString(model, arrayCurrentParents, currentProperty);
          }

          if (string.IsNullOrEmpty(currentObject))
          {
            continue;
          }

          Console.WriteLine($"{currentObject}.{currentProperty}");

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new Dictionary<string, string> { [currentProperty] = value });
          }
          else
          {
            model[currentObject].Add(currentProperty, value);
          }

          continue;
        }

        if (
          isArray && reader.TokenType == JsonTokenType.String ||
          isArray && reader.TokenType == JsonTokenType.Number ||
          isArray && reader.TokenType == JsonTokenType.True ||
          isArray && reader.TokenType == JsonTokenType.False)
        {
          var value = "null";

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = "int";
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = "bool";
          }

          string? currentObject = null;
          var currentProperty = arrayCurrentParents.Last().Key;

          currentObject = BuildArrayObjectString(model, arrayCurrentParents, currentProperty);

          if (string.IsNullOrEmpty(currentObject))
          {
            continue;
          }

          var lineage = currentObject.Split('-');
          currentObject = String.Join('-', lineage.Take(lineage.Length - 1));
          value += "[]";

          Console.WriteLine($"primitive: {currentObject}.{currentProperty}");

          if (model[currentObject].ContainsKey(currentProperty))
          {
            continue;
          }
          else
          {
            model[currentObject].Add(currentProperty, value);
          }
        }

      }

      var result = new List<JNode>();
      var childLookup = new Dictionary<string, List<JNode>>();

      foreach (var k in model.Keys)
      {
        var lineage = k.Split('-');

        var parentKey = String.Join("", lineage.Take(lineage.Length - 1));
        var lineageKey = parentKey + lineage.Last();

        var jNodeKvps = new List<JNodeKvp>();

        foreach (var kvp in model[k])
        {
          var jNodeKvp = new JNodeKvp(kvp);
          jNodeKvps.Add(jNodeKvp);
        }

        var jNode = new JNode(lineageKey, lineage.Last(), jNodeKvps, result.FirstOrDefault(x => x.LineageKey == parentKey));

        childLookup.Add(lineageKey, new());

        if (childLookup.TryGetValue(parentKey, out var parent))
        {
          parent.Add(jNode);
        }

        result.Add(jNode);
      }

      result.ForEach(x => x.Children = childLookup[x.LineageKey]);
      return result;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"// BASE - {ex.GetBaseException().Message} // INNER - {ex.InnerException?.Message} // SOURCE - {ex.Source} // STACKTRACE - {ex.StackTrace} // TARGETSITE - {ex.TargetSite}");
      return null;
    }
  }

  private static string? BuildArrayObjectString(
    Dictionary<string, Dictionary<string, string>> model,
    List<KeyValuePair<string, List<string>>> arrayCurrentParents,
    string currentProperty
  )
  {
    var currentObject = "";
    var nestedArrayIndex = arrayCurrentParents.Count() - 1;
    var currentLevel = arrayCurrentParents[nestedArrayIndex];
    var currentObjectIndex = arrayCurrentParents[nestedArrayIndex].Value.Count() - 1;
    var currentArrayLineageIndex = currentLevel.Value.IndexOf(currentLevel.Key);
    var lineageDiff = currentObjectIndex - currentArrayLineageIndex;
    var isNestedObject = currentObjectIndex > currentLevel.Value.IndexOf(currentLevel.Key);

    var arrayKeys = arrayCurrentParents.Select(x => { return x.Key; }).ToArray();

    for (int i = 0; i <= currentObjectIndex; i++)
    {
      var lineageName = arrayKeys.Any(x => x == currentLevel.Value[i]) ?
        currentLevel.Value[i] + "[]" :
        currentLevel.Value[i] + "{}";

      currentObject += (i < currentObjectIndex) ?
        lineageName + "-" :
        lineageName;
    }

    if (model.ContainsKey(currentObject))
    {
      if (model[currentObject].ContainsKey(currentProperty))
      {
        return null;
      }
    }

    return currentObject;
  }

  public static string JNodesToCSharp(List<JNode> jNodes, CSharpOptions options)
  {
    try
    {
      var classes = new Dictionary<string, JNodeClass>();

      foreach (var jn in jNodes)
      {
        var kvps = jn.KeyValues
          .Where(x => x.IsSelected)
          .Select(x => x.Kvp)
          .ToList();

        if (kvps.Count > 0)
        {
          // for (int i = 0; i < kvps.Count; i++)
          // {
          //   if (kvps[i].Value.Contains("[]"))
          //   {
          //     kvps[i] = new KeyValuePair<string, string>(kvps[i].Key, ConfigureCollection(kvps[i].Value, options));
          //   }
          // }

          if (classes.TryGetValue(jn.Name, out var jnc))
          {
            foreach (var kvp in kvps)
            {
              // TODO - Handle case when same key has been selected multiple times and some has a null value 
              if (!jnc.Kvps.Any(x => x.Key == kvp.Key))
              {
                jnc.Kvps.Add(kvp);
              }
            }
          }

          if (!classes.TryGetValue(jn.Name, out var _))
          {
            var newJnc = new JNodeClass(jn.Name, kvps);
            classes.Add(jn.Name, newJnc);
          }

          if (jn.Parent is not null)
          {
            var iteratingNode = jn;

            while (iteratingNode.Parent is not null)
            {
              var previousNode = iteratingNode;
              iteratingNode = iteratingNode.Parent;

              var dataType = previousNode.Type == JNodeType.Array ? $"{previousNode.Name}[]" : $"{previousNode.Name}";

              if (classes.TryGetValue(iteratingNode.Name, out var parentJnc))
              {
                if (!parentJnc.Kvps.Any(x => x.Key == $"{previousNode.Name}" && x.Value == dataType))
                {
                  parentJnc.Kvps.Add(new KeyValuePair<string, string>($"{previousNode.Name}", dataType));
                }
              }

              if (!classes.TryGetValue(iteratingNode.Name, out var _))
              {
                var _kvps = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>($"{previousNode.Name}", dataType) };
                var newJnc = new JNodeClass(iteratingNode.Name, _kvps);
                classes.Add(iteratingNode.Name, newJnc);
              }
            }
          }
        }
      }

      var cs = "";

      foreach (var kvp in classes)
      {
        var jnc = kvp.Value;

        var code = jnc.GetClassName(options);

        for (int i = 0; i < jnc.Kvps.Count; i++)
        {
          code += jnc.GetProperty(i, options);
        }

        cs += code + $"}}{Environment.NewLine}{Environment.NewLine}";
      }

      return cs;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"// BASE - {ex.GetBaseException().Message} // INNER - {ex.InnerException?.Message} // SOURCE - {ex.Source} // STACKTRACE - {ex.StackTrace} // TARGETSITE - {ex.TargetSite}");
      return "";
    }
  }
}