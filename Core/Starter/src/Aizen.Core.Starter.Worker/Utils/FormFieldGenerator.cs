using System.Reflection;
using Aizen.Core.Starter.Worker.Controllers;

namespace Aizen.Core.Starter.Worker.Utils;

public class FormFieldGenerator
{
    public static List<FormField> GetFormFields(Type type)
    {
        List<FormField> formFields = new List<FormField>();

        foreach (PropertyInfo property in type.GetProperties())
        {
            FormField field = new FormField
            {
                Name = property.Name,
                Type = ConvertToInputType(property.PropertyType)
            };

            if (IsComplexType(property.PropertyType))
            {
                field.NestedFields = GetFormFields(property.PropertyType);
            }

            formFields.Add(field);
        }

        return formFields;
    }

    private static bool IsComplexType(Type type)
    {
        return type.IsClass && type != typeof(string);
    }

    private static string ConvertToInputType(Type dataType)
    {
        if (dataType == typeof(string))
        {
            return "text";
        }

        if (dataType == typeof(int) || dataType == typeof(decimal) || dataType == typeof(double))
        {
            return "number";
        }

        if (dataType == typeof(DateTime))
        {
            return "date";
        }

        if (dataType == typeof(bool))
        {
            return "checkbox";
        }

        return "text";
    }
}