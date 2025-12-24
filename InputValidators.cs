using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GraphVisualizer.Utils
{
    public static class InputValidators
    {
        public static bool ValidateMatrix(string matrixText, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(matrixText))
            {
                errorMessage = "Матрица не может быть пустой";
                return false;
            }

            var lines = matrixText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                errorMessage = "Матрица должна содержать хотя бы одну строку";
                return false;
            }

            int expectedColumns = lines.Length;

            for (int i = 0; i < lines.Length; i++)
            {
                var values = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length != expectedColumns)
                {
                    errorMessage = $"Строка {i + 1}: ожидается {expectedColumns} значений, найдено {values.Length}";
                    return false;
                }

                foreach (var value in values)
                {
                    if (!IsValidNumber(value))
                    {
                        errorMessage = $"Строка {i + 1}, значение '{value}': неверный формат числа";
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool ValidateAdjacencyList(string listText, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(listText))
            {
                errorMessage = "Список смежности не может быть пустым";
                return false;
            }

            var lines = listText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Поддерживаем оба символа стрелки: → и ->
                if (!Regex.IsMatch(line, @"^\s*\d+\s*:(?:\s*(?:→|->)\s*\d+(?:\(\s*[\d\.,]+\s*\))?)*\s*$"))
                {
                    errorMessage = $"Неверный формат строки: '{line}'\nФормат: 'вершина: →смежная1(вес) →смежная2(вес)' или 'вершина: ->смежная1(вес) ->смежная2(вес)'";
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            string normalized = value.Replace('.', ',');

            return double.TryParse(normalized, NumberStyles.Any,
                CultureInfo.CurrentCulture, out _);
        }

        public static double ParseDouble(string value, double defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            string normalized = value.Replace('.', ',');

            if (double.TryParse(normalized, NumberStyles.Any,
                CultureInfo.CurrentCulture, out double result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}