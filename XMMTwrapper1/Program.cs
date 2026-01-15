namespace XMMTwrapper1
{
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>


        [STAThread]
        static void Main()
        {
            var state = Thread.CurrentThread.GetApartmentState();
            System.Diagnostics.Debug.WriteLine($"ApartmentState: {state}");

            Log.Start();
            Log.EnableDebugOutput(true);
            // Log.SetLogFile("startup.log", enableFile: true); // optional


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Trace.WriteLine("Before Form1");
            var form = new Form1();
            //Trace.WriteLine("Before Run");
            Application.Run(form);
            //Trace.WriteLine("After Run");
        }
    }
}