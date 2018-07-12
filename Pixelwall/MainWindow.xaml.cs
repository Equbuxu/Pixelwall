using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pixelwall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        public Data data;
        public MainWindow()
        {
            InitializeComponent();
            data = new Data(this);
            data.LoadChosenTexturesList("chosentextures.txt");
        }

        public void ConsoleLog(string msg)
        {
            if (String.IsNullOrEmpty(ConsoleBlock.Text))
                ConsoleBlock.Text = msg;
            else
                ConsoleBlock.Text = ConsoleBlock.Text + '\n' + msg;
        }

        public void ConsoleLogError(string msg)
        {
            ConsoleLog("[ERROR] " + msg);
        }

        public void ConsoleLogWarning(string msg)
        {
            ConsoleLog("[WARNING] " + msg);
        }

        private Uri image;

        private void OnBlocksClick(object sender, RoutedEventArgs e)
        {
            Blocks blocks = new Blocks(data);
            blocks.Show();
        }

        private void OnChooseImageClick(object sender, RoutedEventArgs e)
        {
            
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".png";
            fileDialog.Filter = "Images (*.png,*.jpg, *.jpeg)|*.png;*.jpg;*.jpeg";

            bool? success = fileDialog.ShowDialog();
            if (success.Value)
            {
                image = new Uri(fileDialog.FileName);

                var loadedPreview = new BitmapImage();
                loadedPreview.BeginInit();
                loadedPreview.UriSource = image;
                loadedPreview.CacheOption = BitmapCacheOption.OnLoad;
                loadedPreview.EndInit();

                WidthTextBox.Text = loadedPreview.PixelWidth.ToString();
                HeightTextBox.Text = loadedPreview.PixelHeight.ToString();
                PreviewImage.Source = loadedPreview;
                
            }
        }

        private void OnGenerateClick(object sender, RoutedEventArgs e)
        {
            
            Pixelart pixelart;

            BlockOrientation orientation = BlockOrientation.TOP;
            if (topRadio.IsChecked.Value)
                orientation = BlockOrientation.TOP;
            if (bottomRadio.IsChecked.Value)
                orientation = BlockOrientation.BOTTOM;
            if (vertRadio.IsChecked.Value)
                orientation = BlockOrientation.VERTICAL;


            int width = 0, height = 0;
            if (int.TryParse(WidthTextBox.Text, out width) && int.TryParse(HeightTextBox.Text, out height))
            {
                pixelart = new Pixelart(data, width, height, image, DitherCheckBox.IsChecked.Value, orientation);
            }
            else
            {
                pixelart = new Pixelart(data, image, DitherCheckBox.IsChecked.Value, orientation);
            }

            Result resultWindow = new Result(pixelart, data, this);
            resultWindow.Show();
            
        }
    }
}
