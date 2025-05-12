namespace SimplePasswordManager
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
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

                Loop.MainLoop();

                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] Application started successfully.");
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in Main: {ex.Message}");
                }
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
        }
    }
}