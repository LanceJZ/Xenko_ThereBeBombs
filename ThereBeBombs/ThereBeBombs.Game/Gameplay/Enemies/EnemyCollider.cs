using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Engine;
using Xenko.Physics;

namespace ThereBeBombs.Gameplay.Enemies
{
    public class EnemyCollider : AsyncScript
    {
        public RigidbodyComponent Trigger;
        Core.Timer AttackTimer;

        public override async Task Execute()
        {
            Trigger.ProcessCollisions = true;
            Entity atimerE = new Entity { new Core.Timer() };
            SceneSystem.SceneInstance.RootScene.Entities.Add(atimerE);
            AttackTimer = atimerE.Get<Core.Timer>();

            while (Game.IsRunning)
            {
                Collision firstCollision = await Trigger.NewCollision();

                PhysicsComponent otherCollider = Trigger == firstCollision.ColliderA ? firstCollision.ColliderB : firstCollision.ColliderA;

                Player.PlayerController isPlayer = otherCollider.Entity.Get<Player.PlayerController>();

                if (isPlayer != null)
                    CollisionStarted(isPlayer);
            }
        }

        protected void CollisionStarted(Player.PlayerController player)
        {
            //Action for hitting player.

            player.Hit();
        }
    }
}
