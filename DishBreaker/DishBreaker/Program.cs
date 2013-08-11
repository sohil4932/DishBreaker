using System;

namespace DishBreaker
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DishBreaker game = new DishBreaker())
            {
                game.Run();
            }
        }
    }
#endif
}

