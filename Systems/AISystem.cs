using Microsoft.Xna.Framework;
using DungeonExplorer.Entities;
using DungeonExplorer.World;
using DungeonExplorer.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonExplorer.Components;

namespace DungeonExplorer.Systems
{
    /// <summary>
    /// Sistema AI che gestisce il comportamento intelligente dei nemici
    /// </summary>
    public class AISystem
    {
        private AStar _pathfinder;
        private Random _random;
        private const float TILE_SIZE = 32f;

        public AISystem()
        {
            _pathfinder = new AStar();
            _random = new Random();
        }

        /// <summary>
        /// Aggiorna l'AI di un nemico
        /// </summary>
        public void Update(GameTime gameTime, Enemy enemy, Player player, Dungeon dungeon)
        {
            if (enemy?.AI == null || enemy.Health?.CurrentHealth <= 0) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Aggiorna i timer
            UpdateTimers(enemy.AI, deltaTime);

            // Determina e esegui il comportamento
            AIBehaviorType behavior = DetermineBehavior(enemy, player, dungeon);
            ExecuteBehavior(enemy, player, dungeon, behavior, deltaTime);

            // Aggiorna lo stato dell'AI
            UpdateAIState(enemy, player);
        }

        /// <summary>
        /// Determina il comportamento appropriato per il nemico
        /// </summary>
        private AIBehaviorType DetermineBehavior(Enemy enemy, Player player, Dungeon dungeon)
        {
            float distanceToPlayer = Vector2.Distance(enemy.Transform.Position, player.Transform.Position);

            // Se il giocatore è morto, torna al comportamento di pattuglia
            if (player.Health.CurrentHealth <= 0)
            {
                return AIBehaviorType.Patrol;
            }

            // Comportamento basato sulla distanza e visibilità
            if (distanceToPlayer <= enemy.AI.AttackRange && CanSeeTarget(enemy, player, dungeon))
            {
                return AIBehaviorType.Attack;
            }
            else if (distanceToPlayer <= enemy.AI.ChaseRange && CanSeeTarget(enemy, player, dungeon))
            {
                return AIBehaviorType.Chase;
            }
            else if (enemy.AI.CurrentState == AIState.Alerted && enemy.AI.AlertTimer > 0)
            {
                return AIBehaviorType.Investigate;
            }
            else
            {
                return AIBehaviorType.Patrol;
            }
        }

        /// <summary>
        /// Esegue il comportamento specificato
        /// </summary>
        private void ExecuteBehavior(Enemy enemy, Player player, Dungeon dungeon, AIBehaviorType behavior, float deltaTime)
        {
            switch (behavior)
            {
                case AIBehaviorType.Patrol:
                    ExecutePatrolBehavior(enemy, dungeon, deltaTime);
                    break;
                    
                case AIBehaviorType.Chase:
                    ExecuteChaseBehavior(enemy, player, dungeon, deltaTime);
                    break;
                    
                case AIBehaviorType.Attack:
                    ExecuteAttackBehavior(enemy, player, deltaTime);
                    break;
                    
                case AIBehaviorType.Investigate:
                    ExecuteInvestigateBehavior(enemy, dungeon, deltaTime);
                    break;
                    
                case AIBehaviorType.Flee:
                    ExecuteFleeBehavior(enemy, player, dungeon, deltaTime);
                    break;
            }
        }

        /// <summary>
        /// Comportamento di pattuglia
        /// </summary>
        private void ExecutePatrolBehavior(Enemy enemy, Dungeon dungeon, float deltaTime)
        {
            // Se non ha punti di pattuglia, ne genera alcuni
            if (enemy.AI.PatrolPoints.Count == 0)
            {
                GeneratePatrolPoints(enemy, dungeon);
            }

            if (enemy.AI.PatrolPoints.Count > 0)
            {
                Vector2 targetPoint = enemy.AI.PatrolPoints[enemy.AI.CurrentPatrolIndex];
                float distanceToTarget = Vector2.Distance(enemy.Transform.Position, targetPoint);

                if (distanceToTarget < TILE_SIZE)
                {
                    // Raggiunto il punto, passa al successivo dopo una pausa
                    enemy.AI.PatrolPauseTimer += deltaTime;
                    
                    if (enemy.AI.PatrolPauseTimer >= enemy.AI.PatrolPauseDuration)
                    {
                        enemy.AI.CurrentPatrolIndex = (enemy.AI.CurrentPatrolIndex + 1) % enemy.AI.PatrolPoints.Count;
                        enemy.AI.PatrolPauseTimer = 0f;
                        enemy.AI.CurrentPath.Clear(); // Ricalcola il path
                    }
                }
                else
                {
                    // Muovi verso il punto di pattuglia
                    MoveTowardsTarget(enemy, targetPoint, dungeon, deltaTime, enemy.AI.PatrolSpeed);
                }
            }
        }

