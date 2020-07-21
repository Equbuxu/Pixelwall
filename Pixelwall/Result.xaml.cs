using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using System.Windows.Shapes;

namespace Pixelwall
{
    /// <summary>
    /// Interaction logic for Result.xaml
    /// </summary>
    public partial class Result : Window
    {
        Pixelart art;
        Bitmap image;
        Data data;
        MainWindow window;

        public Result(Pixelart result, Data data, MainWindow window)
        {
            InitializeComponent();

            art = result;
            image = art.GetImage();
            this.data = data;
            this.window = window;
            GenerationResult.Source = Util.BitmapToBitmapImage(image);
            AddMaterials();
        }

        private void AddMaterials()
        {
            foreach (KeyValuePair<string, int> pair in art.blockUses.OrderByDescending(pair => pair.Value))
            {
                Bitmap texture;
                string displayName;

                //Get texture and name
                if (data.textures.ContainsKey(pair.Key))
                {
                    texture = data.textures[pair.Key].texture;
                    displayName = data.textures[pair.Key].displayName;
                }
                else
                {
                    int i;
                    if (!Int32.TryParse(pair.Key, out i))
                    {
                        window.ConsoleLogError("Could not find the texture and display name for \"" + pair.Key + "\"");
                        continue;
                    }

                    if (data.blocks[i].textureIDs.Length < 1)
                    {
                        window.ConsoleLogError("No textures are present for block \"" + data.blocks[i].displayName + "\"");
                        continue;
                    }

                    texture = data.textures[data.blocks[i].textureIDs[0]].texture;
                    displayName = data.blocks[i].displayName;
                }

                //make a gui element
                System.Windows.Controls.Image image = new System.Windows.Controls.Image
                {
                    Source = Util.BitmapToBitmapImage(texture),
                    Margin = new Thickness(2.0)
                };

                TextBlock number = new TextBlock
                {
                    Text = displayName + " - " + pair.Value.ToString(),
                    Margin = new Thickness(2.0)
                };

                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5.0)
                };

                Border border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1.0),
                    Margin = new Thickness(5.0)
                };

                panel.Children.Add(image);
                panel.Children.Add(number);
                border.Child = panel;
                MaterialList.Children.Add(border);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG image|*.png",
                Title = "Save an Image"
            };
            fileDialog.ShowDialog();

            if (!String.IsNullOrEmpty(fileDialog.FileName))
            {
                if (ShowChunkGrid.IsChecked.Value)
                {
                    DrawChunkGrid(image).Save(fileDialog.FileName);
                }
                else
                {
                    image.Save(fileDialog.FileName);
                }
            }
        }

        private Bitmap DrawChunkGrid(Bitmap image)
        {
            Bitmap newImage = new Bitmap(image);
            Graphics gr = Graphics.FromImage(newImage);

            System.Drawing.Pen redPen = new System.Drawing.Pen(System.Drawing.Color.Red, 3);
            System.Drawing.Pen orangePen = new System.Drawing.Pen(System.Drawing.Color.Orange, 1);

            for (int i = 0; i < image.Size.Width; i += data.TextureResolution * 8)
            {
                if (i % (data.TextureResolution * 16) == 0)
                    gr.DrawLine(redPen, new PointF { X = i, Y = 0 }, new PointF { X = i, Y = image.Size.Height });
                else
                    gr.DrawLine(orangePen, new PointF { X = i, Y = 0 }, new PointF { X = i, Y = image.Size.Height });
            }

            for (int i = 0; i < image.Size.Height; i += data.TextureResolution * 8)
            {
                if (i % (data.TextureResolution * 16) == 0)
                    gr.DrawLine(redPen, new PointF { X = 0, Y = i }, new PointF { X = image.Size.Width, Y = i });
                else
                    gr.DrawLine(orangePen, new PointF { X = 0, Y = i }, new PointF { X = image.Size.Width, Y = i });
            }

            redPen.Dispose();
            orangePen.Dispose();

            return newImage;
        }

        private void OnSaveTextClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Txt file|*.txt",
                Title = "Save resources list"
            };
            fileDialog.ShowDialog();

            if (!String.IsNullOrEmpty(fileDialog.FileName))
            {
                StreamWriter file = new StreamWriter(fileDialog.FileName);
                foreach (KeyValuePair<string, int> pair in art.blockUses.OrderByDescending(pair => pair.Value))
                {
                    string displayName;
                    if (data.textures.ContainsKey(pair.Key))
                    {
                        displayName = data.textures[pair.Key].displayName;
                    }
                    else
                    {
                        int i;
                        Int32.TryParse(pair.Key, out i);
                        displayName = data.blocks[i].displayName;
                    }

                    if (pair.Value <= 64)
                        file.WriteLine("{0}: {1}", displayName, pair.Value);
                    else if (pair.Value % 64 == 0)
                        file.WriteLine("{0}: {1} ({2}x64)", displayName, pair.Value, pair.Value / 64);
                    else
                        file.WriteLine("{0}: {1} ({2}x64 + {3})", displayName, pair.Value, pair.Value / 64, pair.Value % 64);
                }
                file.Close();
                file.Dispose();
            }
        }

        private void OnLitematicSave(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "litematic|*.litematic",
                Title = "Save a litematic"
            };
            fileDialog.ShowDialog();

            if (!String.IsNullOrEmpty(fileDialog.FileName))
            {
                art.CreateLitematic().Save(fileDialog.FileName);
            }
        }

        private void OnMultLitematicSave(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "litematic|*.litematic",
                Title = "Save a litematic"
            };
            fileDialog.ShowDialog();

            if (!String.IsNullOrEmpty(fileDialog.FileName))
            {
                foreach (KeyValuePair<string, Texture> texture in data.textures)
                {
                    Litematic lm = art.CreateSpecificLitematic(texture.Value);
                    if (lm == null)
                        continue;
                    string name = System.IO.Path.GetDirectoryName(fileDialog.FileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileDialog.FileName) + "-" + texture.Value.displayName + ".litematic";
                    lm.Save(name);
                }
            }
        }

        private void OnMultipleSave(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG image|*.png",
                Title = "Save an Image"
            };
            fileDialog.ShowDialog();

            if (String.IsNullOrEmpty(fileDialog.FileName))
            {
                return;
            }

            Bitmap image = new Bitmap(art.width * 16, art.height * 16);
            foreach (KeyValuePair<string, Texture> texture in data.textures)
            {
                if (!art.PaintSpecificImage(texture.Value, image))
                    continue;
                string name = System.IO.Path.GetDirectoryName(fileDialog.FileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileDialog.FileName) + "-" + texture.Value.displayName + ".png";
                if (ShowChunkGrid.IsChecked.Value)
                {
                    var grid = DrawChunkGrid(image);
                    grid.Save(name);
                }
                else
                {
                    image.Save(name);
                }
            }
            image.Dispose();
        }
    }
}
