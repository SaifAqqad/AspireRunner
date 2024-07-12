using System.Text.Json;
using System.Text.Json.Nodes;

namespace AspireRunner.Core.Extensions;

internal static class JsonExtensions
{
    public static JsonObject Flatten(this JsonNode? obj)
    {
        var flatObj = new JsonObject();
        if (obj == null)
        {
            return flatObj;
        }

        FlattenInto(obj, flatObj, "$");
        return flatObj;
    }

    private static void FlattenInto(JsonNode? node, JsonNode obj, string path)
    {
        if (node == null)
        {
            return;
        }

        if (node.GetValueKind() is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            obj[path] = node.DeepClone();
        }

        switch (node)
        {
            case JsonObject jsonObj:
            {
                foreach (var property in jsonObj)
                {
                    FlattenInto(property.Value, obj, $"{path}.{property.Key}");
                }

                break;
            }
            case JsonArray jsonArray:
            {
                for (var i = 0; i < jsonArray.Count; i++)
                {
                    FlattenInto(jsonArray[i], obj, $"{path}[{i}]");
                }

                break;
            }
        }
    }
}