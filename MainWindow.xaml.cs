using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

//Библиотеки, подключенные для:
using Microsoft.Win32;                  //использования OFileDialog
using System.IO;                        //работы с файлами
using System.Numerics;                  //использование контейнера List
using System.ComponentModel;            //возможность переопределения закрытия окна
//using System.Collections.Concurrent;    //использование функции поиска в List
//using System.Collections.Generic;       //использование функции удаления из List

namespace BookReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public struct Progress
    {
        public Progress(int p, double z)
        {
            page = p;
            zoom = z;
        }

        public int page { set; get; }
        public double zoom { set; get; }
    }

    public struct BookMark
    {
        public BookMark(string n, int p, double z, int i)
        {
            name = n;
            page = p;
            zoom = z;
            ind = i;
        }

        public int page { set; get; }
        public double zoom { set; get; }
        public int ind { set; get; }
        public string name { set; get; }
    }

    public partial class MainWindow : Window
    {
        FlowDocument document = new FlowDocument();
        string libFile = "library.txt";
        List<Progress> progs = new List<Progress>(); 
        List<BookMark> bookmarks = new List<BookMark>();
        List<BookMark> tempBM = null;
        int bookIndex = -1;  

        public MainWindow()
        {
            InitializeComponent();
            LoadLib();
            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
        }

        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {   
            SaveProgress();
            UpdateLib();
            e.Cancel = false;    
        }

        public void UpdateLib()
        {
            int countBM = 0;
            StreamWriter sw = new StreamWriter(libFile);
            for (int i = 0; i < cmbLib.Items.Count; i++)
            {
                sw.WriteLine(cmbLib.Items[i]);
                sw.WriteLine(progs[i].page);
                sw.WriteLine(progs[i].zoom);
                tempBM = null; countBM = 0;
                tempBM = bookmarks.FindAll(x => x.ind == i);
                if (tempBM != null)
                {
                    sw.WriteLine(tempBM.Count);
                    while(countBM < tempBM.Count)
                    {
                        sw.WriteLine(tempBM[countBM].name);
                        sw.WriteLine(tempBM[countBM].ind);
                        sw.WriteLine(tempBM[countBM].page);
                        sw.WriteLine(tempBM[countBM].zoom);
                        countBM++;
                    }
                }
            }
            sw.Close();
        }

        private void LoadLib()
        {
            StreamReader sr = new StreamReader(libFile);
            String temp = "";
            int countBM;
            Progress prog = new Progress(0,0);
            BookMark bookmark = new BookMark("", 0, 0, 0);
            temp = sr.ReadLine();
            while (temp != null)
            {
                //читаем название книги
                cmbLib.Items.Add(temp);
                //читаем прогресс
                temp = sr.ReadLine();
                prog.page = Convert.ToInt32(temp);
                temp = sr.ReadLine();
                prog.zoom= Convert.ToInt32(temp);
                progs.Add(prog);
                //читаем закладки
                temp = sr.ReadLine();
                countBM = Convert.ToInt32(temp);
                for(int i = 0;i < countBM; i++)
                {
                    temp = sr.ReadLine();
                    bookmark.name = Convert.ToString(temp);
                    temp = sr.ReadLine();
                    bookmark.ind = Convert.ToInt32(temp);
                    temp = sr.ReadLine();
                    bookmark.page = Convert.ToInt32(temp);
                    temp = sr.ReadLine();
                    bookmark.zoom = Convert.ToInt32(temp);

                    bookmarks.Add(bookmark);
                }
                temp = sr.ReadLine();
            }
        }

        private void setBookMarks()
        {
            int countBM = 0;
            cmbBookmark.Items.Clear();
            tempBM = null;
            tempBM = bookmarks.FindAll(x => x.ind == bookIndex);
            if (tempBM == null) return;
            while(countBM < tempBM.Count)
            {
                cmbBookmark.Items.Add(tempBM[countBM++].name);
            }
        }

        private void RestoreProgress()
        {
            textFlower.Zoom = progs[bookIndex].zoom;  //устанавливаем зум
            textFlower.GoToPage(progs[bookIndex].page); //открывается страница
        }

        public void SaveProgress()
        {
            if (bookIndex == -1) return;
            Progress prog = progs[bookIndex];
            prog.zoom = textFlower.Zoom;
            prog.page = textFlower.MasterPageNumber;
            progs[bookIndex] = prog;
        }

        private void btnAddBook_Click(object sender, RoutedEventArgs e)
        {
            SaveProgress(); 
            string filenames = "";
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Text Files (*.txt)|*.txt|RichText Files (*.rtf)|*.rtf|All files (*.*)|*.*";
            fileDialog.Multiselect = true;
            fileDialog.DefaultExt = ".txt";
            //Nullable<bool> dialogOK = fileDialog.ShowDialog();

            if (fileDialog.ShowDialog() == true)
            {
                foreach (string sFilename in fileDialog.FileNames)
                {
                    filenames += ";" + sFilename;
                }

                filenames = filenames.Substring(1);

                cmbLib.Items.Add(filenames);
            }
            Progress prog = new Progress(1, 100);
            progs.Add(prog);
            UpdateLib();
            //MessageBox.Show("Кнопка нажата");
        }

        private void btnOpenBook_Click(object sender, RoutedEventArgs e)
        {   
            if(bookIndex != -1)
                if (MessageBox.Show("Обновить прогресс чтения?", "Внимание, вопрос", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes) 
                    SaveProgress(); 
            if (cmbLib.Text == "") return;
            bookIndex = cmbLib.SelectedIndex;
            btnAddBookmark.IsEnabled = true;
            cmbBookmark.IsEnabled = true;
            btnGoToBookMark.IsEnabled = true;   
            btnDelBookMark.IsEnabled = true;    
            readingNow.Text = cmbLib.Text;
            setBookMarks();
            TextRange txtRange = new TextRange(document.ContentStart, document.ContentEnd);
            using (var fs = new FileStream(readingNow.Text, FileMode.Open))
            {
                if (Path.GetExtension(readingNow.Text).ToLower() == ".rtf")
                    txtRange.Load(fs, DataFormats.Rtf);
                else if (Path.GetExtension(readingNow.Text).ToLower() == ".txt")
                    txtRange.Load(fs, DataFormats.Text);
            }
            textFlower.Document = document;
            if (MessageBox.Show("Восстановить прогресс чтения?", "Внимание, вопрос", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                RestoreProgress();
            else textFlower.FirstPage();
        }

        private void btnAddBookmark_Click(object sender, RoutedEventArgs e)
        {
            BookMark bookmark = new BookMark((tempBM.Count+1).ToString(), textFlower.MasterPageNumber, textFlower.Zoom, bookIndex);
            tempBM.Add(bookmark);
            bookmarks.Add(bookmark);
            cmbBookmark.Items.Add(bookmark.name);
        }

        private void goToBM_Click(object sender, RoutedEventArgs e)
        {
            textFlower.Zoom = tempBM[cmbBookmark.SelectedIndex].zoom;  //устанавливаем зум
            textFlower.GoToPage(tempBM[cmbBookmark.SelectedIndex].page); //открывается страница
        }

        private void btnDelBook_Click(object sender, RoutedEventArgs e)
        {
            if (cmbLib.Text == readingNow.Text)
            {
                textFlower.Document = null;
                cmbBookmark.Items.Clear();
                readingNow.Text = "";
                bookIndex = -1;
                btnAddBookmark.IsEnabled = false;
                cmbBookmark.IsEnabled = false;
                cmbBookmark.Items.Clear();
                btnGoToBookMark.IsEnabled = false;
                btnDelBookMark.IsEnabled = false;
            }
            int index = cmbLib.SelectedIndex, i=0;
            progs.RemoveAt(index);
            while(i < bookmarks.Count)
            {
                if(bookmarks[i].ind == index)
                {
                    bookmarks.RemoveAt(i);
                }
                i++;
            }
            cmbLib.Items.Remove(cmbLib.SelectedItem);
        }

        private void btnDelBookMark_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (i < bookmarks.Count)
            {
                if (bookmarks[i].ind == cmbLib.SelectedIndex && bookmarks[i].name == cmbBookmark.Text)
                {
                    bookmarks.RemoveAt(i);
                }
                i++;
            }
            tempBM.RemoveAt(cmbBookmark.SelectedIndex);
            cmbBookmark.Items.Remove(cmbBookmark.SelectedItem);

        }
    }
}
