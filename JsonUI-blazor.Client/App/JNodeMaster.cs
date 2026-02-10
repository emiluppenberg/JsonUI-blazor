using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static General;

public static class JNodeMaster
{
  public static List<JNode>? CreateFromJson(string rawContent, ILanguageOptions langOptions, string rootName)
  {
    var model = new Dictionary<string, Dictionary<string, string>>();
    var modelCurrentParents = new List<string>();
    var arrayCurrentParents = new List<KeyValuePair<string, List<string>>>();
    string rootType = "";

    var isArray = false;
    var bytes = Encoding.UTF8.GetBytes(rawContent);
    var reader = new Utf8JsonReader(bytes);

    var langNumber = langOptions.Language == "C#" ? "int" : "number";
    var langBoolean = langOptions.Language == "C#" ? "bool" : "boolean";
    var langDate = langOptions.Language == "C#" ? "DateTime" : "Date";
    var langNull = langOptions.Language == "C#" ? "object" : "null";

    try
    {
      while (reader.Read())
      {
        if (reader.BytesConsumed == 1)
        {
          rootType = reader.TokenType == JsonTokenType.StartArray ? "[]" : "{}";
          modelCurrentParents.Add($"{rootName}{rootType}");
        }

        if (reader.TokenType == JsonTokenType.EndObject && !isArray)
        {
          modelCurrentParents.RemoveAt(modelCurrentParents.Count() - 1);
          continue;
        }

        if (reader.TokenType == JsonTokenType.EndObject && isArray)
        {
          var currentArray = arrayCurrentParents.LastOrDefault();
          var currentArrayIndex = currentArray.Value.IndexOf($"{currentArray.Key}[]");

          if (currentArray.Value.Count - 1 > currentArrayIndex)
          {
            currentArray.Value.RemoveAt(currentArray.Value.Count - 1);
          }

          continue;
        }

        if (reader.TokenType == JsonTokenType.StartArray && !isArray)
        {
          var lineage = new List<string>(modelCurrentParents);
          var key = modelCurrentParents.Last()
          .Replace("[]", "").Replace("{}", "");

          arrayCurrentParents.Add(new(key, lineage));
          isArray = true;
          continue;
        }

        if (reader.TokenType == JsonTokenType.EndArray && arrayCurrentParents.Count() > 0)
        {
          var currentArray = arrayCurrentParents.Last();
          var currentProperty = currentArray.Key;
          var takeCount = currentArray.Value.Count - 1; // Parent of array is always the index before this - (.Take is not zero based)
          var parentKey = string.Join("^", currentArray.Value.Take(takeCount));
          var arrayKey = string.Join("^", currentArray.Value);

          // First check if current array is an object
          if (!model.TryGetValue(arrayKey, out var _))
          {
            // Else check if parent contains current array
            if (model.TryGetValue(parentKey, out var parent))
            {
              // If parent does not contain, JSON is an empty array
              if (!parent.ContainsKey(currentProperty))
              {
                var nullArr = langNull == "null" ? "unknown[]" : $"{langNull}[]"; // Guard for TypeScript
                parent.Add(currentProperty, nullArr);
              }
            }
          }

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
            var key = $"{currentProperty}[]";
            var lineage = new List<string>(!isArray ? modelCurrentParents : arrayCurrentParents.Last().Value) { key };
            arrayCurrentParents.Add(new(currentProperty, lineage));
            isArray = true;
            continue;
          }

          if (reader.TokenType == JsonTokenType.StartObject && !isArray)
          {
            var key = $"{currentProperty}{{}}";
            modelCurrentParents.Add(key);
            continue;
          }

          if (reader.TokenType == JsonTokenType.StartObject && isArray)
          {
            var key = $"{currentProperty}{{}}";
            arrayCurrentParents.LastOrDefault().Value.Add(key);
            continue;
          }

          var value = langNull;

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";

            var parseString = reader.GetString();

            if (DateTime.TryParse(parseString, out _))
            {
              value = langDate;
            }
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = langNumber;
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = langBoolean;
          }

          string currentObject = null!;

          if (!isArray)
          {
            currentObject = modelCurrentParents.Count() > 1 ?
            string.Join("^", modelCurrentParents) : modelCurrentParents.Last();
          }

          if (isArray)
          {
            currentObject = string.Join("^", arrayCurrentParents.Last().Value);
          }

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new() { [currentProperty] = value });
            continue;
          }
          else if (model[currentObject].ContainsKey(currentProperty))
          {
            if (!model[currentObject][currentProperty].Contains(value))
            {
              model[currentObject][currentProperty] += $" | {value}";
              continue;
            }
          }
          else
          {
            model[currentObject].Add(currentProperty, value);
            continue;
          }

