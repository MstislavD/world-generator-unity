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
            weighted_tree_test3();
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
        foreach (int i in extracted)
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

    void weighted_tree_test3()
    {
        WeightedTree<int> tree = new WeightedTree<int>();
        int nodes_num = 1000;
        int peek_num = 10000;

        for (int i = 0; i < nodes_num; i++)
        {
            tree.Add(i, 1f);
        }

        Dictionary<int, int> peeks_by_item = new Dictionary<int, int>()
        {
            { Random.Range(0, nodes_num), 0 },
            { Random.Range(0, nodes_num), 0 },
            { Random.Range(0, nodes_num), 0 }
        };

        int[] items = peeks_by_item.Keys.ToArray();
        tree.Add(items[0], nodes_num / 2);
        tree.Add(items[1], nodes_num / 4);
        tree.Add(items[2], nodes_num / 8);

        for (int i = 0; i < peek_num; i++)
        {
            int item = tree.Peek(Random.Range(0, 1f));
            if (peeks_by_item.ContainsKey(item))
            {
                peeks_by_item[item] += 1;
            }
        }

        string result = "";
        for (int i = 0; i < 3; i++)
        {
            result += $"{items[i]} (weight: {nodes_num / Mathf.Pow(2, i + 1)} ) -> {peeks_by_item[items[i]]} peeks; ";
        }
        Debug.Log(result);


    }

}
