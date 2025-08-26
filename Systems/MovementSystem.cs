using Microsoft.Xna.Framework;
using DungeonExplorer.Entities;
using DungeonExplorer.Core;

namespace DungeonExplorer.Systems
{
    /// <summary>
    /// Sistema responsabile del movimento delle entità
    /// </summary>
    public class MovementSystem
    {
        /// <summary>
        /// Aggiorna il movimento del giocatore basato sull'input
        /// </summary>
        public void Update(GameTime gameTime, Player player, InputManager inputManager)
        {
            if (player?.Movement == null || player.Transform == null) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector2 movementDirection = inputManager.GetMovementDirection();

            // Applica il movimento se il giocatore è vivo
            if (player.Health.CurrentHealth > 0)
            {
                MoveEntity(player, movementDirection, deltaTime);
            }
        }

        /// <summary>
        /// Muove un'entità nella direzione specificata
        /// </summary>
        public void MoveEntity(Entity entity, Vector2 direction, float deltaTime)
        {
            if (entity?.Movement == null || entity.Transform == null) return;

            // Calcola la velocità effettiva considerando i modificatori
            float currentSpeed = entity.Movement.Speed * entity.Movement.SpeedMultiplier;
            
            // Calcola il movimento
            Vector2 movement = direction * currentSpeed * deltaTime;
            
            // Applica il movimento
            entity.Transform.Position += movement;
            
            // Aggiorna la direzione di movimento
            if (direction != Vector2.Zero)
            {
                entity.Movement.LastDirection = direction;
                entity.Movement.IsMoving = true;
            }
            else
            {
                entity.Movement.IsMoving = false;
            }

            // Aggiorna la rotazione se l'entità dovrebbe ruotare verso la direzione di movimento
            if (entity.Movement.RotateTowardsMovement && direction != Vector2.Zero)
            {
                float targetRotation = (float)System.Math.Atan2(direction.Y, direction.X);
                
                if (entity.Movement.SmoothRotation)
                {
                    // Interpolazione fluida della rotazione
                    float rotationSpeed = entity.Movement.RotationSpeed * deltaTime;
                    entity.Transform.Rotation = LerpAngle(entity.Transform.Rotation, targetRotation, rotationSpeed);
                }
                else
                {
                    entity.Transform.Rotation = targetRotation;
                }
            }
        }

        /// <summary>
        /// Muove un'entità verso una posizione target
        /// </summary>
        public bool MoveTowardsTarget(Entity entity, Vector2 targetPosition, float deltaTime, float stoppingDistance = 0f)
        {
            if (entity?.Movement == null || entity.Transform == null) return false;

            Vector2 direction = targetPosition - entity.Transform.Position;
            float distance = direction.Length();

            // Controlla se abbiamo raggiunto il target
            if (distance <= stoppingDistance)
            {
                entity.Movement.IsMoving = false;
                return true; // Target raggiunto
            }

            // Normalizza la direzione
            direction.Normalize();

            // Muovi verso il target
            MoveEntity(entity, direction, deltaTime);

            return false; // Target non ancora raggiunto
        }

        /// <summary>
        /// Applica una forza di knockback a un'entità
        /// </summary>
        public void ApplyKnockback(Entity entity, Vector2 knockbackDirection, float force, float deltaTime)
        {
            if (entity?.Movement == null || entity.Transform == null) return;

            Vector2 knockbackMovement = knockbackDirection * force * deltaTime;
            entity.Transform.Position += knockbackMovement;

            // Riduce gradualmente il knockback
            entity.Movement.KnockbackForce *= 0.9f;
            
            if (entity.Movement.KnockbackForce < 0.1f)
            {
                entity.Movement.KnockbackForce = 0f;
            }
        }