        /// <summary>
        /// Comportamento di inseguimento
        /// </summary>
        private void ExecuteChaseBehavior(Enemy enemy, Player player, Dungeon dungeon, float deltaTime)
        {
            enemy.AI.CurrentState = AIState.Chasing;
            enemy.AI.AlertTimer = enemy.AI.AlertDuration; // Mantiene lo stato di allerta
            
            Vector2 playerPos = player.Transform.Position;
            MoveTowardsTarget(enemy, playerPos, dungeon, deltaTime, enemy.AI.ChaseSpeed);
        }

        /// <summary>
        /// Comportamento di attacco
        /// </summary>
        private void ExecuteAttackBehavior(Enemy enemy, Player player, float deltaTime)
        {
            enemy.AI.CurrentState = AIState.Attacking;
            
            // Ferma il movimento per attaccare
            if (enemy.Movement != null)
            {
                enemy.Movement.IsMoving = false;
            }

            // Gestisce il cooldown dell'attacco
            if (enemy.AI.AttackCooldownTimer <= 0f)
            {
                PerformAttack(enemy, player);
                enemy.AI.AttackCooldownTimer = enemy.AI.AttackCooldown;
            }
        }

        /// <summary>
        /// Comportamento di investigazione
        /// </summary>
        private void ExecuteInvestigateBehavior(Enemy enemy, Dungeon dungeon, float deltaTime)
        {
            enemy.AI.CurrentState = AIState.Investigating;

            // Se non ha una posizione da investigare, usa l'ultima posizione conosciuta del giocatore
            if (enemy.AI.LastKnownPlayerPosition == Vector2.Zero)
            {
                // Genera una posizione casuale nelle vicinanze
                enemy.AI.LastKnownPlayerPosition = enemy.Transform.Position + 
                    new Vector2(_random.Next(-200, 200), _random.Next(-200, 200));
            }

            float distanceToInvestigate = Vector2.Distance(enemy.Transform.Position, enemy.AI.LastKnownPlayerPosition);
            
            if (distanceToInvestigate < TILE_SIZE)
            {
                // Ha raggiunto la posizione, resta in attesa
                enemy.AI.InvestigateTimer += deltaTime;
                
                if (enemy.AI.InvestigateTimer >= enemy.AI.InvestigateDuration)
                {
                    // Finito di investigare, torna alla pattuglia
                    enemy.AI.CurrentState = AIState.Patrol;
                    enemy.AI.AlertTimer = 0f;
                    enemy.AI.InvestigateTimer = 0f;
                    enemy.AI.LastKnownPlayerPosition = Vector2.Zero;
                }
            }
            else
            {
                // Muovi verso la posizione da investigare
                MoveTowardsTarget(enemy, enemy.AI.LastKnownPlayerPosition, dungeon, deltaTime, enemy.AI.PatrolSpeed);
            }
        }

        /// <summary>
        /// Comportamento di fuga
        /// </summary>
        private void ExecuteFleeBehavior(Enemy enemy, Player player, Dungeon dungeon, float deltaTime)
        {
            enemy.AI.CurrentState = AIState.Fleeing;
            
            // Calcola la direzione opposta al giocatore
            Vector2 directionFromPlayer = Vector2.Normalize(enemy.Transform.Position - player.Transform.Position);
            Vector2 fleeTarget = enemy.Transform.Position + directionFromPlayer * 200f; // Fuggi per 200 unità
            
            MoveTowardsTarget(enemy, fleeTarget, dungeon, deltaTime, enemy.AI.ChaseSpeed * 1.2f);
        }

        /// <summary>
        /// Muove un nemico verso una posizione target usando pathfinding
        /// </summary>
        private void MoveTowardsTarget(Enemy enemy, Vector2 targetPosition, Dungeon dungeon, float deltaTime, float speed)
        {
            // Controlla se serve ricalcolare il path
            if (enemy.AI.CurrentPath.Count == 0 || enemy.AI.PathRecalculateTimer <= 0f)
            {
                CalculatePath(enemy, targetPosition, dungeon);
                enemy.AI.PathRecalculateTimer = enemy.AI.PathRecalculateInterval;
            }

            if (enemy.AI.CurrentPath.Count > 0)
            {
                Vector2 nextWaypoint = enemy.AI.CurrentPath[0];
                float distanceToWaypoint = Vector2.Distance(enemy.Transform.Position, nextWaypoint);

                if (distanceToWaypoint < TILE_SIZE * 0.5f)
                {
                    // Raggiunto il waypoint, passa al successivo
                    enemy.AI.CurrentPath.RemoveAt(0);
                }
                else
                {
                    // Muovi verso il waypoint
                    Vector2 direction = Vector2.Normalize(nextWaypoint - enemy.Transform.Position);
                    Vector2 movement = direction * speed * deltaTime;
                    enemy.Transform.Position += movement;

                    // Aggiorna componente movimento se presente
                    if (enemy.Movement != null)
                    {
                        enemy.Movement.IsMoving = true;
                        enemy.Movement.LastDirection = direction;
                    }
                }
            }
        }

