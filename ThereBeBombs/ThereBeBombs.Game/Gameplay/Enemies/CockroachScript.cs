using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Navigation;
using Xenko.Physics;
using ThereBeBombs.Core;

namespace ThereBeBombs.Gameplay.Enemies
{
    public class CockroachScript : SyncScript
    {
        /// <summary>
        /// The maximum speed the enemy can run at
        /// </summary>
        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 10;
        /// <summary>
        /// The distance from the destination at which the enemy will stop moving
        /// </summary>
        public float DestinationThreshold { get; set; } = 0.2f;
        /// <summary>
        /// A number from 0 to 1 indicating how much a enemy should slow down when going around corners
        /// </summary>
        /// <remarks>0 is no slowdown and 1 is completely stopping (on >90 degree angles)</remarks>
        public float CornerSlowdown { get; set; } = 0.6f;
        /// <summary>
        /// Multiplied by the distance to the target and clamped to 1 and used to slow down when nearing the destination
        /// </summary>
        public float DestinationSlowdown { get; set; } = 0.4f;
        // Allow some inertia to the movement
        Vector3 moveDirection = Vector3.Zero;
        bool IsRunning = false;
        // Attacking
        //[Display("Bite Collision")]
        //public RigidbodyComponent BiteCollision { get; set; }
        [Display("Attack Distance")]
        public float AttackDistance { get; set; } = 0.25f;
        /// <summary>
        /// Cooldown in seconds required for the character to recover from starting an attack until it can choose another action
        /// </summary>
        [Display("Attack Cooldown")]
        public float AttackCooldown { get; set; } = 0.65f;
        Entity ModelEntity;
        float yawOrientation;
        Entity AttackEntity = null;
        // Pathfinding Component
        NavigationComponent navigation;
        readonly List<Vector3> pathToDestination = new List<Vector3>();
        int WaypointIndex;
        Vector3 MoveDestination;
        bool ReachedDestination => WaypointIndex >= pathToDestination.Count;
        Vector3 CurrentWaypoint => WaypointIndex < pathToDestination.Count ? pathToDestination[WaypointIndex] : Vector3.Zero;
        bool HasNewDestination;

        public override void Start()
        {
            base.Start();

            // Get the navigation component on the same entity as this script
            navigation = Entity.Get<NavigationComponent>();

            //if (BiteCollision == null) throw
            //        new ArgumentException("Please add a RigidbodyComponent as a BiteCollision to the enemy entity!");

            ModelEntity = Entity;
            MoveDestination = Entity.Transform.WorldMatrix.TranslationVector;
            //BiteCollision.Enabled = false;

            //Debug
            //this.GetSimulation().ColliderShapesRendering = true;
        }

        public override void Update()
        {
            if (!IsRunning)
            {
                Attack();
            }
            else
            {
                Move(MaxRunSpeed);
            }

        }

        public void SpotsPlayer(Vector3 playerPosition)
        {
            //Action for spotting player.
            System.Diagnostics.Debug.WriteLine("Cockroach has found player." + playerPosition);
        }

        protected void CollisionStarted()
        {
        }

        public void Hit()
        {
            System.Diagnostics.Debug.WriteLine("Cockroach has been hit.");

        }

        void Attack()
        {
            float elapsedTotalSeconds = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            AttackCooldown = (AttackCooldown > 0) ? AttackCooldown - elapsedTotalSeconds : 0f;
            //BiteCollision.Enabled = (AttackCooldown > 0);

            if (AttackEntity == null)
                return;

            Vector3 directionToCharacter = AttackEntity.Transform.WorldMatrix.TranslationVector -
                                       ModelEntity.Transform.WorldMatrix.TranslationVector;
            directionToCharacter.Y = 0;
            float currentDistance = directionToCharacter.Length();

            if (currentDistance <= AttackDistance)
            {
                // Attack!
                HaltMovement();

                AttackEntity = null;
                AttackCooldown = AttackCooldown;
                //BiteCollision.Enabled = true;
            }
            else
            {
                directionToCharacter.Normalize();
                UpdateDestination(AttackEntity.Transform.WorldMatrix.TranslationVector);
            }
        }

