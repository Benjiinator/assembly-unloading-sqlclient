using Contract;
using Shared;
using System;

namespace Plugin
{
    public class Main : IModule
    {
        public string Name => "Module";


        public void Initialize()
        {
            Console.WriteLine("Starting plugin");
        }

        public void Execute()
        {
            Console.WriteLine("Executing plugin");
            var sqlLogic = new SqlLogic();
            sqlLogic.CreateConnection();
        }

        public void Close()
        {
            Console.WriteLine("Closing plugin");
        }


    }
}
