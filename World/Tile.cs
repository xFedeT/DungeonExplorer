// ============================================
// Tile.cs - Rappresenta una singola tile del mondo
// ============================================
using Microsoft.Xna.Framework;

namespace DungeonExplorer.World
{
    public enum TileType
    {
        Wall,
        Floor,
        Door,
        Corridor
    }

    /// <summary>
    /// Rappresenta una singola tile nel dungeon
    /// </summary>
    public class Tile
    {
        public TileType Type { get; set; }
        public bool IsWalkable { get; set; }
        public bool IsTransparent { get; set; }
        public Vector2 Position { get; set; }
        public Color Tint { get; set; }
        public bool IsExplored { get; set; }
        public bool IsVisible { get; set; }

        public Tile(TileType type, Vector2 position)
        {
            Type = type;
            Position = position;
            Tint = Color.White;
            IsExplored = false;
            IsVisible = false;

            // Imposta le proprietà base in base al tipo
            switch (type)
            {
                case TileType.Wall:
                    IsWalkable = false;
                    IsTransparent = false;
                    break;
                case TileType.Floor:
                case TileType.Corridor:
                case TileType.Door:
                    IsWalkable = true;
                    IsTransparent = true;
                    break;
            }
        }

        public void SetExplored()
        {
            IsExplored = true;
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (visible) IsExplored = true;
        }
    }
}