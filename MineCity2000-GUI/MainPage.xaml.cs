namespace com.mc2k.gui;
using com.mc2k.MineCity2000;

public partial class MainPage : ContentPage
{
  public MainPage()
  {
    InitializeComponent();
  }

  private void OnSimCityFileClicked(object sender, EventArgs e)
  {
  }

  private void OnMinecraftDirectoryClicked(object sender, EventArgs e)
  {
  }

  private void OnFillUndergroundCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
  }

  private void OnGenerateTerrainCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
  }

  private void OnConvertClicked(object sender, EventArgs e)
  {
    Boolean fillUnderground = false;
    Boolean generateTerrain = false;
    string inputFile = "C:\\Program Files\\Maxis\\SimCity 2000\\CITIES\\zabnca.sc2";
    string buildingsDir = "C:\\Users\\jerne\\Documents\\Dev stuff\\GitHub\\mine-city-2000\\buildings";
    string outputDir = "C:\\Users\\jerne\\Documents\\Dev stuff\\GitHub\\mine-city-2000\\donotcommit";
    SCMapper mapper = new SCMapper(buildingsDir);
    //mapper.Worker = worker;
    MapperOptions options = new MapperOptions(fillUnderground, generateTerrain);
    mapper.makeMap(inputFile, outputDir, options);
  }
}