          continue;
        }

        if (
          isArray && reader.TokenType == JsonTokenType.String ||
          isArray && reader.TokenType == JsonTokenType.Number ||
          isArray && reader.TokenType == JsonTokenType.True ||
          isArray && reader.TokenType == JsonTokenType.False ||
          isArray && reader.TokenType == JsonTokenType.Null)
        {
          var value = langNull;

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";

            var parseString = reader.GetString();

            if (DateTime.TryParse(parseString, out _))
            {
              value = langDate;
            }
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = langNumber;
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = langBoolean;
          }

          string? currentObject = null;
          var currentProperty = arrayCurrentParents.Last().Key;
          currentObject = string.Join("^", arrayCurrentParents.Last().Value);

          var lineage = currentObject.Split('^');
          currentObject = String.Join('^', lineage.Take(lineage.Length - 1));

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new() { [currentProperty] = $"{value}[]" });
            continue;
          }
          else if (model[currentObject].ContainsKey(currentProperty))
          {
            if (!model[currentObject][currentProperty].Contains(value))
            {
              var oldValueRaw = model[currentObject][currentProperty]
                .Replace("[]", "").Replace("(", "").Replace(")", "").Replace("unknown | ", "");
              model[currentObject][currentProperty] = $"({oldValueRaw} | {value})[]";
              continue;
            }
          }
          else
          {
            model[currentObject].Add(currentProperty, $"{value}[]");
            continue;
          }
        }
      }

      var result = new List<JNode>();
      var childLookup = new Dictionary<string, List<JNode>>();
      childLookup.Add($"{rootName}{rootType}", new());

      foreach (var k in model.Keys)
      {
        var lineage = k.Split('^');

        var parentKey = String.Join("", lineage.Take(lineage.Length - 1));
        var lineageKey = parentKey + lineage.Last();

        var jNodeKvps = new List<JNodeKvp>();

        foreach (var kvp in model[k])
        {
          // Handle C# unions
          if (kvp.Value.Contains("|") && langOptions.Language == "C#")
          {
            var arraySuffix = kvp.Value.Contains("[]") ? "[]" : "";
            var values = kvp.Value.Split('|');
            var isNullable = kvp.Value.Contains("object");

            // When the union is only (datatype | null), just assume the datatype is optional
            if (values.Length == 2 && isNullable)
            {
              var rawValue = values.First(x => !x.Contains("object"))!;
              rawValue = rawValue.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("[]", "");
              var newValue = $"{rawValue}{arraySuffix}";
              var newKvp = new KeyValuePair<string, string>(kvp.Key, newValue);
              var new_jNodeKvp = new JNodeKvp(newKvp, langOptions);
              new_jNodeKvp.Nullable = isNullable;
              new_jNodeKvp.DataNullable = true;
              jNodeKvps.Add(new_jNodeKvp);
              continue;
            }

            foreach (var v in values)
            {
              var rawValue = v.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("[]", "");
              var newKey = $"{kvp.Key}_{rawValue}";
              newKey = langOptions.NamingConvention.Parse(newKey);
              var newValue = $"{rawValue}{arraySuffix}";
              var newKvp = new KeyValuePair<string, string>(newKey, newValue);
              var new_jNodeKvp = new JNodeKvp(newKvp, langOptions);
              new_jNodeKvp.MapFrom = kvp.Key;
              new_jNodeKvp.Nullable = isNullable;
              new_jNodeKvp.DataNullable = isNullable ? true : false;
              jNodeKvps.Add(new_jNodeKvp);
            }

            continue;
          }

          var jNodeKvp = new JNodeKvp(kvp, langOptions);
          jNodeKvps.Add(jNodeKvp);
        }

        var jNode = new JNode(lineageKey, parentKey, lineage.Last(), jNodeKvps, langOptions);

        if (!childLookup.TryGetValue(lineageKey, out var _))
        {
          childLookup.Add(lineageKey, new());
        }
        if (!childLookup.TryGetValue(parentKey, out var _))
        {
          childLookup.Add(parentKey, new() { jNode });
        }
        else if (childLookup.TryGetValue(parentKey, out var parent))
        {
          parent.Add(jNode);
        }

        result.Add(jNode);
      }

      for (int i = 0; i < result.Count; i++)
      {
        var item = result[i];
        item.Parent = result.FirstOrDefault(x => x.LineageKey == item.ParentKey);

        if (item.Parent is null && item.ParentKey.Length > rootName.Length + 2)
        {
          string parentParentKey = "";
          string parentName = "";

          foreach (var k in model.Keys)
          {
            var split = k.Split('^');
            var join = String.Join("", split);

            if (join == item.LineageKey)
            {
              parentParentKey = String.Join("", split.Take(split.Length - 2));
              parentName = split.ElementAt(split.Length - 2);
              break;
            }
          }

          var jNode = new JNode(item.ParentKey, parentParentKey, parentName, new(), langOptions);
          item.Parent = jNode;
          jNode.Children.Add(item);
          result.Add(jNode);
        }

        if (item.Parent is not null && !item.Parent.Children.Contains(item))
        {
          item?.Parent?.Children.Add(item);
        }

        var unaddedChildren = childLookup[item.LineageKey].Where(x => !item.Children.Contains(x)).ToList();
        item.Children.AddRange(unaddedChildren);
      }

      return result;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"// ERROR{Environment.NewLine} - {ex.GetBaseException().Message}{Environment.NewLine}// STACKTRACE{Environment.NewLine} - {ex.StackTrace}");
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

  public static string JNodesToCode(List<JNode> jNodes, ILanguageOptions langOptions)
  {
    var classes = new Dictionary<string, JNodeClass>();

    foreach (var jn in jNodes)
    {
      var jnKvps = jn.KeyValues
        .Where(x => x.IsSelected)
        .ToList();

      if (jnKvps.Count > 0)
      {
        if (classes.TryGetValue(jn.Name, out var jnc))
        {
          foreach (var jnk in jnKvps)
          {
            // TODO - Handle case when same key has been selected multiple times and some has a null value 
            if (!jnc.Kvps.Any(x => x.Kvp.Key == jnk.Kvp.Key))
            {
              jnc.Kvps.Add(jnk);
            }
          }
        }

        if (!classes.TryGetValue(jn.Name, out var _))
        {
          var newJnc = new JNodeClass(jn, jnKvps);
          classes.Add(jn.Name, newJnc);
        }

        if (jn.Parent is not null)
        {
          var iteratingNode = jn;

          while (iteratingNode.Parent is not null)
          {
            var previousNode = iteratingNode;
            iteratingNode = iteratingNode.Parent;

            if (classes.TryGetValue(iteratingNode.Name, out var parentJnc))
            {
              if (!parentJnc.Kvps.Any(x => x.Kvp.Key == previousNode.Name))
              {
                parentJnc.Kvps.Add(new JNodeKvp(previousNode));
              }
            }

            if (!classes.TryGetValue(iteratingNode.Name, out var _))
            {
              var _jnKvps = new List<JNodeKvp>() { new JNodeKvp(previousNode) };
              var newJnc = new JNodeClass(iteratingNode, _jnKvps);
              classes.Add(iteratingNode.Name, newJnc);
            }
          }
        }
      }
    }

    var usingStr = langOptions.JsonLibrary is not null ? $"using {langOptions.JsonLibrary.Using};{Environment.NewLine}{Environment.NewLine}" : "";
    var zodStr = classes.Any(x => x.Value.TypeOption is not null && x.Value.TypeOption.UseZodSchema == true) ? $"import * as z from \"zod\";{Environment.NewLine}{Environment.NewLine}" : "";
    var cs = usingStr + zodStr;

    foreach (var kvp in classes)
    {
      var jnc = kvp.Value;

      cs += langOptions.ParseObject(kvp.Value);
    }

    return cs;
  }
}