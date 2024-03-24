namespace com.mc2k.gui;

using System.ComponentModel;
using com.mc2k.MineCity2000;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;

public partial class MainPage : ContentPage
{
  String? inputFile = null;
  String? outputDir = null;
  private BackgroundWorker bw = new BackgroundWorker();

  public MainPage()
  {
    InitializeComponent();
    InitializeBackgroundWorker();
  }

  private void InitializeBackgroundWorker()
  {
    bw = new BackgroundWorker();
    bw.WorkerReportsProgress = true;
    bw.WorkerSupportsCancellation = true;
    bw.DoWork += new DoWorkEventHandler(bw_DoWork);
    bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
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

    String selectedPath = result.Folder.Path;
    String selectedDirName = result.Folder.Name;

    outputDir = selectedPath;

    if (selectedDirName.Equals(".minecraft"))
    {
      if (!File.Exists(selectedPath + "\\saves"))
      {
        Directory.CreateDirectory(selectedPath + "\\saves");
      }

      outputDir = outputDir + "\\saves";
    }

    StatusLabel.Text = "";
  }

  private void OnConvertClicked(object sender, EventArgs e)
  {
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

    String workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    SCMapper mapper = new SCMapper(workingDir + "\\buildings", progressCallback);
    MapperOptions options = new MapperOptions(FillUndergroundCheckBox.IsChecked, GenerateTerrainCheckBox.IsChecked);

    String cityName = inputFile.ToLower().Split('\\').Last().Replace(".sc2", "");
    mapper.makeMap(inputFile, @$"{outputDir}\{cityName}", options);
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

  private void progressCallback(int progressPercentage)
  {
    bw.ReportProgress(progressPercentage);
  }
}

