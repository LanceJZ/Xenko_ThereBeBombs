// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Navigation;
using Xenko.Physics;
using ThereBeBombs.Core;
using ThereBeBombs.Gameplay.Enemies;

namespace ThereBeBombs.Player
{
    public class PlayerController : SyncScript
    {
        // The character controller does only two things - moves the character and makes it attack close targets
        //  If the character is too far from its target it will run after it until it's close enough then halt movement and attack
        //  If the character is walking towards a specific location instead it will run to it then halt movement when close enough

        readonly EventReceiver<ClickResult> MoveDestinationEvent =
            new EventReceiver<ClickResult>(PlayerInput.MoveDestinationEventKey);

        /// <summary>
        /// The maximum speed the character can run at
        /// </summary>
        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 10;
        /// <summary>
        /// The distance from the destination at which the character will stop moving
        /// </summary>
        public float DestinationThreshold { get; set; } = 0.2f;
        /// <summary>
        /// A number from 0 to 1 indicating how much a character should slow down when going around corners
        /// </summary>
        /// <remarks>0 is no slowdown and 1 is completely stopping (on >90 degree angles)</remarks>
        public float CornerSlowdown { get; set; } = 0.6f;
        /// <summary>
        /// Multiplied by the distance to the target and clamped to 1 and used to slow down when nearing the destination
        /// </summary>
        public float DestinationSlowdown { get; set; } = 0.4f;
        //// The PlayerController will propagate its speed to the AnimationController
        //public static readonly EventKey<float> RunSpeedEventKey = new EventKey<float>();
        // Allow some inertia to the movement
        Vector3 MoveDirection = Vector3.Zero;
        bool isRunning = false;
        // Attacking
        //[Display("Punch Collision")]
        //public RigidbodyComponent PunchCollision { get; set; }
        /// <summary>
        /// The maximum distance from which the character can perform an attack
        /// </summary>
        [Display("Attack Distance")]
        public float AttackDistance { get; set; } = 1f;
        /// <summary>
        /// Cooldown in seconds required for the character to recover from starting an attack until it can choose another action
        /// </summary>
        [Display("Attack Cooldown")]
        public float AttackCooldown { get; set; } = 0.65f;
        // The PlayerController will propagate if it is attacking to the AnimationController
        //public static readonly EventKey<bool> IsAttackingEventKey = new EventKey<bool>();
        //Character Component
        CharacterComponent Character;
        Entity ModelChildEntity;
        float YawOrientation;
        Core.Timer AttackTimer;
        Entity AttackEntity = null;
        // Pathfinding Component
        NavigationComponent Navigation;
        readonly List<Vector3> PathToDestination = new List<Vector3>();
        int WaypointIndex;
        Vector3 MoveToDestination;
        bool ReachedDestination => WaypointIndex >= PathToDestination.Count;
        Vector3 CurrentWaypoint => WaypointIndex < PathToDestination.Count ? PathToDestination[WaypointIndex] : Vector3.Zero;

        /// <summary>
        /// Called when the script is first initialized
        /// </summary>
        public override void Start()
        {
            base.Start();
            // Get the navigation component on the same entity as this script
            Navigation = Entity.Get<NavigationComponent>();
            // Will search for an CharacterComponent within the same entity as this script
            Character = Entity.Get<CharacterComponent>();

            if (Character == null) throw new ArgumentException("Please add a CharacterComponent to the entity containing PlayerController!");

            //if (PunchCollision == null) throw
            //        new ArgumentException("Please add a RigidbodyComponent as a PunchCollision to the entity containing PlayerController!");

            ModelChildEntity = Entity.GetChild(0);
            MoveToDestination = Entity.Transform.WorldMatrix.TranslationVector;
            //PunchCollision.Enabled = false;

            Entity atimerE = new Entity { new Core.Timer() };
            SceneSystem.SceneInstance.RootScene.Entities.Add(atimerE);
            AttackTimer = atimerE.Get<Core.Timer>();
        }

        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {

            Move(MaxRunSpeed);

        }

        public void Hit()
        {
            //Action for being hit.

            System.Diagnostics.Debug.WriteLine("Player has been hit.");
        }

