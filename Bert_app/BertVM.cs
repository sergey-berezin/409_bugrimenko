using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace BertApp
{
    internal class OneText: INotifyPropertyChanged
    {
        public string Text { get; set; }
        [JsonProperty]
        private Dictionary<string, string> history = new Dictionary<string, string>();
        [JsonProperty]
        private string answer = null;
        [JsonProperty]
        private string question;
        public string Question
        {
            get { return question; }
            set
            {
                question = value;
                if (history.ContainsKey(question))
                {
                    Answer = history[question];
                    return;
                }
                var getAnsTask = Analyzer.factory.StartNew(async () =>
                {
                    var a = await Analyzer.GetAnswerAsync(Question, Text);
                    this.history.Add(question, a);
                    this.Answer = a;
                }, Analyzer.cts.Token);
            }
        }
        public string Answer
        {
            get { return answer; }
            set
            {
                answer = value;
                OnPropertyChanged(nameof(Answer));
            }
        }
        public OneText(string text)
        {
            Text = text;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
    [JsonObject(MemberSerialization.OptOut)]
    internal class BertVM: INotifyPropertyChanged
    {
        public ObservableCollection<OneText> Texts;
        public BertVM()
        {
            Texts = new ObservableCollection<OneText>();
            Texts.CollectionChanged += Texts_CollectionChanged;
        }

        public void Clear()
        {
            this.Texts = new ObservableCollection<OneText>();
            OnPropertyChanged(nameof (Texts));
        }

        private void Texts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Texts");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public bool Save(string filename)
        {
            FileStream? fs = null;
            try
            {
                var json =
                    Newtonsoft.Json.JsonConvert.SerializeObject(this);

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    File.WriteAllText(filename, json);
                }
                else
                {
                    File.WriteAllText(filename, json);
                }
                return true;
            }
            catch (Exception x)
            {
                MessageBox.Show($"Error Saving history: {x}");
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }
        public static bool Load(string filename, ref BertVM vm)
        {
            FileStream? fs = null;
            try
            {
                var json = File.ReadAllText(filename);
                vm = Newtonsoft.Json.JsonConvert.DeserializeObject<BertVM>(json);
                vm.OnPropertyChanged(nameof(Texts));
                return true;
            }
            catch (Exception x)
            {
                Console.WriteLine($"Error Loading history: {x.Message}");
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }
    }
}
