using CityGame;
using System;
using System.ServiceModel;

namespace GameHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IGameService gameService = new GameService();
            gameService.ReadData();
            using (var host = new ServiceHost(gameService))
            {
                host.Open();
                Console.WriteLine("Хост запущен");
                
                Console.ReadLine();
            }
        }
    }
}
