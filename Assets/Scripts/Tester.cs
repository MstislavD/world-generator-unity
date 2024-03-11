using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Tester : MonoBehaviour
{
    [SerializeField]
    bool weightedTree = false;

    private void OnValidate()
    {
        if (weightedTree)
        {
            weighted_tree_test2();
            weightedTree = false;
        }
    }

    void weighted_tree_test()
    {
        bool reverse = true;
        int value_num = 10;
        int extraction_num = 10;
        Dictionary<int, float> weight_by_value = new Dictionary<int, float>();
        List<int> extracted = new List<int>();
        WeightedTree<int> tree = new WeightedTree<int>();
        float total_weight = 0;

        for (int i = 0; i < value_num; i++)
        {
            float weight = Mathf.Pow(2f, i);
            total_weight += weight;
            weight_by_value[i] = weight;
            tree.Add(i, weight);
        }

        if (reverse)
        {
            total_weight = 0;
            for (int i = 0; i < value_num; i++)
            {
                float weight = Mathf.Pow(0.5f, i);
                weight_by_value[i] = weight;
                total_weight += weight;
                tree.Add(i, weight);
            }
        }

        total_weight = 0;
        for (int i = 0; i < extraction_num; i++)
        {
            float position = Random.Range(0f, 1f);
            int item = tree.Extract(position);
            total_weight += weight_by_value[item];
            extracted.Add(item);
        }

        string result = "";
        foreach(int i in extracted)
        {
            result += $"{i} -> ";
        }

        Debug.Log($"Extracted {result}");
        Debug.Log(tree.About());

    }

    void weighted_tree_test2()
    {
        bool reverse = false;
        int runs_num = 200;
        int value_num = 10;
        int extraction_num = 10;

        Dictionary<int, float> extraction_order_by_item = Enumerable.Range(0, value_num).ToDictionary(i => i, i => 0f);

        for (int j = 0; j < runs_num; j++)
        {
            WeightedTree<int> tree = new WeightedTree<int>();

            for (int i = 0; i < value_num; i++)
            {
                float weight = Mathf.Pow(2f, i);
                tree.Add(i, weight);
            }

            if (reverse)
            {
                for (int i = 0; i < value_num; i++)
                {
                    float weight = Mathf.Pow(0.5f, i);
                    tree.Add(i, weight);
                }
            }

            for (int i = 0; i < extraction_num; i++)
            {
                float position = Random.Range(0f, 1f);
                int item = tree.Extract(position);
                extraction_order_by_item[item] += i;
            }
        }

        string result = "";
        foreach (int i in Enumerable.Range(0, value_num))
        {
            result += $"{i} -> {extraction_order_by_item[i] / runs_num}; ";
        }

        Debug.Log($"Extracted {result}");
    }
}
