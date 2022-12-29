using H264ToH265BatchConverter.ViewModels;
using System.Windows.Controls;

namespace H264ToH265BatchConverter.Controls
{
    /// <summary>
    /// Logique d'interaction pour FileConversionComponent.xaml
    /// </summary>
    public partial class FileConversionComponent : UserControl
    {
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
        }


    }
}
