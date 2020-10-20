using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Linq.Expressions;

namespace InterstellarFuelSwitch
{
    public static class ParseTools
    {
        public static double ParseDouble(string data)
        {
            string trimmed = data.Trim();

            return trimmed != string.Empty && double.TryParse(trimmed, out double result) ? result : 0.0;
        }

        public static List<double> ParseDoubles<T>(string stringOfDoubles, Expression<Func<T>> expr)
        {
            if (string.IsNullOrEmpty(stringOfDoubles))
                return new List<double>();

            var body = (MemberExpression)expr.Body;

            return ParseDoubles(stringOfDoubles, body.Member.Name);
        }

        public static List<double> ParseDoubles(string stringOfDoubles, string errorDisplayName = "")
        {
            var list = new List<double>();
            var array = stringOfDoubles.Trim().Split(';');
            foreach (var arrayItem in array)
            {
                if (double.TryParse(arrayItem.Trim(), out double item))
                    list.Add(item);
                else
                    Debug.Log("InsterstellarFuelSwitch: parseDoubles error in '" + stringOfDoubles + "', invalid float: " + errorDisplayName + " [len:" + arrayItem.Length + "] '" + arrayItem + "']");
            }
            return list;
        }

        public static List<int> ParseIntegers(string stringOfInts)
        {
            List<int> newIntList = new List<int>();
            string[] valueArray = stringOfInts.Split(';');
            for (int i = 0; i < valueArray.Length; i++)
            {
                if (int.TryParse(valueArray[i], out int newValue))
                    newIntList.Add(newValue);
                else
                    Debug.Log("InsterstellarFuelSwitch: error in '" + stringOfInts + "',  invalid integer: " + valueArray[i]);
            }
            return newIntList;
        }

        public static List<string> ParseNames(string names)
        {
            return ParseNames(names, false, true, string.Empty);
        }

        public static List<string> ParseNames(string names, bool replaceBackslashErrors)
        {
            return ParseNames(names, replaceBackslashErrors, true, string.Empty);
        }

        public static List<string> ParseNames(string names, bool replaceBackslashErrors, bool trimWhiteSpace, string prefix)
        {
            var source = names.Split(';',',').ToList<string>();
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (source[i] == string.Empty)
                    source.RemoveAt(i);
            }
            if (trimWhiteSpace)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Trim(' ');
                }
            }
            if (prefix != string.Empty)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    source[i] = prefix + source[i];
                }
            }
            if (replaceBackslashErrors)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Replace('\\', '/');
                }
            }
            return source.ToList<string>();
        }

        public static string Print(IEnumerable<string> list)
        {
            var result = "";
            foreach (var item in list)
            {
                result += item + ";";
            }
            return result;
        }

        public static bool ListEquals<T>(IList<T> list1, IList<T> list2)
        {
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i])) return false;
            }
            return true;
        }
    }
}
