using System;

namespace FootballManagementDemo
{
#if NET10_0
    public static class TopLevelMain
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("FootballManagementDemo (net10.0) top-level entry executing.");
            return 0;
        }
    }
#endif
}
