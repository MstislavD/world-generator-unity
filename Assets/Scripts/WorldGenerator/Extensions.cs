using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static T[] Populate<T>(int length, T value)
    {
        T[] array = new T[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = value;
        }
        return array;
    }

    public static T[] Populate<T>(this T value, int length)
    {
        T[] array = new T[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = value;
        }
        return array;
    }

}
