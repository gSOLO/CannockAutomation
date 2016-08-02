using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CannockAutomation.Helpers;

namespace CannockAutomation.Extensions
{
    public static class EnumExtensions
    {
        public static String GetDescription(this Enum e)
        {
            try
            {
                var da = (DescriptionAttribute[])(e.GetType().GetField(e.ToString())).GetCustomAttributes(typeof(DescriptionAttribute), false);
                return da.Length > 0 ? da[0].Description : e.ToString();
            }
            catch
            {
                return e.ToString();
            }
        }

        public static String GetName(this Enum e)
        {
            return e.GetDescription();
        }

        public static T GetEnumValue<T>(this String description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                    {
                        return (T) field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name == description)
                    {
                        return (T) field.GetValue(null);
                    }
                }
            }
            return default(T);
        }

        public static List<T> ToList<T>(this Enum e, Enum exclude=default(Enum))
        {
            var eType = e.GetType();
            var list = new List<T>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Enum flag in Enum.GetValues(eType))
            {
                if (!e.HasFlag(flag) || (exclude != null && exclude.HasFlag(flag))) continue;
                if (exclude == null && flag.Count() != 1) continue;
                list.Add((T)Enum.Parse(eType, flag.ToString()));
            }
            return list.Distinct().ToList();
        }

        public static int Count(this Enum e)
        {
            var count = 0;

            var t = typeof (int);

            if (Enum.GetUnderlyingType(e.GetType()) != t) return 1;

            var value = (int)Convert.ChangeType(e, t);

            while (value != 0)
            {
                value = value & (value - 1);

                count++;
            }

            return count;
        }

        public static Boolean Any(this Enum e, Predicate<Enum> predicate=null)
        {
            if (predicate == null)
            {
                return e.Count() > 0;
            }
            var eType = e.GetType();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Enum flag in Enum.GetValues(eType))
            {
                if (!e.HasFlag(flag)) continue;
                if (predicate.Invoke(flag)) return true;
            }
            return false;
        }

        public static QueryHelper GetQuery(this Enum e, String match = null, String exclude = null, String startsWith = null, String endsWith = null)
        {
            var useDumbQuery = exclude == null && startsWith == null && endsWith == null;
            if (match == null && useDumbQuery) match = e.GetName().ToLower();
            return useDumbQuery ? new QueryHelper(e, match) : new SmartQueryHelper(e, match, exclude) { Start = startsWith, End = endsWith, };
        }

    }
}
