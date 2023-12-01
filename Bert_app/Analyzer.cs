using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bert;

namespace BertApp
{
    internal class Analyzer
    {
        public static CancellationTokenSource cts = new();
        //private static bool cancelFlag = false;
        public static bool notInitialized = true;
        public static TaskFactory factory = new();
        public static async Task<string> GetAnswerAsync(string question, string text)
        {
            if (notInitialized)
            {
                await TextAnalyzer.CreateAsync(cts.Token);
                notInitialized = false;
            }
            var a = await TextAnalyzer.GetAnswerAsync(question, text);
            return a;
        }
    }
}