        void HaltMovement()
        {
            IsRunning = false;
            moveDirection = Vector3.Zero;
            //character.SetVelocity(Vector3.Zero);
            MoveDestination = ModelEntity.Transform.WorldMatrix.TranslationVector;
        }

        void UpdateDestination(Vector3 destination)
        {
            Vector3 delta = MoveDestination - destination;

            if (delta.Length() > 0.01f) // Only recalculate path when the target position is different
            {
                // Generate a new path using the navigation component
                pathToDestination.Clear();

                if (navigation.TryFindPath(destination, pathToDestination))
                {
                    // Skip the points that are too close to the enemy
                    WaypointIndex = 0;
                    while (!ReachedDestination && (CurrentWaypoint - Entity.Transform.WorldMatrix.TranslationVector).Length() < 0.25f)
                    {
                        WaypointIndex++;
                    }

                    // If this path still contains more points, set the enemy to running
                    if (!ReachedDestination)
                    {
                        IsRunning = true;
                        MoveDestination = destination;
                    }
                }
                else
                {
                    // Could not find a path to the target location
                    pathToDestination.Clear();
                    HaltMovement();
                }
            }
        }

        void UpdateMoveTowardsDestination(float speed)
        {
            if (!ReachedDestination)
            {
                Vector3 direction = CurrentWaypoint - Entity.Transform.WorldMatrix.TranslationVector;

                // Get distance towards next point and normalize the direction at the same time
                float length = direction.Length();
                direction /= length;

                // Check when to advance to the next waypoint
                bool advance = false;

                // Check to see if an intermediate point was passed by projecting the position along the path
                if (pathToDestination.Count > 0 && WaypointIndex > 0 && WaypointIndex != pathToDestination.Count - 1)
                {
                    Vector3 pointNormal = CurrentWaypoint - pathToDestination[WaypointIndex - 1];
                    pointNormal.Normalize();
                    float current = Vector3.Dot(Entity.Transform.WorldMatrix.TranslationVector, pointNormal);
                    float target = Vector3.Dot(CurrentWaypoint, pointNormal);

                    if (current > target)
                    {
                        advance = true;
                    }
                }
                else
                {
                    if (length < DestinationThreshold) // Check distance to final point
                    {
                        advance = true;
                    }
                }

                // Advance waypoint?
                if (advance)
                {
                    WaypointIndex++;
                    if (ReachedDestination)
                    {
                        // Final waypoint reached
                        HaltMovement();
                        return;
                    }
                }

                // Calculate speed based on distance from final destination
                float moveSpeed = (MoveDestination - Entity.Transform.WorldMatrix.TranslationVector).Length() * DestinationSlowdown;

                if (moveSpeed > 1.0f)
                    moveSpeed = 1.0f;

                // Slow down around corners
                float cornerSpeedMultiply = Math.Max(0.0f, Vector3.Dot(direction, moveDirection)) * CornerSlowdown + (1.0f - CornerSlowdown);

                // Allow a very simple inertia to the character to make animation transitions more fluid
                moveDirection = moveDirection * 0.85f + direction * moveSpeed * cornerSpeedMultiply * 0.15f;
                //character.SetVelocity(moveDirection * speed);
                Entity.GetParent().Transform.Position += (MoveDestination * speed);

                // Broadcast speed as per cent of the max speed
                //RunSpeedEventKey.Broadcast(moveDirection.Length());

                // Model orientation
                if (moveDirection.Length() > 0.001)
                {
                    yawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(-moveDirection.Z, moveDirection.X) + MathUtil.PiOverTwo);
                }

                ModelEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(yawOrientation), 0, 0);
            }
            else
            {
                // No target
                HaltMovement();
            }
        }

        void Move(float speed)
        {
            if (AttackCooldown > 0)
                return;

            if (HasNewDestination)
            {
                //if (clickResult.Type == ClickType.Ground)
                //{
                //    attackEntity = null;
                //    UpdateDestination(clickResult.WorldPosition);
                //}

                //if (clickResult.Type == ClickType.LootCrate)
                //{
                //    attackEntity = clickResult.ClickedEntity;
                //    Attack();
                //}
            }

            if (!IsRunning)
            {
                return;
            }

            UpdateMoveTowardsDestination(speed);
        }
    }
}