        /// <summary>
        /// Calcola un path usando A*
        /// </summary>
        private void CalculatePath(Enemy enemy, Vector2 targetPosition, Dungeon dungeon)
        {
            var startNode = new Node((int)(enemy.Transform.Position.X / TILE_SIZE), (int)(enemy.Transform.Position.Y / TILE_SIZE));
            var endNode = new Node((int)(targetPosition.X / TILE_SIZE), (int)(targetPosition.Y / TILE_SIZE));

            var pathNodes = _pathfinder.FindPath(startNode, endNode, dungeon);
            
            enemy.AI.CurrentPath.Clear();
            
            if (pathNodes != null)
            {
                foreach (var node in pathNodes)
                {
                    enemy.AI.CurrentPath.Add(new Vector2(node.X * TILE_SIZE + TILE_SIZE * 0.5f, 
                                                        node.Y * TILE_SIZE + TILE_SIZE * 0.5f));
                }
            }
        }

        /// <summary>
        /// Controlla se un nemico può vedere il target
        /// </summary>
        private bool CanSeeTarget(Enemy enemy, Entity target, Dungeon dungeon)
        {
            // Implementazione semplificata del line of sight
            Vector2 start = enemy.Transform.Position;
            Vector2 end = target.Transform.Position;
            
            // Usa un raycast semplificato
            return !PathfindingHelper.IsLineBlocked(start, end, dungeon, TILE_SIZE);
        }

        /// <summary>
        /// Genera punti di pattuglia per un nemico
        /// </summary>
        private void GeneratePatrolPoints(Enemy enemy, Dungeon dungeon)
        {
            enemy.AI.PatrolPoints.Clear();
            
            // Trova la stanza in cui si trova il nemico
            var currentRoom = dungeon.GetRoomAt(enemy.Transform.Position);
            
            if (currentRoom != null)
            {
                // Genera 2-4 punti di pattuglia nella stanza
                int pointCount = _random.Next(2, 5);
                
                for (int i = 0; i < pointCount; i++)
                {
                    Vector2 point = currentRoom.GetRandomPosition() * TILE_SIZE;
                    enemy.AI.PatrolPoints.Add(point);
                }
            }
            else
            {
                // Se non è in una stanza, crea punti intorno alla posizione corrente
                for (int i = 0; i < 4; i++)
                {
                    float angle = (float)(i * Math.PI * 0.5);
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 100f;
                    enemy.AI.PatrolPoints.Add(enemy.Transform.Position + offset);
                }
            }
        }

        /// <summary>
        /// Esegue un attacco contro il giocatore
        /// </summary>
        private void PerformAttack(Enemy enemy, Player player)
        {
            if (player.Health != null)
            {
                int damage = enemy.AI.AttackDamage;
                player.Health.TakeDamage(damage);
                
                // Feedback visuale/audio dell'attacco potrebbe essere aggiunto qui
            }
        }

        /// <summary>
        /// Aggiorna i timer dell'AI
        /// </summary>
        private void UpdateTimers(AIComponent ai, float deltaTime)
        {
            if (ai.AttackCooldownTimer > 0)
                ai.AttackCooldownTimer -= deltaTime;

            if (ai.AlertTimer > 0)
                ai.AlertTimer -= deltaTime;

            if (ai.PathRecalculateTimer > 0)
                ai.PathRecalculateTimer -= deltaTime;
        }

        /// <summary>
        /// Aggiorna lo stato generale dell'AI
        /// </summary>
        private void UpdateAIState(Enemy enemy, Player player)
        {
            float distanceToPlayer = Vector2.Distance(enemy.Transform.Position, player.Transform.Position);
            
            // Aggiorna l'ultima posizione conosciuta del giocatore se è visibile
            if (distanceToPlayer <= enemy.AI.SightRange && CanSeeTarget(enemy, player, null))
            {
                enemy.AI.LastKnownPlayerPosition = player.Transform.Position;
                enemy.AI.AlertTimer = enemy.AI.AlertDuration;
            }
        }
    }

    /// <summary>
    /// Tipi di comportamento AI
    /// </summary>
    public enum AIBehaviorType
    {
        Patrol,
        Chase,
        Attack,
        Investigate,
        Flee
    }
}