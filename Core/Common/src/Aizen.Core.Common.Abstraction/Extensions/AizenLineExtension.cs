using Aizen.Core.Common.Abstraction.Attributes;
using Aizen.Core.Common.Abstraction.Exception;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

namespace Aizen.Core.Common.Abstraction.Extensions
{
    public static class AizenLineExtension
    {
        public static string ToAizenLineString(
        this object data)
        {
            var properties = data.GetType()
                .GetProperties()
                .Where(a => a.CustomAttributes.Any(ca => ca.AttributeType == typeof(AizenLinePropertyAttribute)))
                .OrderBy(a => a.GetCustomAttribute<AizenLinePropertyAttribute>()?.Order)
                .ToList();

            var stringBuilder = new StringBuilder();

            foreach (var property in properties)
            {
                var capacity = property.GetCustomAttribute<AizenLinePropertyAttribute>()?.Capacity;
                var text = property.GetValue(data)?.ToString() ?? string.Empty;
                var spaceCount = capacity - text.Length;

                if (spaceCount < 0)
                {
                    throw new AizenException(
                        $"Out or the maximum capacity property {property.Name}.");
                }

                stringBuilder.Append(text);
                stringBuilder.Append("".PadRight(spaceCount.GetValueOrDefault()));
            }

            return stringBuilder.ToString();
        }
        public static T ToAizenLineObject<T>(
            this string line) where T : new()
        {
            var pegasusLine = Activator.CreateInstance<T>();
            var properties = pegasusLine.GetType()
                .GetProperties()
                .Where(a => a.CustomAttributes.Any(ca => ca.AttributeType == typeof(AizenLinePropertyAttribute)))
                .OrderBy(a => a.GetCustomAttribute<AizenLinePropertyAttribute>()?.Order)
                .ToList();

            var startIndex = 0;
            foreach (var property in properties)
            {
                var capacity = property.GetCustomAttribute<AizenLinePropertyAttribute>()?.Capacity;
                var text = line.Substring(
                    startIndex,
                    capacity.GetValueOrDefault());

                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(
                        pegasusLine,
                        text.TrimEnd());
                }
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(
                        pegasusLine,
                        Convert.ToInt32(text.TrimEnd()));
                }
                else
                {
                    throw new AizenException($"{property.PropertyType.Assembly} property type not supported by Aizen line serializer.");
                }

                startIndex += capacity.GetValueOrDefault();
            }


            return pegasusLine;
        }

        public static string ToAizenHashLine(this string line)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(line));
                line = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            return line;
        }
    }
}
