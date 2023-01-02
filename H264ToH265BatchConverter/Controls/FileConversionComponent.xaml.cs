using H264ToH265BatchConverter.ViewModels;
using System;
using System.Windows.Controls;

namespace H264ToH265BatchConverter.Controls
{
    /// <summary>
    /// Logique d'interaction pour FileConversionComponent.xaml
    /// </summary>
    public partial class FileConversionComponent : UserControl
    {
        public FileConversionViewModel File { get; set; }

        public Action<FileConversionComponent> OnClose { get; set; }

        public FileConversionComponent()
        {
            InitializeComponent();

            DataContext = this;
        }

        public FileConversionComponent(FileConversionViewModel file, Action<FileConversionComponent> onClose = null)
        {
            InitializeComponent();

            DataContext = this;

            File = file;

            file.FileComponent = this;

            OnClose = onClose;
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OnClose?.Invoke(this);
        }
    }
}
