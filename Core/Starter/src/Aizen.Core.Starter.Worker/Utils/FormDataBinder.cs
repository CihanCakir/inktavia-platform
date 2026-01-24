using System.Reflection;

namespace Aizen.Core.Starter.Worker.Controllers
{
    public class FormDataBinder
    {
        public static void BindFormData(Dictionary<string, string> formData, object data)
        {
            foreach (var propertyInfo in data.GetType().GetProperties())
            {
                SetProperty(data, propertyInfo, formData);
            }
        }

        private static void SetProperty(object obj, PropertyInfo propertyInfo, Dictionary<string, string> formData)
        {
            if (propertyInfo.PropertyType == typeof(string))
            {
                propertyInfo.SetValue(obj, formData[propertyInfo.Name]);
            }
            else if (propertyInfo.PropertyType == typeof(int))
            {
                propertyInfo.SetValue(obj, int.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(decimal))
            {
                propertyInfo.SetValue(obj, decimal.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                propertyInfo.SetValue(obj, double.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(float))
            {
                propertyInfo.SetValue(obj, float.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                propertyInfo.SetValue(obj, DateTime.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                propertyInfo.SetValue(obj, bool.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                propertyInfo.SetValue(obj, Guid.Parse(formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType == typeof(Dictionary<string, string>))
            {
                var dict = new Dictionary<string, string>();
                foreach (var kv in formData)
                {
                    if (kv.Key.StartsWith($"{propertyInfo.Name}-"))
                    {
                        dict.Add(kv.Key.Substring($"{propertyInfo.Name}-".Length), kv.Value);
                    }
                }

                propertyInfo.SetValue(obj, dict);
            }
            else if (propertyInfo.PropertyType.IsEnum)
            {
                propertyInfo.SetValue(obj, Enum.Parse(propertyInfo.PropertyType, formData[propertyInfo.Name]));
            }
            else if (propertyInfo.PropertyType.IsClass &&
                     propertyInfo.PropertyType != typeof(Dictionary<string, string>))
            {
                var nestedObj = Activator.CreateInstance(propertyInfo.PropertyType);
                var nestedFormData = formData.Where(kv => kv.Key.StartsWith($"{propertyInfo.Name}-"))
                    .ToDictionary(kv => kv.Key.Substring($"{propertyInfo.Name}-".Length), kv => kv.Value);
                foreach (var nestedProperty in propertyInfo.PropertyType.GetProperties())
                {
                    if (nestedFormData.TryGetValue($"{nestedProperty.Name}",
                            out string nestedValue))
                    {
                        if (!string.IsNullOrEmpty(nestedValue))
                        {
                            SetProperty(nestedObj, nestedProperty, nestedFormData);
                        }
                    }
                }

                propertyInfo.SetValue(obj, nestedObj);
            }
        }
    }
}