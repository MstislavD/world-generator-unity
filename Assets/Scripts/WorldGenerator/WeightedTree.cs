using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


public class WeightedTree<T>
{
    Dictionary<T, Node> node_by_value = new Dictionary<T, Node>();
    Node root;

    class Node
    {
        public T value;
        public float weight;
        public float subtree_weight;
        public Node left;
        public Node right;
        public Node parent;

        Node create_child(T value, float weight, bool is_left)
        {
            Node node = new Node();
            node.value = value;
            node.weight = weight;
            node.subtree_weight = weight;
            node.parent = this;
            if (is_left)
            {
                left = node;
            }
            else
            {
                right = node;
            }
            return node;
            
        }

        public void change_subtree_weight(float weight_delta)
        {
            subtree_weight += weight_delta;
            parent?.change_subtree_weight(weight_delta);
        }

        public Node add_node(T value, float weight)
        {
            subtree_weight += weight;
            if (left == null)
            {
                return create_child(value, weight, true);
            }
            else if (right == null)
            {
                return create_child(value, weight, false);
            }
            return left.subtree_weight < right.subtree_weight ? left.add_node(value, weight) : right.add_node(value, weight);
        }

        public Node find_node_by_position(float position)
        {
            if (position > subtree_weight)
            {
                throw new Exception("Position cannot be greater than subtree weight.");
            }
            float lw = left == null ? 0 : left.subtree_weight;
            if (position < lw)
            {
                return left.find_node_by_position(position);
            }
            else if (position - lw < weight || right == null)
            {
                return this;
            }
            else
            {
                position = position - lw - weight;
                if (position > right.subtree_weight)
                {
                    position = right.subtree_weight;
                }
                return right.find_node_by_position(position);
            }
        }

        public void remove()
        {
            change_subtree_weight(-weight);
            Node node = remove_r();
            value = node.value;
            weight = node.weight;
            subtree_weight += weight;
        }

        Node remove_r()
        {
            Node removed_node = this;
            if (left != null && right != null)
            {
                removed_node = left.subtree_weight > right.subtree_weight ? left.remove_r() : right.remove_r();
            }
            else if (left != null)
            {
                removed_node = left.remove_r();
            }
            else if (right != null)
            {
                removed_node = right.remove_r();
            }
            else if (parent != null)
            {
                if (parent.left == this)
                {
                    parent.left = null;
                }
                else
                {
                    parent.right = null;
                }
            }
            subtree_weight -= removed_node.weight;
            return removed_node;
        }
    }

    public void Add(T value, float weight)
    {
        if (node_by_value.ContainsKey(value))
        {
            Node node = node_by_value[value];
            node.change_subtree_weight(weight - node.weight);
            node.weight = weight;
        }
        else
        {
            if (root == null)
            {
                root = new Node();
                root.value = value;
                root.weight = weight;
                root.subtree_weight = weight;
                node_by_value[value] = root;
            }
            else
            {
                node_by_value[value] = root.add_node(value, weight);
            }
           
        }
    }

    public void AddMany(IEnumerable<T> items, float weight)
    {
        foreach (T item in items)
        {
            Add(item, weight);
        }
    }

    public T Extract(float position) 
    {
        Node node = peek(position);
        T value = node.value;
        node.remove();
        node_by_value.Remove(value);
        if (!node.value.Equals(value))
        {
            node_by_value[node.value] = node;
        }
        return value;
    }

    public T Peek(float position) => peek(position).value;

    Node peek(float position)
    {
        if (root == null)
        {
            throw new Exception("Tree is empty");
        }
        if (position < 0 || position > 1)
        {
            throw new Exception("Position must be in [0f; 1f] range");
        }
        position *= root.subtree_weight;
        Node node = root.find_node_by_position(position);
        return node;
    }

    public string About()
    {
        string result = "";

        result += $"Total number of nodes: {node_by_value.Count}. ";
        result += $"Total weight: {root.subtree_weight}. Average weight: {root.subtree_weight / node_by_value.Count}";

        return result;
    }

}
