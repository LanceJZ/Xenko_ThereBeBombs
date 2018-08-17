using Xenko.Engine;

namespace ThereBeBombs
{
    class ThereBeBombsApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
