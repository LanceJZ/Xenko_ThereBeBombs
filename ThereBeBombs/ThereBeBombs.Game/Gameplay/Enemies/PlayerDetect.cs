using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xenko.Engine.Events;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Engine;
using Xenko.Physics;

namespace ThereBeBombs.Gameplay.Enemies
{
    public class PlayerDetect : AsyncScript
    {
        public PhysicsComponent Trigger;

        public override async Task Execute()
        {
            Trigger.ProcessCollisions = true;

            while (Game.IsRunning)
            {
                Collision firstCollision = await Trigger.NewCollision();

                PhysicsComponent otherCollider = Trigger == firstCollision.ColliderA ? firstCollision.ColliderB : firstCollision.ColliderA;

                Player.PlayerController isPlayer = otherCollider.Entity.Get<Player.PlayerController>();

                if (isPlayer != null)
                    CollisionStarted(otherCollider.Entity.Transform.Position);

                Collision collision;

                do
                {
                    collision = await Trigger.CollisionEnded();
                }
                while (collision != firstCollision);

                if (isPlayer != null)
                    CollisionStarted(Vector3.Zero);
            }
        }

        protected void CollisionStarted(Vector3 playerPostion)
        {
            //Action for being hit.
            if (playerPostion != Vector3.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Player in collider.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Player out of collider.");
            }

            Entity.GetParent().Get<CockroachScript>().SpotsPlayer(playerPostion);
        }
    }
}
