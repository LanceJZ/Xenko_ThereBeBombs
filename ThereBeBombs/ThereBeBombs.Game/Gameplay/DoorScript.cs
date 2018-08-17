using System;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Audio;
using Xenko.Engine;
using Xenko.Engine.Events;
using ThereBeBombs.Core;
using ThereBeBombs.Gameplay;

namespace Gameplay.Door
{
    public class DoorScript : SyncScript
    {
        public Trigger Trigger { get; set; }
        EventReceiver<bool> triggeredEvent;
        bool activated = false;

        public override void Update()
        {
            // Check if the door has been pushed on.
            bool triggered;

            if (!activated && (triggeredEvent?.TryReceive(out triggered) ?? false))
            {
                CollisionStarted();
            }

        }

        public override void Start()
        {
            base.Start();

            triggeredEvent = (Trigger != null) ? new EventReceiver<bool>(Trigger.TriggerEvent) : null;
        }

        protected void CollisionStarted()
        {
            activated = true;

            // Add visual effect
            Matrix effectMatrix = Matrix.Translation(Entity.Transform.WorldMatrix.TranslationVector);

            Entity.Transform.Position.Y = 3;

            //Func<Task> cleanupTask = async () =>
            //{
            //    await Game.WaitTime(TimeSpan.FromMilliseconds(500));

            //    Game.RemoveEntity(Entity);
            //};

            //Script.AddTask(cleanupTask);
        }
    }
}