// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Xenko.Core.Collections;
using Xenko.Engine;
using Xenko.Navigation;

namespace Gameplay
{
    public class EnableDynamicNavigation : StartupScript
    {
        public override void Start()
        {
            DynamicNavigationMeshSystem dynamicNavigationMeshSystem = Game.GameSystems.OfType<DynamicNavigationMeshSystem>().FirstOrDefault();

            // Wait for the dynamic navigation to be registered
            if (dynamicNavigationMeshSystem == null)
                Game.GameSystems.CollectionChanged += GameSystemsOnCollectionChanged;
            else
                dynamicNavigationMeshSystem.Enabled = true;
        }

        public override void Cancel()
        {
            Game.GameSystems.CollectionChanged -= GameSystemsOnCollectionChanged;
        }

        private void GameSystemsOnCollectionChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            if (trackingCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                if (trackingCollectionChangedEventArgs.Item is DynamicNavigationMeshSystem dynamicNavigationMeshSystem)
                {
                    dynamicNavigationMeshSystem.Enabled = true;

                    // No longer need to listen to changes
                    Game.GameSystems.CollectionChanged -= GameSystemsOnCollectionChanged;
                }
            }
        }
    }
}
