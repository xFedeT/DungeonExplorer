// ============================================
// Node.cs - Nodo per l'algoritmo A*
// ============================================
using Microsoft.Xna.Framework;
using System;

namespace DungeonExplorer.AI
{
    /// <summary>
    /// Rappresenta un nodo nella griglia per l'algoritmo A*
    /// </summary>
    public class Node : IComparable<Node>
    {
        public Vector2 Position { get; set; }
        public float GCost { get; set; }  // Distanza dal nodo di partenza
        public float HCost { get; set; }  // Distanza euristica dal target
        public float FCost => GCost + HCost; // Costo totale
        public Node Parent { get; set; }
        public bool IsWalkable { get; set; }

        public Node(Vector2 position, bool isWalkable = true)
        {
            Position = position;
            IsWalkable = isWalkable;
            GCost = float.MaxValue;
            HCost = 0f;
            Parent = null;
        }

        public int CompareTo(Node other)
        {
            int result = FCost.CompareTo(other.FCost);
            if (result == 0)
                result = HCost.CompareTo(other.HCost);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is Node other)
                return Position == other.Position;
            return false;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}