        void Attack()
        {
            if (AttackEntity == null)
                return;

            if (isRunning)
                return;

            Vector3 directionToCharacter = AttackEntity.Transform.WorldMatrix.TranslationVector -
                                       ModelChildEntity.Transform.WorldMatrix.TranslationVector;
            directionToCharacter.Y = 0;
            float currentDistance = directionToCharacter.Length();

            if (currentDistance <= AttackDistance)
            {
                // Attack!
                HaltMovement();

                if (AttackEntity.Get<CockroachScript>() != null)
                    AttackEntity.Get<CockroachScript>().Hit();

                AttackEntity = null;
                AttackTimer.Reset(AttackCooldown);
                //PunchCollision.Enabled = true;
                //IsAttackingEventKey.Broadcast(true);
            }
            else
            {
                directionToCharacter.Normalize();
                UpdateDestination(AttackEntity.Transform.WorldMatrix.TranslationVector);
            }
        }

        void HaltMovement()
        {
            isRunning = false;
            MoveDirection = Vector3.Zero;
            Character.SetVelocity(Vector3.Zero);
            MoveToDestination = ModelChildEntity.Transform.WorldMatrix.TranslationVector;
        }

        void UpdateDestination(Vector3 destination)
        {
            Vector3 delta = MoveToDestination - destination;

            if (delta.Length() > 0.01f) // Only recalculate path when the target position is different
            {
                // Generate a new path using the navigation component
                PathToDestination.Clear();

                if (Navigation.TryFindPath(destination, PathToDestination))
                {
                    // Skip the points that are too close to the player
                    WaypointIndex = 0;

                    while (!ReachedDestination && (CurrentWaypoint - Entity.Transform.WorldMatrix.TranslationVector).Length() < 0.25f)
                    {
                        WaypointIndex++;
                    }

                    // If this path still contains more points, set the player to running
                    if (!ReachedDestination)
                    {
                        isRunning = true;
                        MoveToDestination = destination;
                    }
                }
                else
                {
                    // Could not find a path to the target location
                    PathToDestination.Clear();
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
                if (PathToDestination.Count > 0 && WaypointIndex > 0 && WaypointIndex != PathToDestination.Count - 1)
                {
                    Vector3 pointNormal = CurrentWaypoint - PathToDestination[WaypointIndex-1];
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
                float moveSpeed = (MoveToDestination - Entity.Transform.WorldMatrix.TranslationVector).Length() * DestinationSlowdown;

                if (moveSpeed > 1.0f)
                    moveSpeed = 1.0f;

                // Slow down around corners
                float cornerSpeedMultiply = Math.Max(0.0f, Vector3.Dot(direction, MoveDirection)) * CornerSlowdown + (1.0f - CornerSlowdown);

                // Allow a very simple inertia to the character to make animation transitions more fluid
                MoveDirection = MoveDirection * 0.85f + direction * moveSpeed * cornerSpeedMultiply * 0.15f;
                Character.SetVelocity(MoveDirection * speed);

                // Broadcast speed as per cent of the max speed
                //RunSpeedEventKey.Broadcast(moveDirection.Length());

                // Character orientation
                if (MoveDirection.Length() > 0.001)
                {
                    YawOrientation = MathUtil.RadiansToDegrees((float) Math.Atan2(-MoveDirection.Z, MoveDirection.X) + MathUtil.PiOverTwo);
                }

                Rotate();
            }
            else
            {
                // No target
                HaltMovement();
            }
        }

        void RotateTowardsTarget(Vector3 pointDestination)
        {
            Vector3 direction = pointDestination - Entity.Transform.Position;

            YawOrientation = MathUtil.RadiansToDegrees((float) Math.Atan2(-direction.Z, direction.X) + MathUtil.PiOverTwo);

            Rotate();
        }

        void Rotate()
        {
            ModelChildEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(YawOrientation), 0, 0);
        }

        void Move(float speed)
        {
            if (!AttackTimer.Expired)
                return;

            // Character speed
            ClickResult clickResult;

            if (MoveDestinationEvent.TryReceive(out clickResult) && clickResult.Type != ClickType.Empty)
            {
                if (clickResult.Type == ClickType.Ground)
                {
                    AttackEntity = null;
                    UpdateDestination(clickResult.WorldPosition);
                }

                if (clickResult.Type == ClickType.Container)
                {
                    AttackEntity = clickResult.ClickedEntity;
                    Attack();
                }

                if (clickResult.Type == ClickType.Enemy)
                {
                    AttackEntity = clickResult.ClickedEntity;
                    RotateTowardsTarget(clickResult.WorldPosition);
                    Attack();

                    System.Diagnostics.Debug.WriteLine("Player Clicked Enemy.");
                }

                if (clickResult.Type == ClickType.Door)
                {
                    AttackEntity = null;
                    System.Diagnostics.Debug.WriteLine("Player Clicked Door.");
                }
            }

            if (!isRunning)
            {
                //RunSpeedEventKey.Broadcast(0);
                return;
            }

            UpdateMoveTowardsDestination(speed);
        }
    }
}
