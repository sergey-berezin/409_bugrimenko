using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BertApp
{
    internal class OneText: INotifyPropertyChanged
    {
        public string Text { get; set; }
        private string answer = null;
        private string question;
        public string Question
        {
            get { return question; }
            set
            {
                question = value;
                var getAnsTask = Analyzer.factory.StartNew(async () =>
                {
                    var a = await Analyzer.GetAnswerAsync(Question, Text);
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
    internal class BertVM: INotifyPropertyChanged
    {
        /*
        public string text = null;
        private string answer = null;
        public string Question
        {
            get { return Question; }
            set
            {
                Question = value;
                var getAnsTask = Analyzer.factory.StartNew(async () =>
                {
                    var a = await Analyzer.GetAnswerAsync(Question, text);
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
                OnPropertyChanged("Answer");
            }
        }

        public BertVM()
        {
        }
        */
        public ObservableCollection<OneText> Texts;
        public BertVM()
        {
            Texts = new ObservableCollection<OneText>();
            Texts.CollectionChanged += Texts_CollectionChanged;
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
    }
}
