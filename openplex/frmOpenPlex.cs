﻿using OpenPlex;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Web.Script.Serialization;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OpenPlex
{
    public partial class frmOpenPlex : Form
    {
        public frmOpenPlex()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e) { }

        public static string linkBackground = "https://raw.githubusercontent.com/invu/openplex-app/master/Assets/openplex-backgrounds-db.txt";
        public static string linkDatabase = "https://raw.githubusercontent.com/invu/openplex-app/master/Assets/openplex-db.txt";
        public static string linkLatestVersion = "https://raw.githubusercontent.com/invu/openplex-app/master/Assets/openplex-version.txt";
        public static string pathTempFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenPlex\";
        public static string pathDownloads = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OpenPlex\Downloads\";

        public static Bitmap LoadPicture(string url)
        {
            System.Net.HttpWebRequest wreq;
            System.Net.HttpWebResponse wresp;
            Stream mystream;
            Bitmap bmp;

            bmp = null;
            mystream = null;
            wresp = null;
            try
            {
                wreq = (HttpWebRequest)WebRequest.Create(url);
                wreq.AllowWriteStreamBuffering = true;

                wresp = (HttpWebResponse)wreq.GetResponse();

                if ((mystream = wresp.GetResponseStream()) != null)
                    bmp = new Bitmap(mystream);
            }
            catch
            {
                // Do nothing... 
            }
            finally
            {
                if (mystream != null)
                    mystream.Close();

                if (wresp != null)
                    wresp.Close();
            }

            return (bmp);
        }

        public string Random(ICollection<string> Items)
        {
            Random Rndm = new Random();
            List<string> StringList = new List<string>(Items);
            return StringList[Rndm.Next(0, Items.Count)];
        }

        public static Bitmap ChangeOpacity(Image img, float opacityvalue)
        {
            try
            {
                Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
                Graphics graphics = Graphics.FromImage(bmp);
                ColorMatrix colormatrix = new ColorMatrix();
                colormatrix.Matrix33 = opacityvalue;
                ImageAttributes imgAttribute = new ImageAttributes();
                imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                graphics.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribute);
                graphics.Dispose();   // Releasing all resource used by graphics 
                return bmp;
            }
            catch
            {
                return Properties.Resources.Dark_Sky_Night;
            }
        }

        WebClient client = new WebClient();

        public static List<string> listMovieBackgrounds = new List<string>();

        public static string pathVLC = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";

        private void frmOpenPlex_Load(object sender, EventArgs e)
        {
            checkForUpdate();

            frmClient = new frmDownloadClient();

            VLCToolStripMenuItem.Visible = File.Exists(pathVLC);

            string movieBackground = client.DownloadString(linkBackground);
            string[] movieBackgroundDb = movieBackground.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string background in movieBackgroundDb)
            {
                listMovieBackgrounds.Add(background);
            }

            string generateImage = Random(listMovieBackgrounds);

            try { tabHome.BackgroundImage = ChangeOpacity(LoadPicture(generateImage), 0.2F); }
            catch { tabHome.BackgroundImage = Properties.Resources.Dark_Sky_Night; }

            Directory.CreateDirectory(pathTempFolder);
            Directory.CreateDirectory(pathDownloads);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileAsync(new Uri(linkDatabase), pathTempFolder + @"\openplex-db.txt");
            }

            tabAbout.BackgroundImage = ChangeOpacity(Properties.Resources.Dark_Sky_Night, 0.2F);

            lblAboutVersion.Text = "v" + Application.ProductVersion;

        }

        Version newVersion;

        public void checkForUpdate()
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(linkLatestVersion);
            StreamReader reader = new StreamReader(stream);
            newVersion = new Version(reader.ReadLine());
            Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            if (curVersion.CompareTo(newVersion) < 0) //wtf
            {
                MessageBox.Show("There's a new version for OpenPlex ready to be installed  (" + newVersion + ")", "Update Available");
                try
                {
                    Process.Start(Application.StartupPath + @"\OpenPlex Updater.exe");
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        public void getMovieDetails(string webFile)
        {
            string url = "";

            string[] movieName = getMovieAndYear(Path.GetFileName(webFile));
            string[] tvshowName = getTVShowName(Path.GetFileName(webFile));

            if (!(movieName == null)) { url = "http://omdbapi.com/?apikey=c933e052&t=" + movieName[0] + "&y=" + movieName[1] + "&plot=full"; }
            else if (!(tvshowName == null)) { url = "http://omdbapi.com/?apikey=c933e052&t=" + tvshowName[0] + "&Season=" + tvshowName[1] + "&Episode=" + tvshowName[2]; }

            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString(url);
                JavaScriptSerializer oJS = new JavaScriptSerializer();
                ImdbEntity obj = new ImdbEntity();
                obj = oJS.Deserialize<ImdbEntity>(json);
                if (obj.Response == "True")
                {
                    movieInfoTitle.Text = obj.Title.Replace("&", "&&");
                    movieInfoRuntime.Text = obj.Runtime;
                    movieInfoGenre.Text = obj.Genre;
                    movieInfoDescription.Text = obj.Plot;
                    //movieInfoDirector.Text = obj.Director;
                    //movieInfoActors.Text = obj.Actors;
                    imgMovieInfoPoster.Image = LoadPicture(obj.Poster);
                    movieInfoReleaseDate.Text = obj.Released;
                    //movieInfoCountry.Text = obj.Country;
                    //movieInfoLanguage.Text = obj.Language;
                    //movieInfoProduction.Text = obj.Production;
                    //movieInfoBoxOffice.Text = obj.BoxOffice;
                    //movieInfoAwards.Text = obj.Awards;

                    movieInfoIMDbRating.Text = obj.imdbRating + "/10";
                    movieInfoIMDbId.Text = obj.imdbID;

                    //movieInfoMetascore.Text = obj.Metascore;

                    movieInfoFileURL.Text = webFile;

                    panelMovieSubItems.Visible = true;
                    movieInfoDescription.Visible = true;
                }
                else
                {
                    movieInfoTitle.Text = Path.GetFileName(webFile);
                    movieInfoRuntime.Text = "";
                    movieInfoGenre.Text = "";
                    movieInfoDescription.Text = "";
                    //movieInfoDirector.Text = obj.Director;
                    //movieInfoActors.Text = obj.Actors;
                    imgMovieInfoPoster.Image = ChangeOpacity(Properties.Resources.PosterDefault, 0.4F);
                    movieInfoReleaseDate.Text = "";
                    //movieInfoCountry.Text = obj.Country;
                    //movieInfoLanguage.Text = obj.Language;
                    //movieInfoProduction.Text = obj.Production;
                    //movieInfoBoxOffice.Text = obj.BoxOffice;
                    //movieInfoAwards.Text = obj.Awards;

                    movieInfoIMDbRating.Text = "";
                    movieInfoIMDbId.Text = "";

                    //movieInfoMetascore.Text = obj.Metascore;

                    movieInfoFileURL.Text = webFile;

                    panelMovieSubItems.Visible = false;
                    movieInfoDescription.Visible = false;
                }
            }

            try
            {
                //Details from Popcorn Time API for Background (fanart/trailer)
                var jsonPopcornTime = client.DownloadString("https://tv-v2.api-fetch.website/movie/" + movieInfoIMDbId.Text);
                JavaScriptSerializer oJSPopcornTime = new JavaScriptSerializer();
                PopcornTimeEntity objPopcornTime = new PopcornTimeEntity();
                objPopcornTime = oJSPopcornTime.Deserialize<PopcornTimeEntity>(jsonPopcornTime);

                tabMovieDetails.BackgroundImage = ChangeOpacity(LoadPicture(objPopcornTime.images.fanart), 0.2F);
                movieInfoPopcornFanartURL.Text = objPopcornTime.images.fanart;
                movieInfoPopcornTrailerURL.Text = objPopcornTime.trailer;
                btnMovieTrailer.Visible = true;
            }
            catch
            {
                tabMovieDetails.BackgroundImage = ChangeOpacity(Properties.Resources.Dark_Sky_Night, 0.4F);
                movieInfoPopcornFanartURL.Text = "";
                movieInfoPopcornTrailerURL.Text = "";
                btnMovieTrailer.Visible = false;
            }
        }

        public void getTVShowDetails()
        {

        }

        public string[] getTVShowName(string input)
        {
            string Standard = @"^((?<series_name>.+?)[. _-]+)?s(?<season_num>\d+)[. _-]*e(?<ep_num>\d+)(([. _-]*e|-)(?<extra_ep_num>(?!(1080|720)[pi])\d+))*[. _-]*((?<extra_info>.+?)((?<![. _-])-(?<release_group>[^-]+))?)?$";
            var regexStandard = new Regex(Standard, RegexOptions.IgnoreCase);
            Match episode = regexStandard.Match(input);
            var Showname = episode.Groups["series_name"].Value;
            var Season = episode.Groups["season_num"].Value;
            var Episode = episode.Groups["ep_num"].Value;

            string[] tvshow = { Showname, Season, Episode };
            return tvshow;
        }

        private void btnSearch_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            if (!(txtSearchBox.Text == "")) { searchDatabase(txtSearchBox.Text); }
        }

        public void searchDatabase(string text)
        {
            try
            {
                dataGrid.Rows.Clear();

                if (File.Exists(pathTempFolder + @"\openplex-db.txt"))
                {
                    string[] readDb = File.ReadAllLines(pathTempFolder + @"\openplex-db.txt");
                    string[] keyWords = Regex.Split(txtSearchBox.Text, @"\s+");

                    foreach (string file in readDb) { if (GetWords(txtSearchBox.Text.ToLower()).Any(x => Path.GetFileName(file.ToLower()).Contains(x))) { dataGrid.Rows.Add(file.Replace("%20", " ")); } }

                    lblHeaderResultsText.Text = string.Join(" ", keyWords);;
                    tab.SelectedTab = tabSearchResults;
                }
                else MessageBox.Show("Unable to locate database file. Please restart the app and try again.");
            }
            catch { MessageBox.Show("Unable to search database. Please try again in a moment."); }
        }

        static string[] GetWords(string input)
        {
            MatchCollection matches = Regex.Matches(input, @"\b[\w']*\b");

            var words = from m in matches.Cast<Match>()
                        where !string.IsNullOrEmpty(m.Value)
                        select TrimSuffix(m.Value);

            return words.ToArray();
        }

        static string TrimSuffix(string word)
        {
            int apostropheLocation = word.IndexOf('\'');
            if (apostropheLocation != -1)
            {
                word = word.Substring(0, apostropheLocation);
            }

            return word;
        }

        public string[] getMovieAndYear(string input)
        {
            string pattern = @"(?'title'.*)\.(?'year'[^\.]+)\.(?'pixelsize'[^\.]+)\.(?'format'[^\.]+)\.(?'formatsize'[^\.]+)\.(?'filename'[^\.]+)\.(?'extension'[^\.]+)";
            Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            string extension = match.Groups["extension"].Value;
            string fileName = match.Groups["filename"].Value;
            string formatSize = match.Groups["formatsize"].Value;
            string format = match.Groups["format"].Value;
            string pixelSize = match.Groups["pixelsize"].Value;
            string year = match.Groups["year"].Value;
            string title = match.Groups["title"].Value;
            title = title.Replace(".", " ");

            string[] movie = { title, year };
            return movie;
        }

        public static bool ContainsAll(string source, params string[] values)
        {
            return values.All(x => source.ToLower().Contains(x));
        }

        private void lblBackHome_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tabHome;
        }

        private void btnBack_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabHome;
        }


        private void imgAbout_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tabAbout;
        }

        private void btnTag4_Click(object sender, EventArgs e)
        {
            CButtonLib.CButton ctrlTag = sender as CButtonLib.CButton;
            txtSearchBox.Text += ctrlTag.Text;
        }

        private void wMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string value = dataGrid.CurrentRow.Cells[0].Value.ToString();

                Process.Start("wmplayer.exe", value);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error"); }
        }

        private void VLCToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                string value = dataGrid.CurrentRow.Cells[0].Value.ToString();

                Process VLC = new Process();
                VLC.StartInfo.FileName = pathVLC;
                VLC.StartInfo.Arguments = ("-vvv " + value);
                VLC.Start();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error"); }
        }

        private void imgCloseAbout_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tabHome;
        }

        private void lblAboutReportIssue_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/invu/openplex-app/issues/new");
        }

        private void dataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            getMovieDetails(dataGrid.CurrentRow.Cells[0].Value.ToString());
            tab.SelectedTab = tabMovieDetails;
        }

        private void imgCloseMovieDetails_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tabSearchResults;
        }

        private frmDownloadClient frmClient;

        private void btnMovieDownload_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            if (!frmClient.Visible)
            {
                frmClient.Show();
                frmClient.doDownloadFile(movieInfoFileURL.Text);
            }
            else
            {
                frmClient.doDownloadFile(movieInfoFileURL.Text);
                frmClient.Focus();
            }
        }

        private void btnMoviePlay_Click(object sender, EventArgs e)
        {
            contextFileName.Show(btnMoviePlay, btnMoviePlay.PointToClient(Cursor.Position));
        }

        private void btnMovieTrailer_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            Process.Start(movieInfoPopcornTrailerURL.Text);
        }

        private void frmOpenPlex_SizeChanged(object sender, EventArgs e)
        {
            panelMovieSubItems.Size = new Size(panelMovieItems.Width, panelMovieSubItems.Height);
        }

        private void tabHome_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string filePath in files)
                {
                    try
                    {
                        Process.Start("wmplayer.exe", filePath);
                    }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error"); }
                }
            }
        }

        private void imgCloseSearchResults_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tabHome;
        }

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string value = dataGrid.CurrentRow.Cells[0].Value.ToString();
                Clipboard.SetText(value);
                MessageBox.Show(this, "URL Copied!", "Success");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error"); }
        }

        private void frmOpenPlex_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { if (Properties.Settings.Default.settingsClearDataOnClose == true) { Directory.Delete(pathTempFolder, true); } }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error"); }
        }
    }
}
