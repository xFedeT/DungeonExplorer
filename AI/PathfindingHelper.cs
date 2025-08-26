// ============================================
// PathfindingHelper.cs - Utility per pathfinding
// ============================================
using Microsoft.Xna.Framework;
using DungeonExplorer.Entities;
using DungeonExplorer.World;
using System.Collections.Generic;
using System;

namespace DungeonExplorer.AI
{
    /// <summary>
    /// Classe di utilità per operazioni di pathfinding comuni
    /// </summary>
    public static class PathfindingHelper
    {
        private const int TILE_SIZE = 32;

        /// <summary>
        /// Verifica se c'è una linea di vista diretta tra due punti
        /// </summary>
        public static bool HasLineOfSight(Vector2 from, Vector2 to, Dungeon dungeon)
        {
            Vector2 direction = Vector2.Normalize(to - from);
            float distance = Vector2.Distance(from, to);
            int steps = (int)(distance / (TILE_SIZE / 4f)); // Check ogni 8 pixel

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 checkPos = Vector2.Lerp(from, to, t);
                Vector2 gridPos = new Vector2(
                    (float)Math.Floor(checkPos.X / TILE_SIZE),
                    (float)Math.Floor(checkPos.Y / TILE_SIZE)
                );

                var tile = dungeon.GetTile((int)gridPos.X, (int)gridPos.Y);
                if (tile == null || !tile.IsWalkable)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Trova la posizione più vicina raggiungibile dal target
        /// Utile quando il target esatto non è raggiungibile
        /// </summary>
        public static Vector2 FindNearestWalkablePosition(Vector2 target, Dungeon dungeon, int maxRadius = 5)
        {
            Vector2 gridTarget = new Vector2(
                (float)Math.Floor(target.X / TILE_SIZE),
                (float)Math.Floor(target.Y / TILE_SIZE)
            );

            // Se la posizione target è già walkable, ritornala
            var targetTile = dungeon.GetTile((int)gridTarget.X, (int)gridTarget.Y);
            if (targetTile != null && targetTile.IsWalkable)
                return target;

            // Cerca in cerchi concentrici
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) == radius || Math.Abs(dy) == radius)
                        {
                            Vector2 checkPos = gridTarget + new Vector2(dx, dy);
                            var tile = dungeon.GetTile((int)checkPos.X, (int)checkPos.Y);
                            
                            if (tile != null && tile.IsWalkable)
                            {
                                return new Vector2(
                                    checkPos.X * TILE_SIZE + TILE_SIZE / 2f,
                                    checkPos.Y * TILE_SIZE + TILE_SIZE / 2f
                                );
                            }
                        }
                    }
                }
            }

            // Se non trova nulla, ritorna la posizione originale
            return target;
        }

        /// <summary>
        /// Calcola una posizione di fuga dal player
        /// </summary>
        public static Vector2 FindFleePosition(Vector2 from, Vector2 fleeFrom, Dungeon dungeon, float fleeDistance = 128f)
        {
            Vector2 fleeDirection = Vector2.Normalize(from - fleeFrom);
            Vector2 fleeTarget = from + fleeDirection * fleeDistance;
            
            return FindNearestWalkablePosition(fleeTarget, dungeon);
        }

        /// <summary>
        /// Trova una posizione di intercettazione basata sulla velocità del target
        /// </summary>
        public static Vector2 CalculateInterceptPosition(Vector2 from, Vector2 targetPos, Vector2 targetVelocity, float interceptorSpeed)
        {
            Vector2 toTarget = targetPos - from;
            float distance = toTarget.Length();
            
            if (targetVelocity.Length() == 0 || interceptorSpeed <= 0)
                return targetPos;

            // Calcola il tempo di intercettazione usando la formula quadratica
            float a = targetVelocity.LengthSquared() - interceptorSpeed * interceptorSpeed;
            float b = 2 * Vector2.Dot(toTarget, targetVelocity);
            float c = toTarget.LengthSquared();

            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
                return targetPos; // Nessuna intercettazione possibile

            float t1 = (-b - MathF.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b + MathF.Sqrt(discriminant)) / (2 * a);

            float t = t1 > 0 ? t1 : t2;
            if (t < 0) return targetPos;

            return targetPos + targetVelocity * t;
        }

        /// <summary>
        /// Genera punti di pattuglia casuali in una zona
        /// </summary>
        public static List<Vector2> GeneratePatrolPoints(Vector2 center, Dungeon dungeon, int count = 4, float radius = 96f)
        {
            var points = new List<Vector2>();
            var random = new Random();

            for (int i = 0; i < count * 3; i++) // Tenta più volte per trovare punti validi
            {
                float angle = random.NextSingle() * MathF.PI * 2f;
                float distance = random.NextSingle() * radius;
                
                Vector2 point = center + new Vector2(
                    MathF.Cos(angle) * distance,
                    MathF.Sin(angle) * distance
                );

                Vector2 walkablePoint = FindNearestWalkablePosition(point, dungeon, 3);
                if (Vector2.Distance(walkablePoint, point) < TILE_SIZE * 2) // Abbastanza vicino al punto desiderato
                {
                    points.Add(walkablePoint);
                    if (points.Count >= count) break;
                }
            }

            return points;
        }
    }
}