        /// <summary>
        /// Movimento con accelerazione e decelerazione
        /// </summary>
        public void UpdateSmoothMovement(Entity entity, Vector2 targetDirection, float deltaTime)
        {
            if (entity?.Movement == null || entity.Transform == null) return;

            float acceleration = entity.Movement.Acceleration;
            float deceleration = entity.Movement.Deceleration;
            float maxSpeed = entity.Movement.Speed * entity.Movement.SpeedMultiplier;

            // Calcola la velocità target
            Vector2 targetVelocity = targetDirection * maxSpeed;

            // Interpola verso la velocità target
            if (targetDirection != Vector2.Zero)
            {
                // Accelerazione
                entity.Movement.Velocity = Vector2.Lerp(
                    entity.Movement.Velocity, 
                    targetVelocity, 
                    acceleration * deltaTime
                );
            }
            else
            {
                // Decelerazione
                entity.Movement.Velocity = Vector2.Lerp(
                    entity.Movement.Velocity, 
                    Vector2.Zero, 
                    deceleration * deltaTime
                );
            }

            // Applica la velocità alla posizione
            entity.Transform.Position += entity.Movement.Velocity * deltaTime;

            // Aggiorna lo stato di movimento
            entity.Movement.IsMoving = entity.Movement.Velocity.LengthSquared() > 0.01f;
            
            if (entity.Movement.IsMoving)
            {
                entity.Movement.LastDirection = Vector2.Normalize(entity.Movement.Velocity);
            }
        }

        /// <summary>
        /// Movimento pattuglia tra due punti
        /// </summary>
        public void UpdatePatrolMovement(Entity entity, Vector2 pointA, Vector2 pointB, float deltaTime)
        {
            if (entity?.Movement == null || entity.Transform == null) return;

            Vector2 currentTarget = entity.Movement.PatrolTargetA ? pointA : pointB;
            
            if (MoveTowardsTarget(entity, currentTarget, deltaTime, 5f))
            {
                // Cambia direzione quando raggiunge il target
                entity.Movement.PatrolTargetA = !entity.Movement.PatrolTargetA;
                
                // Pausa opzionale al punto di pattuglia
                entity.Movement.PatrolPauseTimer = entity.Movement.PatrolPauseDuration;
            }

            // Gestisce la pausa
            if (entity.Movement.PatrolPauseTimer > 0)
            {
                entity.Movement.PatrolPauseTimer -= deltaTime;
                entity.Movement.IsMoving = false;
            }
        }

        /// <summary>
        /// Movimento circolare attorno a un punto
        /// </summary>
        public void UpdateCircularMovement(Entity entity, Vector2 centerPoint, float radius, float angularSpeed, float deltaTime)
        {
            if (entity?.Movement == null || entity.Transform == null) return;

            // Incrementa l'angolo
            entity.Movement.CircularAngle += angularSpeed * deltaTime;

            // Calcola la nuova posizione
            Vector2 offset = new Vector2(
                (float)System.Math.Cos(entity.Movement.CircularAngle) * radius,
                (float)System.Math.Sin(entity.Movement.CircularAngle) * radius
            );

            entity.Transform.Position = centerPoint + offset;
            entity.Movement.IsMoving = true;

            // Aggiorna la direzione per il rendering
            Vector2 tangent = new Vector2(
                -(float)System.Math.Sin(entity.Movement.CircularAngle),
                (float)System.Math.Cos(entity.Movement.CircularAngle)
            );
            entity.Movement.LastDirection = tangent;
        }

        /// <summary>
        /// Applica attrito al movimento
        /// </summary>
        public void ApplyFriction(Entity entity, float frictionCoefficient, float deltaTime)
        {
            if (entity?.Movement == null) return;

            float friction = 1f - (frictionCoefficient * deltaTime);
            friction = MathHelper.Max(0f, friction);

            entity.Movement.Velocity *= friction;
        }

        /// <summary>
        /// Interpolazione angolare per rotazioni fluide
        /// </summary>
        private float LerpAngle(float from, float to, float t)
        {
            float difference = to - from;
            
            // Normalizza la differenza per prendere il percorso più breve
            while (difference > MathHelper.Pi)
                difference -= MathHelper.TwoPi;
            while (difference < -MathHelper.Pi)
                difference += MathHelper.TwoPi;

            return from + difference * t;
        }

        /// <summary>
        /// Controlla se un'entità può muoversi in una direzione (per prevenire movimenti non validi)
        /// </summary>
        public bool CanMove(Entity entity, Vector2 direction, float distance = 1f)
        {
            if (entity?.Transform == null) return false;

            // Calcola la posizione futura
            Vector2 futurePosition = entity.Transform.Position + direction * distance;

            // Qui potresti aggiungere controlli per collisioni o limiti del mondo
            // Per ora ritorna sempre true
            return true;
        }
    }
}