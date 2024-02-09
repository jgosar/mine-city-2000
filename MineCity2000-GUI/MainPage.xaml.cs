namespace com.mc2k.gui;

using System.ComponentModel;
using com.mc2k.MineCity2000;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;

public partial class MainPage : ContentPage
{
  String inputFile = null;
  String outputDir = null;
  Boolean fillUnderground = false;
  Boolean generateTerrain = false;
  String buildingsDir = null;
  SCMapper mapper = null;
  private BackgroundWorker bw = new BackgroundWorker();

  public MainPage()
  {
    InitializeComponent();
    //openFileDialog1.InitialDirectory = @"C:\Program Files\Maxis\SimCity 2000\Cities";
    //folderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft";

    bw = new BackgroundWorker();
    bw.WorkerReportsProgress = true;
    bw.WorkerSupportsCancellation = true;
    bw.DoWork += new DoWorkEventHandler(bw_DoWork);
    bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

    String workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    String[] debugPaths = new string[2] { "\\MineCity2000-GUI\\bin\\Debug", "\\MineCity2000-GUI\\bin\\x64\\Debug" };

    foreach (string debugPath in debugPaths)
    {
      if (workingDir.Contains(debugPath))
      {
        workingDir = workingDir.Replace(debugPath, "");
      }
    }

    buildingsDir = workingDir + "\\buildings";
    mapper = new SCMapper(buildingsDir);
  }

  private async void OnSimCityFileClicked(object sender, EventArgs e)
  {
    var customFileType = new FilePickerFileType(
               new Dictionary<DevicePlatform, IEnumerable<string>>
               {
                   { DevicePlatform.WinUI, new[] { ".sc2" } },
               });

    PickOptions options = new()
    {
      FileTypes = customFileType,
    };
    var result = await FilePicker.Default.PickAsync(options);
    if (result != null)
    {
      inputFile = result.FullPath;
      StatusLabel.Text = "";
    }
  }

  private async void OnMinecraftDirectoryClicked(object sender, EventArgs e)
  {
    var result = await FolderPicker.Default.PickAsync();
    var selectedPath = result.Folder.Path;

    String[] tmp = selectedPath.Split(new char[] { '\\' });
    if (tmp[tmp.Length - 1].Equals(".minecraft"))
    {
      if (!File.Exists(selectedPath + "\\saves"))
      {
        Directory.CreateDirectory(selectedPath + "\\saves");
      }

      outputDir = selectedPath + "\\saves\\MineCity2000";
    }
    else if (tmp[tmp.Length - 1].Equals("saves"))
    {
      outputDir = selectedPath + "\\MineCity2000";
    }
    else
    {
      outputDir = selectedPath + "\\MineCity2000";
    }

    StatusLabel.Text = "";
  }

  private void OnFillUndergroundCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
    fillUnderground = e.Value;
  }

  private void OnGenerateTerrainCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
    generateTerrain = e.Value;
  }

  private void OnConvertClicked(object sender, EventArgs e)
  {
    /*Boolean fillUnderground = false;
    Boolean generateTerrain = false;
    string inputFile = "C:\\Program Files\\Maxis\\SimCity 2000\\CITIES\\zabnca.sc2";
    string buildingsDir = "C:\\Users\\jerne\\Documents\\Dev stuff\\GitHub\\mine-city-2000\\buildings";
    string outputDir = "C:\\Users\\jerne\\Documents\\Dev stuff\\GitHub\\mine-city-2000\\donotcommit";
    SCMapper mapper = new SCMapper(buildingsDir);
    //mapper.Worker = worker;
    MapperOptions options = new MapperOptions(fillUnderground, generateTerrain);
    mapper.makeMap(inputFile, outputDir, options);*/

    if (inputFile == null)
    {
      StatusLabel.Text = "Please select a SimCity 2000 file";
      return;
    }
    else if (outputDir == null)
    {
      StatusLabel.Text = "Please select the output directory";
      return;
    }

    if (bw.IsBusy != true)
    {
      bw.RunWorkerAsync();
    }
    ConvertButton.IsEnabled = false;
  }

  private void bw_DoWork(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = sender as BackgroundWorker;

    mapper = new SCMapper(buildingsDir);
    mapper.Worker = worker;
    MapperOptions options = new MapperOptions(fillUnderground, generateTerrain);
    mapper.makeMap(inputFile, outputDir, options);
  }
  private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
  {
    ProgressBarContainer.IsVisible = false;
    ConvertButton.IsEnabled = true;

    if ((e.Cancelled == true))
    {
      StatusLabel.Text = "Canceled!";
    }

    else if (!(e.Error == null))
    {
      StatusLabel.Text = ("Error: " + e.Error.Message);
    }

    else
    {
      StatusLabel.Text = "Done!";
    }
  }
  private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
  {
    ProgressBarContainer.IsVisible = true;
    ConversionProgressBar.Progress = e.ProgressPercentage / 100.0;
  }
}

