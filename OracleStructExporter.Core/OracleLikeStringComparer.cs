using System;
using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class OracleLikeStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int minLength = Math.Min(x.Length, y.Length);
            for (int i = 0; i < minLength; i++)
            {
                char c1 = x[i];
                char c2 = y[i];

                int group1 = GetCharGroup(c1);
                int group2 = GetCharGroup(c2);

                if (group1 != group2)
                {
                    // Сравниваем группы: цифры < буквы < спецсимволы
                    return group1.CompareTo(group2);
                }
                else if (c1 != c2)
                {
                    // Внутри одной группы сравниваем по коду символа
                    return c1.CompareTo(c2);
                }
            }

            // Если начало строк совпадает, сравниваем по длине
            return x.Length.CompareTo(y.Length);
        }

        // Определяет группу символа: 1 (цифры), 2 (буквы), 3 (спецсимволы)
        private int GetCharGroup(char c)
        {
            if (c >= '0' && c <= '9') return 1;      // Цифры
            if (c >= 'A' && c <= 'Z') return 2;      // Заглавные буквы
            if (c >= 'a' && c <= 'z') return 2;      // Строчные буквы
            return 3;                                // Спецсимволы и остальное
        }
    }
}
