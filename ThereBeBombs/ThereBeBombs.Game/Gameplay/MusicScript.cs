// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Audio;
using Xenko.Engine;

namespace ThereBeBombs.Gameplay
{
    /// <summary>
    /// The main script in charge of the sound.
    /// </summary>
    public class MusicScript : AsyncScript
    {
        public Sound SoundMusic { get; set; }

        SoundInstance music;

        public override async Task Execute()
        {
            //music = SoundMusic.CreateInstance();

            if (!IsLiveReloading)
            {
                // start ambient music
                //music.IsLooping = true;
                //music.Play();
            }

            while (Game.IsRunning)
            {
                // wait for next frame
                //await Script.NextFrame();
            }
        }
    }
}
