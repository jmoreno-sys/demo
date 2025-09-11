using System;
using System.Data;
using System.Linq;
using RestSharp.Extensions;

namespace ExcelDataReader
{
    public class ExcelColum : Attribute
    {
        public ExcelColum(string columnName, string description= "")
        {
            Index = ExcelColumnNameToNumber(columnName);
            Description = description;
        }

        public int Index { get; private set; }
        public string Description { get; private set; }

        public ExcelColum(int index, string description = "")
        {
            Index = index;
            Description = description;
        }

        private static int ExcelColumnNameToNumber(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException("columnName");

            columnName = columnName.ToUpperInvariant();

            var sum = 0;

            for (var i = 0; i < columnName.Length; i++)
            {
                sum *= 26;
                sum += (columnName[i] - 'A' + 1);
            }

            return sum;
        }
    }

    public static class Converters
    {
        public static T ValueConverter<T>(DataRow row) where T : class
        {
            var model = Activator.CreateInstance<T>();
            var props = typeof(T).GetProperties().Where(m => m.GetCustomAttributes(true).OfType<ExcelColum>().Any());
            foreach (var prop in props)
            {
                var value = row[prop.GetAttribute<ExcelColum>().Index-1];
                prop.SetValue(model, value.ToString());
            }

            return model;
        }
    }
}