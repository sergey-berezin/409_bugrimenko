using BERTTokenizers;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Buffers;

namespace Bert
{
    public delegate void DownloadProgressHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

    public static class DownloadWithProgress
    {
        public static async Task ExecuteAsync(HttpClient httpClient, string downloadPath, string destinationPath, 
                                              DownloadProgressHandler progress, CancellationToken token, 
                                              Func<HttpRequestMessage> requestMessageBuilder = null)
        {
            requestMessageBuilder ??= GetDefaultRequestBuilder(downloadPath);
            var download = new HttpClientDownloadWithProgress(httpClient, destinationPath, token, requestMessageBuilder);
            download.ProgressChanged += progress;
            await download.StartDownload();
            download.ProgressChanged -= progress;
        }

        private static Func<HttpRequestMessage> GetDefaultRequestBuilder(string downloadPath)
        {
            return () => new HttpRequestMessage(HttpMethod.Get, downloadPath);
        }
    }

    internal class HttpClientDownloadWithProgress
    {
        private readonly HttpClient _httpClient;
        private readonly string _destinationFilePath;
        private readonly Func<HttpRequestMessage> _requestMessageBuilder;
        private CancellationToken _token;
        private int _bufferSize = 8192;

        public event DownloadProgressHandler ProgressChanged;

        public HttpClientDownloadWithProgress(HttpClient httpClient, string destinationFilePath, CancellationToken token, Func<HttpRequestMessage> requestMessageBuilder)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _destinationFilePath = destinationFilePath ?? throw new ArgumentNullException(nameof(destinationFilePath));
            _token = token;
            _requestMessageBuilder = requestMessageBuilder ?? throw new ArgumentNullException(nameof(requestMessageBuilder));
        }

        public async Task StartDownload()
        {
            using var requestMessage = _requestMessageBuilder.Invoke();
            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            await DownloadAsync(response);
        }

        private async Task DownloadAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync(_token))
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            var isMoreToRead = true;

            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, true))
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        ReportProgress(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 100 == 0)
                        ReportProgress(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        private void ReportProgress(long? totalDownloadSize, long totalBytesRead)
        {
            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
        }
    }
    public class BertInput
    {
        public long[] InputIds { get; set; }
        public long[] AttentionMask { get; set; }
        public long[] TypeIds { get; set; }
    }
    public class TextAnalyzer
    {
        private static string modelPath;
        private static object modelLock = new();
        private TextAnalyzer(string onnxFilePath)
        {
            modelPath = onnxFilePath;
        }
        public static async Task<TextAnalyzer> CreateAsync(CancellationToken token, DownloadProgressHandler downloadProgress = null)
        {
            var client = new HttpClient();
            var downloadPath = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
            var destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BertModel";
            if (Directory.Exists(destinationPath))
            {
                if (File.Exists(destinationPath + "\\bert-large-uncased-whole-word-masking-finetuned-squad.onnx"))
                {
                    return new TextAnalyzer(destinationPath + "\\bert-large-uncased-whole-word-masking-finetuned-squad.onnx");
                }
            }
            
            Directory.CreateDirectory(destinationPath);

            await DownloadWithProgress.ExecuteAsync(client, downloadPath, 
                                                    destinationPath+ "\\bert-large-uncased-whole-word-masking-finetuned-squad.onnx", 
                                                    downloadProgress, token);
            TextAnalyzer textAnalyzer = new TextAnalyzer(destinationPath + "\\bert-large-uncased-whole-word-masking-finetuned-squad.onnx");
            return textAnalyzer;
        }
        public static async Task<string> GetAnswerAsync(string question, string context)
        {
            var sentence = "{\"question\": \"@QUESTION\", \"context\": \"@CTX\"}".Replace("@CTX", context);
            sentence = sentence.Replace("@QUESTION", question);
            //Console.WriteLine(sentence);

            // Create Tokenizer and tokenize the sentence.
            var tokenizer = new BertUncasedLargeTokenizer();

            // Get the sentence tokens.
            var tokens = tokenizer.Tokenize(sentence);
            //Console.WriteLine(String.Join(", ", tokens));

            // Encode the sentence and pass in the count of the tokens in the sentence.
            var encoded = tokenizer.Encode(tokens.Count(), sentence);

            // Break out encoding to InputIds, AttentionMask and TypeIds from list of (input_id, attention_mask, type_id).
            var bertInput = new BertInput()
            {
                InputIds = encoded.Select(t => t.InputIds).ToArray(),
                AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
            };

            // Create input tensor.

            var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
            var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
            var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);


            // Create input data for session.
            var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
                                                    NamedOnnxValue.CreateFromTensor("input_mask", attention_mask),
                                                    NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids) };
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> output = null;

            lock (modelLock)
            {
                // Create an InferenceSession from the Model Path.
                var session = new InferenceSession(modelPath);

                // Run session and send the input data in to get inference output. 
                output = session.Run(input); //С флагом long running task
            }

            // Call ToList on the output.
            // Get the First and Last item in the list.
            // Get the Value of the item and cast as IEnumerable<float> to get a list result.
            List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
            List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

            // Get the Index of the Max value from the output lists.
            var startIndex = startLogits.ToList().IndexOf(startLogits.Max());
            var endIndex = endLogits.ToList().IndexOf(endLogits.Max());

            // From the list of the original tokens in the sentence
            // Get the tokens between the startIndex and endIndex and convert to the vocabulary from the ID of the token.
            var predictedTokens = tokens
                        .Skip(startIndex)
                        .Take(endIndex + 1 - startIndex)
                        .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                        .ToList();

            var answer = string.Join(" ", predictedTokens);
            var result = "\nQuestion: @QUESTION\nAnswer: @ANS\n".Replace("@ANS", answer);
            result = result.Replace("@QUESTION", question);

            // Return the result.
            return result;
        }

        private static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
        {
            // Create a tensor with the shape the model is expecting. Here we are sending in 1 batch with the inputDimension as the amount of tokens.
            Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

            // Loop through the inputArray (InputIds, AttentionMask and TypeIds)
            for (var i = 0; i < inputArray.Length; i++)
            {
                // Add each to the input Tenor result.
                // Set index and array value of each input Tensor.
                input[0, i] = inputArray[i];
            }
            return input;
        }
    }
}