using rabbit.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NETUtilities.Utils
{
    public static class DataMasking
    {
        private static readonly JsonSerializerOptions _settings = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        private static readonly JsonWriterOptions _writerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public static string MaskJson(object value)
        {
            if (value == null) return null;
            string json = value is string ? value as string : JsonSerializer.Serialize(value, _settings);
            if (string.IsNullOrEmpty(json)) return json;
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms, _writerOptions);
                MaskJsonElement(writer, document.RootElement);
                writer.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch { }
            return json;
        }

        private static void MaskJsonElement(Utf8JsonWriter writer, JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
                foreach (JsonProperty property in jsonElement.EnumerateObject())
                {
                    MaskJProperty(writer, property);
                }
                writer.WriteEndObject();
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                writer.WriteStartArray();
                foreach (JsonElement element in jsonElement.EnumerateArray())
                {
                    MaskJsonElement(writer, element);
                }
                writer.WriteEndArray();
            }
            else
            {
                jsonElement.WriteTo(writer);
            }
        }

        private static void MaskJProperty(Utf8JsonWriter writer, JsonProperty jsonProperty)
        {
            if (BaseConfiguration.DataMaskings.ContainsKey(jsonProperty.Name))
            {
                writer.WriteString(jsonProperty.Name, MaskData(jsonProperty.Value.ValueKind == JsonValueKind.String ? jsonProperty.Value.GetString() : jsonProperty.Value.GetRawText(), BaseConfiguration.DataMaskings[jsonProperty.Name]));
            }
            else
            {
                if (jsonProperty.Value.ValueKind != JsonValueKind.Array && jsonProperty.Value.ValueKind != JsonValueKind.Object)
                {
                    jsonProperty.WriteTo(writer);
                }
                else
                {
                    writer.WritePropertyName(jsonProperty.Name);
                    MaskJsonElement(writer, jsonProperty.Value);
                }
            }
        }

        public static string MaskXml(string xml)
        {
            XDocument xDoc = XDocument.Parse(xml);
            MaskXmlNode(xDoc.Root);
            return xDoc.ToString(SaveOptions.DisableFormatting);
        }

        private static void MaskXmlNode(XElement xNode)
        {
            if (xNode.HasElements)
            {
                foreach (XElement node in xNode.Elements())
                {
                    MaskXmlNode(node);
                }
            }
            else
            {
                xNode.Value = MaskDataByKey(xNode.Value, xNode.Name.LocalName);
            }
        }

        public static string MaskDataByKey(string input, string key) => BaseConfiguration.DataMaskings.ContainsKey(key) ? MaskData(input, BaseConfiguration.DataMaskings[key]) : input;

        public static string MaskData(string data, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(data)) return string.Empty;
            return pattern.ToUpper() switch
            {
                "MASKALL" => new string('*', data.Length),
                "HIDE" => "MASKED",
                "MASKCARD" => Regex.Replace(data, @"(\d[\s|-]?){12,19}\d", match => match.Value[..6] + new string('X', match.Value.Length - 10) + match.Value[^4..]),
                _ => data
            };
        }

        public static string MaskCardNumber(string cardNumber, bool isFormat = false)
        {
            cardNumber = MaskData(cardNumber, "MASKCARD");
            if (isFormat)
            {
                cardNumber = cardNumber.Replace('X', '*');
                List<string> subs = new();
                for (int i = 0; i < cardNumber.Length / 4; i++)
                {
                    subs.Add(cardNumber[(i * 4)..((i * 4) + 4)]);
                }
                if (cardNumber.Length % 4 > 0)
                {
                    subs.Add(cardNumber[^(cardNumber.Length % 4)..]);
                }
                cardNumber = string.Join(' ', subs);
            }
            return cardNumber;
        }
    }
}
