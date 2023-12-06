using Bert;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace BertApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BertVM bertVM;
        public MainWindow()
        {
            InitializeComponent();

            bertVM = new BertVM();
            var destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BertAppHistory\\work_history";
            tabs.ItemsSource = bertVM.Texts;
            if (File.Exists(destinationPath))
            {
                BertVM.Load(destinationPath, ref bertVM);
                tabs.ItemsSource = bertVM.Texts;
            }
            InitializeSaving();
        }

        private void ChooseFile_btn(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                var filepath = openFileDialog.FileName;
                string text;
                try
                {
                    text = File.ReadAllText(filepath);
                    var elem = new OneText(text);
                    bertVM.Texts.Add(elem);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }
            }
        }

        private void DeleteHistory(object sender, RoutedEventArgs e)
        {
            var destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BertAppHistory\\work_history";
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            bertVM.Texts.Clear();
        }

        private void SaveHistory_btn(object sender, RoutedEventArgs e)
        {
            SaveHistory();
        }
        private void SaveHistory()
        {
            var destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BertAppHistory";
            if (Directory.Exists(destinationPath))
            {
                bertVM.Save(destinationPath + "\\work_history");
                return;
            }
            Directory.CreateDirectory(destinationPath);
            bertVM.Save(destinationPath + "\\work_history");
            return;
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var toRemove = ((sender as Button).DataContext as OneText);
            bertVM.Texts.Remove(toRemove);
        }
        public static T FindChild<T>(DependencyObject parent, string childName)
   where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
        private void QuestionBtnClick(object sender, RoutedEventArgs e)
        {
            var btn = (sender as Button);
            var parentElement = VisualTreeHelper.GetParent(btn);
            OneText whoAsked = btn.DataContext as OneText;
            var i = bertVM.Texts.IndexOf(whoAsked);
            var qst = FindChild<TextBox>(parentElement, "qst").Text;
            bertVM.Texts[i].Question = qst;
            //MessageBox.Show("Analyzing question: " + qst);
        }

        // The `onTick` method will be called periodically unless cancelled.
        private static async Task RunPeriodicAsync(Action onTick,
                                                   TimeSpan dueTime,
                                                   TimeSpan interval,
                                                   CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                // Call our onTick function.
                onTick?.Invoke();

                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }

        private void InitializeSaving()
        {
            var dueTime = TimeSpan.FromSeconds(5);
            var interval = TimeSpan.FromSeconds(5);

            // TODO: Add a CancellationTokenSource and supply the token here instead of None.
            RunPeriodicAsync(SaveHistory, dueTime, interval, Analyzer.cts.Token);
        }
    }
}
