using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Data.SQLite;
using ControlHelper;

namespace SQLiteAsyncPicturesRetriever
{
    class FileDownloader : WebClient
    {
        public String url { set; get; }
        public String file { set; get; }
    }

    public partial class Form1 : Form
    {
        private Image PlaceHolder;
        private List<String> Urls;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Urls = new List<string>();

            // Initialize GUI stuff.
            listView1.LargeImageList = new ImageList();
            listView1.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;
            listView1.LargeImageList.ImageSize = new System.Drawing.Size(128, 128);

            // Prepare SQLite DB, create table.
            RunSqlStatement(@"CREATE TABLE IF NOT EXISTS Downloaded ( URL TEXT, PATH TEXT )");

            // Picture for not loaded pictures.
            PlaceHolder = Image.FromFile("PlaceHolder.jpg");

            // Create directory, where we place all our downloaded pictures.
            DirectoryInfo dir = new DirectoryInfo(@"SavedPictures");
            if (!dir.Exists)
                dir.Create();
            
            // Read text file with links.
            using (StreamReader inputReader = new StreamReader("urls.txt"))
            {
                while (!inputReader.EndOfStream)
                {
                    String url = inputReader.ReadLine();
                    if (String.IsNullOrEmpty(url))
                        continue;


                    Urls.Add(url);
                }
            }

            // Run downloads Async.
            const int LIMIT = 4;
            Task.Factory.StartNew(() => Parallel.ForEach(Urls, new ParallelOptions { MaxDegreeOfParallelism = LIMIT }, url => DownloadImageAndAddToListView(url)));
        }


        private void DownloadImageAndAddToListView(String url)
        {
            String filePath = Path.Combine(@"SavedPictures", Path.GetFileName(new Uri(url).LocalPath));

            var imagesCollection = listView1.LargeImageList.Images;
            ListViewItem nitem = new ListViewItem(Path.GetFileName(filePath));
            
            // Add record to GUI ListView.
            ControlHelper.ControlHelper.ControlInvoke(listView1, () => imagesCollection.Add(url, PlaceHolder));
            ControlHelper.ControlHelper.ControlInvoke(listView1, () => listView1.Items.Add(nitem));
            ControlHelper.ControlHelper.ControlInvoke(listView1, () => nitem.ImageKey = url);

            // Check if we already have this pic.
            DataTable dt = RunSqlStatement("SELECT path FROM Downloaded WHERE url=\'" + url + "\';");

            if (dt.Rows.Count > 0 && File.Exists(filePath))
            {
                ControlHelper.ControlHelper.ControlInvoke(listView1, () => imagesCollection.RemoveByKey(url));
                ControlHelper.ControlHelper.ControlInvoke(listView1, () => imagesCollection.Add(url, Image.FromFile(filePath)));
                return;
            }
            
            
            FileDownloader client = new FileDownloader();
            
            client.url = url;
            client.file = filePath;

            client.DownloadFileCompleted += DownloadCompleted;
            
            // Download Asynchronously.
            client.DownloadFileAsync(new Uri(url), filePath);
        }

        private void DownloadCompleted(Object sender, AsyncCompletedEventArgs args) {
            String filename = (sender as FileDownloader).file;
            String url = (sender as FileDownloader).url;

            var imagesCollection = listView1.LargeImageList.Images;
            
            try
            {
                ControlHelper.ControlHelper.ControlInvoke(listView1, () => imagesCollection.RemoveByKey(url));
                ControlHelper.ControlHelper.ControlInvoke(listView1, () => imagesCollection.Add(url, Image.FromFile(filename)));

                RunSqlStatement("INSERT into Downloaded(url, path) values(\'" + url + "\', \'" + filename + "\');");
            }
            catch (SQLiteException) {
                MessageBox.Show("Could not insert into SQLite");
                Application.Exit();
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not download file " + url + " reason is: " + e.Message);
            }
        }

        private DataTable RunSqlStatement(String SqlStatement)
        {
            SQLiteConnection SqliteCon;
            try
            {
                // Create or Open database.
                string dbConnectionString = @"Data Source=database.s3db";
                SqliteCon = new SQLiteConnection(dbConnectionString);
                SqliteCon.Open();
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect to DB database.s3db");
                return null;
            }

            DataTable respond = new DataTable();
            try
            {
                SQLiteCommand command = new SQLiteCommand(SqlStatement, SqliteCon);
                SQLiteDataAdapter da = new SQLiteDataAdapter(command);
                da.Fill(respond);
            }
            catch
            {
                throw new SQLiteException("Bad SQL Statement");
            }
            finally
            {
                SqliteCon.Close();
            }

            return respond;
        }

    }
}
