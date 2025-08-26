// ============================================
// AStar.cs - Implementazione algoritmo A*
// ============================================
using Microsoft.Xna.Framework;
using DungeonExplorer.World;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DungeonExplorer.AI
{
    /// <summary>
    /// Implementazione completa dell'algoritmo A* per pathfinding
    /// </summary>
    public class AStar
    {
        private const int TILE_SIZE = 32;
        
        // Direzioni di movimento (4-directional)
        private readonly Vector2[] _directions = new Vector2[]
        {
            new Vector2(0, -1), // Su
            new Vector2(1, 0),  // Destra
            new Vector2(0, 1),  // Giù
            new Vector2(-1, 0)  // Sinistra
        };

        // Opzionale: movimento diagonale (8-directional)
        private readonly Vector2[] _diagonalDirections = new Vector2[]
        {
            new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
            new Vector2(-1, 0),                       new Vector2(1, 0),
            new Vector2(-1, 1),  new Vector2(0, 1),  new Vector2(1, 1)
        };

        private Dictionary<Vector2, Node> _nodes;
        private bool _allowDiagonal;

        public AStar(bool allowDiagonal = false)
        {
            _allowDiagonal = allowDiagonal;
            _nodes = new Dictionary<Vector2, Node>();
        }

        /// <summary>
        /// Trova il percorso ottimale tra due punti nel mondo
        /// </summary>
        /// <param name="start">Posizione di partenza in pixel</param>
        /// <param name="goal">Posizione obiettivo in pixel</param>
        /// <param name="dungeon">Il dungeon per controllare le collisioni</param>
        /// <returns>Lista di posizioni Vector2 che rappresentano il percorso</returns>
        public List<Vector2> FindPath(Vector2 start, Vector2 goal, Dungeon dungeon)
        {
            // Converte le posizioni da pixel a coordinate griglia
            Vector2 startGrid = WorldToGrid(start);
            Vector2 goalGrid = WorldToGrid(goal);

            // Verifica che start e goal siano validi
            if (!IsValidPosition(startGrid, dungeon) || !IsValidPosition(goalGrid, dungeon))
                return null;

            // Inizializza le strutture dati
            _nodes.Clear();
            var openSet = new SortedSet<Node>();
            var closedSet = new HashSet<Vector2>();

            // Crea il nodo di partenza
            Node startNode = GetOrCreateNode(startGrid, dungeon);
            startNode.GCost = 0f;
            startNode.HCost = CalculateHeuristic(startGrid, goalGrid);
            
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Prende il nodo con FCost più basso
                Node currentNode = openSet.First();
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                // Verifica se abbiamo raggiunto l'obiettivo
                if (currentNode.Position == goalGrid)
                {
                    return ReconstructPath(currentNode);
                }

                // Esplora i nodi vicini
                var directions = _allowDiagonal ? _diagonalDirections : _directions;
                
                foreach (Vector2 direction in directions)
                {
                    Vector2 neighborPos = currentNode.Position + direction;
                    
                    // Salta se già visitato
                    if (closedSet.Contains(neighborPos))
                        continue;

                    // Salta se non è una posizione valida
                    if (!IsValidPosition(neighborPos, dungeon))
                        continue;

                    Node neighbor = GetOrCreateNode(neighborPos, dungeon);
                    
                    // Calcola il nuovo G cost
                    float movementCost = _allowDiagonal && IsDiagonal(direction) ? 1.414f : 1f; // √2 per diagonali
                    float tentativeGCost = currentNode.GCost + movementCost;

                    // Se questo percorso verso il neighbor è migliore
                    if (tentativeGCost < neighbor.GCost)
                    {
                        // Rimuove dalla open set se già presente (per aggiornare la posizione)
                        openSet.Remove(neighbor);
                        
                        neighbor.Parent = currentNode;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHeuristic(neighborPos, goalGrid);
                        
                        openSet.Add(neighbor);
                    }
                }
            }

            // Nessun percorso trovato
            return null;
        }

        private Node GetOrCreateNode(Vector2 position, Dungeon dungeon)
        {
            if (!_nodes.TryGetValue(position, out Node node))
            {
                bool isWalkable = IsWalkable(position, dungeon);
                node = new Node(position, isWalkable);
                _nodes[position] = node;
            }
            return node;
        }

        private bool IsValidPosition(Vector2 gridPos, Dungeon dungeon)
        {
            // Verifica i bounds
            if (gridPos.X < 0 || gridPos.Y < 0 || 
                gridPos.X >= dungeon.Width || gridPos.Y >= dungeon.Height)
                return false;

            return IsWalkable(gridPos, dungeon);
        }

        private bool IsWalkable(Vector2 gridPos, Dungeon dungeon)
        {
            var tile = dungeon.GetTile((int)gridPos.X, (int)gridPos.Y);
            return tile != null && tile.IsWalkable;
        }

        private float CalculateHeuristic(Vector2 from, Vector2 to)
        {
            // Distanza Manhattan (più veloce per griglia 4-directional)
            if (!_allowDiagonal)
                return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
            
            // Distanza Euclidea (migliore per movimento diagonale)
            return Vector2.Distance(from, to);
        }

        private bool IsDiagonal(Vector2 direction)
        {
            return direction.X != 0 && direction.Y != 0;
        }

        private List<Vector2> ReconstructPath(Node goalNode)
        {
            var path = new List<Vector2>();
            Node current = goalNode;

            // Ricostruisce il percorso andando all'indietro dai parent
            while (current != null)
            {
                // Converte da coordinate griglia a coordinate mondo (centro tile)
                Vector2 worldPos = GridToWorld(current.Position);
                path.Add(worldPos);
                current = current.Parent;
            }

            // Inverti il percorso per averlo da start a goal
            path.Reverse();

            // Opzionale: ottimizza il percorso rimuovendo nodi ridondanti
            return OptimizePath(path);
        }

        private List<Vector2> OptimizePath(List<Vector2> path)
        {
            if (path.Count <= 2) return path;

            var optimizedPath = new List<Vector2> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 prev = path[i - 1];
                Vector2 current = path[i];
                Vector2 next = path[i + 1];

                // Se la direzione cambia, mantieni il punto
                Vector2 dir1 = Vector2.Normalize(current - prev);
                Vector2 dir2 = Vector2.Normalize(next - current);

                float dot = Vector2.Dot(dir1, dir2);
                if (dot < 0.99f) // Non perfettamente allineati
                {
                    optimizedPath.Add(current);
                }
            }

            optimizedPath.Add(path[path.Count - 1]);
            return optimizedPath;
        }

        private Vector2 WorldToGrid(Vector2 worldPos)
        {
            return new Vector2(
                (float)Math.Floor(worldPos.X / TILE_SIZE),
                (float)Math.Floor(worldPos.Y / TILE_SIZE)
            );
        }

        private Vector2 GridToWorld(Vector2 gridPos)
        {
            return new Vector2(
                gridPos.X * TILE_SIZE + TILE_SIZE / 2f,
                gridPos.Y * TILE_SIZE + TILE_SIZE / 2f
            );
        }
    }
}