using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Bert;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace BertServer.Controllers
{
    public static class GlobalId
    {
        public static int maxId = 0;
    }
    [ApiController]
    [Route("[controller]")]
    public class BertAnalyzerController : ControllerBase
    {
        public static Dictionary<string, string> Texts = new Dictionary<string, string>();
        private static CancellationTokenSource cts = new();
        private TextAnalyzer analyzer = TextAnalyzer.CreateAsync(cts.Token).Result;
        private object lockObj = new();

        [HttpPost]
        public ActionResult<string> PostText([FromBody] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                string message = "No text found in request.";
                Console.WriteLine($"{message}\n");
                return BadRequest(message);
            }
            int id = 0;
            lock (lockObj)
            {
                Texts[GlobalId.maxId.ToString()] = text;
                id = GlobalId.maxId;
                Console.WriteLine(GlobalId.maxId);
                GlobalId.maxId++;
                Console.WriteLine(GlobalId.maxId);
            }
            Console.WriteLine($"Got POST request with text:\n{text} \n");
            return Ok(id.ToString());
        }

        [HttpGet]
        public async Task<ActionResult> Get(string textId, string question)
        {
            if (string.IsNullOrWhiteSpace(textId))
            {
                string message = "No text id found in request.";
                Console.WriteLine($"{message}\n");
                return BadRequest(message);
            }
            if (!Texts.ContainsKey(textId))
            {
                string message = "No text with such id found. Check id or send text again.";
                Console.WriteLine($"{message}\n");
                return BadRequest(message);
            }
            if (string.IsNullOrWhiteSpace(question))
            {
                string message = "No question found in request.";
                Console.WriteLine($"{message}\n");
                return BadRequest(message);
            }
            Console.WriteLine($"Got GET request with textId: {textId}.\nQuestion: {question} \n");
            var answer = await TextAnalyzer.GetAnswerAsync(question, Texts[textId]);
            return Ok(answer);
        }
    }
}