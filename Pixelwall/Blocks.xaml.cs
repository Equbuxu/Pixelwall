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
using System.Windows.Shapes;

namespace Pixelwall
{
    /// <summary>
    /// Interaction logic for Blocks.xaml
    /// </summary>
    public partial class Blocks : Window
    {
        Data data;

        public Blocks(Data data)
        {
            InitializeComponent();
            this.data = data;
            ConstructEverything();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, Texture> pair in data.textures)
            {
                pair.Value.used = checkBoxes[pair.Key].IsChecked.Value;
            }
        }

        private Dictionary<string, StackPanel> blocks = new Dictionary<string, StackPanel>();
        private Dictionary<string, WrapPanel> categories = new Dictionary<string, WrapPanel>();
        private Dictionary<string, CheckBox> checkBoxes = new Dictionary<string, CheckBox>();

        private void ConstructEverything()
        {
            foreach (KeyValuePair<string, Texture> element in data.textures)
            {
                AddTexture(element.Value);
            }
        }

        private void AddTexture(Texture texture)
        {
            if (!categories.ContainsKey(texture.category))
            {
                CreateCategoryInDictionary(texture.category);
            }

            if (blocks.ContainsKey(texture.displayName))
            {
                Image image = new Image
                {
                    Source = Util.BitmapToBitmapImage(texture.texture),
                    Width = data.TextureResolution,
                    Height = data.TextureResolution
                };
                blocks[texture.displayName].Children.Insert(1, image);
                checkBoxes.Add(texture.id, blocks[texture.displayName].Children[0] as CheckBox);
            }
            else
            {
                StackPanel panel = ConstructTexture(texture);
                checkBoxes.Add(texture.id, panel.Children[0] as CheckBox);
                blocks.Add(texture.displayName, panel);
                categories[texture.category].Children.Add(panel);
            }
        }


        private StackPanel ConstructTexture(Texture texture)
        {
            CheckBox checkBox = new CheckBox
            {
                IsChecked = texture.used,
                Margin = new Thickness(2.0)
            };

            Image image = new Image
            {
                Source = Util.BitmapToBitmapImage(texture.texture),
                Width = data.TextureResolution,
                Height = data.TextureResolution
            };

            TextBlock text = new TextBlock
            {
                Text = texture.displayName,
                Margin = new Thickness(2.0)
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            panel.Children.Add(checkBox);
            panel.Children.Add(image);
            panel.Children.Add(text);

            return panel;
        }

        private void CreateCategoryInDictionary(string name)
        {
            Border border = new Border
            {
                BorderThickness = new Thickness(1.0),
                BorderBrush = Brushes.Gray,
                Margin = new Thickness(5.0)
            };

            StackPanel textAndCheckBox = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            WrapPanel panel = new WrapPanel
            {
                Orientation = Orientation.Vertical
            };

            CheckBox checkBox = new CheckBox
            {
                IsChecked = true,
                Margin = new Thickness(2.0),
                VerticalAlignment = VerticalAlignment.Center
            };

            checkBox.Click += (object sender, RoutedEventArgs e) =>
            {
                foreach (Object control in panel.Children)
                {
                    if (control.GetType() == typeof(StackPanel))
                    {
                        ((control as StackPanel).Children[0] as CheckBox).IsChecked = (sender as CheckBox).IsChecked;
                    }
                }

            };

            TextBlock text = new TextBlock
            {
                Text = name,
                Margin = new Thickness(2.0),
                FontSize = 15.0
            };

            border.Child = panel;
            textAndCheckBox.Children.Add(checkBox);
            textAndCheckBox.Children.Add(text);
            panel.Children.Add(textAndCheckBox);
            BlockList.Children.Add(border);
            categories.Add(name, panel);

        }
    }
}