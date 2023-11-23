using Bert;
using System.IO;


namespace MyApp
{
    class ConsoleUtility
    {
        const char _block = '■';
        const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        const string _twirl = "-\\|/";

        public static void WriteProgressBar(int percent, bool update = false)
        {
            if (update)
                Console.Write(_back);
            Console.Write("[");
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    Console.Write(' ');
                else
                    Console.Write(_block);
            }
            Console.Write("] {0,3:##0}%", percent);
        }

        public static void WriteProgress(int progress, bool update = false)
        {
            if (update)
                Console.Write("\b");
            Console.Write(_twirl[progress % _twirl.Length]);
        }

        public static void ProgressBar(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            //Console.WriteLine($"Download: {progressPercentage}%, ({totalBytesDownloaded}//{totalFileSize})");
            int i = (int)progressPercentage;
            ConsoleUtility.WriteProgressBar(i, true);
        }
    }

    internal class Program
    {
        private static CancellationTokenSource cts = new();
        private static bool cancelFlag = false;
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Give file path as argument");
                return;
            }

            string path = args[0];
            if (!File.Exists(path))
            {
                Console.WriteLine("File does not exist");
                return ;
            }

            string text;

            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            TaskFactory factory = new();

            Console.WriteLine("Initializing model and checking for neccesary files. Please, wait.\n");
            await TextAnalyzer.CreateAsync(cts.Token, ConsoleUtility.ProgressBar);

            Console.WriteLine("\nModel is ready.\n");

            Console.WriteLine("Text:\n\n");
            Console.WriteLine(text + "\n");

            List<Task> TaskList = new();
            string qst = Console.ReadLine();
            if (cancelFlag)
            {
                return;
            }
            while (!string.IsNullOrEmpty(qst))
            {
                var getAnsTask = factory.StartNew(() => Console.WriteLine(TextAnalyzer.GetAnswerAsync(qst, text).Result),
                                                cts.Token);
                TaskList.Add(getAnsTask);
                qst = Console.ReadLine();
                if (cancelFlag)
                {
                    return;
                }
            }
            Console.WriteLine("Questions input is finished. Waiting for answers.\n");
            try
            {
                Task.WaitAll(TaskList.ToArray(), cts.Token);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine("Program terminated.");
                return;
            }
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            cancelFlag = true;
            cts.Cancel();
            Console.WriteLine("Cancel Key pressed. Terminating program.");
            
        }
    }
}