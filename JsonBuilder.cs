using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public enum JNodeValue
{
  String, Number, Boolean, Null, Object, Array
}

public class JNode(string lineageKey, string name, List<KeyValuePair<string, JNodeValue>> keyValues)
{
  public string LineageKey { get; } = lineageKey;
  public string Name { get; } = name;
  public List<KeyValuePair<string, JNodeValue>> KeyValues { get; } = keyValues;
  public List<JNode> Children { get; set; } = new();
  public bool IsExpanded { get; set; }
}

public class JsonBuilder
{
  private List<string> modelCurrentParents = new() { "Base" };
  private List<KeyValuePair<string, List<string>>> arrayCurrentParents = new();
  private Dictionary<string, Dictionary<string, JNodeValue>> model = new();
  private string debObj = "";
  private string debProp = "";

  public List<JNode>? CreateFromJson(string rawContent)
  {
    model = new();
    modelCurrentParents = new() { "Base" };
    arrayCurrentParents = new();

    debObj = "";
    debProp = "";

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
          string? currentProperty = Regex.Replace(reader.GetString()!, @"(?:^|_)([a-z])",
            match => match.Groups[1].Value.ToUpper());

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

          JNodeValue value = JNodeValue.Null;

          if (reader.TokenType == JsonTokenType.String)
          {
            value = JNodeValue.String;
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = JNodeValue.Number;
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = JNodeValue.Boolean;
          }

          string currentObject = "";

          if (!isArray)
          {
            currentObject = modelCurrentParents.Count() > 1 ?
              string.Join("{}-", modelCurrentParents) + currentObject + "{}" :
              modelCurrentParents.Last() + "{}";
          }

          if (isArray)
          {
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
                continue;
              }
            }
          }

          debObj = currentObject;
          debProp = currentProperty;

          if (!model.ContainsKey(currentObject))
          {
            model.Add(currentObject, new Dictionary<string, JNodeValue> { [currentProperty] = value });
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

        var kvps = new List<KeyValuePair<string, JNodeValue>>();

        foreach (var kvp in model[k])
        {
          kvps.Add(kvp);
        }

        var jNode = new JNode(lineageKey, lineage.Last(), kvps);

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
      Console.WriteLine($"ERROR: {debObj}.{debProp}");

      return null;
    }
  }
}