using System.Collections.ObjectModel;
using System.Diagnostics;
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
    var modelCurrentParents = new List<string>() { rootName };
    var arrayCurrentParents = new List<KeyValuePair<string, List<string>>>();
    string rootType = "";

    var isArray = false;
    var bytes = Encoding.UTF8.GetBytes(rawContent);
    var reader = new Utf8JsonReader(bytes);

    try
    {
      while (reader.Read())
      {
        if (reader.BytesConsumed == 1)
        {
          rootType = reader.TokenType == JsonTokenType.StartArray ? "[]" : "{}";
        }

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

        if (reader.TokenType == JsonTokenType.StartArray && !isArray)
        {
          var lineage = new List<string>(modelCurrentParents);

          arrayCurrentParents.Add(new(modelCurrentParents.Last(), lineage));
          isArray = true;
          continue;
        }

        if (reader.TokenType == JsonTokenType.EndArray && arrayCurrentParents.Count() > 0)
        {
          var currentArray = arrayCurrentParents.Last();

          if (currentArray.Value.Count > 1)
          {
            var currentArrayParentKey = currentArray.Value[currentArray.Value.Count - 2];
            var currentProperty = currentArray.Key;

            if (model.Keys.Count > 0)
            {
              for (int i = model.Keys.Count - 1; i >= 0; i--)
              {
                var key = model.Keys.ElementAt(i);
                var replacedKey = key.Replace("{}", "").Replace("[]", "");
                var arrayObjectKey = key + $"-{currentProperty}[]";

                if (replacedKey.EndsWith(currentArrayParentKey))
                {
                  if (!model[key].ContainsKey(currentProperty) && !model.ContainsKey(arrayObjectKey))
                  {
                    model[key].Add(currentProperty, "object[]");
                  }
                }
              }
            }
            else
            {
              model.Add($"{currentArrayParentKey}{rootType}", new() { [currentProperty] = "object[]" });
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

          var value = "object";

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";

            var parseString = reader.GetString();

            if (DateTime.TryParse(parseString, out _))
            {
              value = langOptions.Language == "C#" ? "DateTime" : "date";
            }
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

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new() { [currentProperty] = value });
          }
          else if (model[currentObject].ContainsKey(currentProperty))
          {
            if (model[currentObject][currentProperty] != value)
            {
              currentProperty += $"_{value}";
              model[currentObject].Add(currentProperty, value);
            }
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
          var value = "object";

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";

            var parseString = reader.GetString();

            if (DateTime.TryParse(parseString, out _))
            {
              value = langOptions.Language == "C#" ? "DateTime" : "date";
            }
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

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new() { [currentProperty] = value });
          }
          else if (model[currentObject].ContainsKey(currentProperty))
          {
            if (model[currentObject][currentProperty] != value)
            {
              currentProperty += $"_{value.Replace("[]", "")}";
              model[currentObject].Add(currentProperty, value);
            }
          }
          else
          {
            model[currentObject].Add(currentProperty, value);
          }
        }

      }

      var result = new List<JNode>();
      var childLookup = new Dictionary<string, List<JNode>>();
      childLookup.Add($"{rootName}{rootType}", new());

      foreach (var k in model.Keys)
      {
        var lineage = k.Split('-');

        var parentKey = String.Join("", lineage.Take(lineage.Length - 1));
        var lineageKey = parentKey + lineage.Last();

        var jNodeKvps = new List<JNodeKvp>();

        foreach (var kvp in model[k])
        {
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
            var split = k.Split('-');
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

  public static string JNodesToCode(List<JNode> jNodes, ILanguageOptions options)
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
          var newJnc = new JNodeClass(jn.Name, jnKvps);
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
              var newJnc = new JNodeClass(iteratingNode.Name, _jnKvps);
              classes.Add(iteratingNode.Name, newJnc);
            }
          }
        }
      }
    }

    var usingStr = options.CSharpJsonOptions is not null ? $"using {options.CSharpJsonOptions.Using};{Environment.NewLine}{Environment.NewLine}" : "";
    var cs = usingStr;

    foreach (var kvp in classes)
    {
      var jnc = kvp.Value;

      cs += options.ParseObject(kvp.Value);
    }

    return cs;
  }
}