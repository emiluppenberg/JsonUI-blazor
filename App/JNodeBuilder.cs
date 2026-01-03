using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static General;

public static class JNodeBuilder
{
  public static List<JNode>? CreateFromJson(string rawContent, CSharpOptions options)
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

        var jNode = new JNode(lineageKey, lineage.Last(), jNodeKvps, result.FirstOrDefault(x => x.LineageKey == parentKey), options);

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