using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace SimplePasswordManager
{
    internal class Program
    {
        static void Main()
        {
            Init.LoadConfig();

            Console.WriteLine("*********************************");
            Console.WriteLine("      SimplePasswordManager      ");
            Console.WriteLine("          by alpenstorm          ");
            Console.WriteLine("*********************************");
            Console.Write("Press any key to start: ");
            Console.ReadKey(true);
            Globals.ClearConsole();
            
            if (Globals.isFirstLoad) { Init.FirstLoad(); }

            Init.Unlock();
            Loop.MainLoop();
        }
    }
}