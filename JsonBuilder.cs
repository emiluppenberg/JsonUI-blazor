using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class JsonBuilder
{
  private List<string> modelCurrentParents = new() { "Base" };
  private List<KeyValuePair<string, List<string>>> arrayCurrentParents = new();
  private Dictionary<string, Dictionary<string, string>> model = new();
  private string debObj = "";
  private string debProp = "";

  [DebuggerStepThrough]
  public Dictionary<string, Dictionary<string, string>>? CreateFromJson(string rawContent)
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

          string value = "";

          if (reader.TokenType == JsonTokenType.String)
          {
            value = "string";
          }

          if (reader.TokenType == JsonTokenType.Number)
          {
            value = "number";
          }

          if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
          {
            value = "boolean";
          }

          if (reader.TokenType == JsonTokenType.Null)
          {
            value = "null";
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
            model.Add(currentObject, new Dictionary<string, string> { [currentProperty] = value });
          }
          else
          {
            model[currentObject].Add(currentProperty, value);
          }
        }
      }

      foreach (var k in model.Keys)
      {
        foreach (var kvp in model[k])
        {
          Debug.WriteLine($"{k}.{kvp.Key} - {kvp.Value}");
        }
      }

      return model;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"// BASE - {ex.GetBaseException().Message} // INNER - {ex.InnerException?.Message} // SOURCE - {ex.Source} // STACKTRACE - {ex.StackTrace} // TARGETSITE - {ex.TargetSite}");
      Debug.WriteLine($"ERROR: {debObj}.{debProp}");
      foreach (var k in model.Keys)
      {
        foreach (var kvp in model[k])
        {
          Debug.WriteLine($"{k}.{kvp.Key} - {kvp.Value}");
        }
      }
      return null;
    }
  }
}