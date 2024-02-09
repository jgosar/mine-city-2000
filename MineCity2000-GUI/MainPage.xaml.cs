﻿namespace com.mc2k.gui;

using System.ComponentModel;
using com.mc2k.MineCity2000;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;

public partial class MainPage : ContentPage
{
  String? inputFile = null;
  String? outputDir = null;
  Boolean fillUnderground = false;
  Boolean generateTerrain = false;
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
    SCMapper mapper = new SCMapper(workingDir + "\\buildings");
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

