using H264ToH265BatchConverter.ViewModels;
using System.Windows.Controls;

namespace H264ToH265BatchConverter.Controls
{
    /// <summary>
    /// Logique d'interaction pour FileConversionComponent.xaml
    /// </summary>
    public partial class FileConversionComponent : UserControl
    {
        public string FileName { get; private set; }

        public string Duration { get; private set; }

        public FileConversionViewModel File { get; set; }

        public FileConversionComponent()
        {
            InitializeComponent();

            DataContext = this;
        }

        public FileConversionComponent(FileConversionViewModel file)
        {
            InitializeComponent();

            DataContext = this;

            File = file;
            FileName = File.Name;
            Duration = "0";
        }


    }
}
