using System;
using System.Collections.Generic;

namespace MoonPlayer.Services
{
    public static class Sorter
    {
        public static void QuickSort<T>(List<T> list, Func<T, T, int> comparison)
        {
            QuickSort(list, 0, list.Count - 1, comparison);
        }

        private static void QuickSort<T>(List<T> list, int low, int high, Func<T, T, int> comparison)
        {
            if (low < high)
            {
                int pivotIndex = Partition(list, low, high, comparison);
                QuickSort(list, low, pivotIndex - 1, comparison);
                QuickSort(list, pivotIndex + 1, high, comparison);
            }
        }

        private static int Partition<T>(List<T> list, int low, int high, Func<T, T, int> comparison)
        {
            T pivot = list[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                if (comparison(list[j], pivot) < 0)
                {
                    i++;
                    Swap(list, i, j);
                }
            }

            Swap(list, i + 1, high);
            return i + 1;
        }

        private static void Swap<T>(List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
