using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Engine;
using Xenko.Physics;
using ThereBeBombs.Gameplay.Enemies;

namespace ThereBeBombs.Player
{
    public class PlayerCollider : AsyncScript
    {
        public RigidbodyComponent Trigger;

        public override async Task Execute()
        {
            Trigger.ProcessCollisions = true;

            while (Game.IsRunning)
            {
                Collision firstCollision = await Trigger.NewCollision();

                PhysicsComponent otherCollider = Trigger == firstCollision.ColliderA ? firstCollision.ColliderB : firstCollision.ColliderA;

                CockroachScript isCockroach = otherCollider.Entity.Get<CockroachScript>();

                if (isCockroach != null)
                    CollisionStarted();
            }
        }

        protected void CollisionStarted()
        {
            //Action for being hit.

            Entity.GetParent().Get<PlayerController>().Hit();
        }
    }
}
