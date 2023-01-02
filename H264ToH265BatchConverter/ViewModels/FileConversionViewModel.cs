using H264ToH265BatchConverter.Controls;
using H264ToH265BatchConverter.Model;
using Lakio.Framework.Core.IO;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace H264ToH265BatchConverter.ViewModels
{
    public class FileConversionViewModel : DependencyObject
    {
        private const string CONST_PathImageDefault = @"Resources\default.png";

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(FileConversionViewModel));

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register("Duration", typeof(string), typeof(FileConversionViewModel));

        public string Duration
        {
            get => (string)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapImage), typeof(FileConversionViewModel));

        public BitmapImage ImageSource
        {
            get => (BitmapImage)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(double), typeof(FileConversionViewModel));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty FileVisibilityProperty = DependencyProperty.Register("FileVisibility", typeof(Visibility), typeof(FileConversionViewModel));

        public Visibility FileVisibility
        {
            get => (Visibility)GetValue(FileVisibilityProperty);
            set => SetValue(FileVisibilityProperty, value);
        }

        public FileObject File { get; set; }

        public FileConversion FileConversion { get; set; }

        public FileConversionComponent FileComponent { get; set; }

        public FileConversionViewModel(FileObject file)
        {
            File = file;

            Name = File.FullName;
            Progress = 0;
            FileVisibility = Visibility.Visible;

            SetImageSource(CONST_PathImageDefault);
        }

        public void SetImageSource(string imagePath)
        {
            var uri = new Uri(Path.GetFullPath(imagePath));

            ImageSource = new BitmapImage(uri);
        }
    }
}
