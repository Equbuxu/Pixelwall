using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Pixelwall
{
    public class Texture
    {
        public string id = null;
        public string displayName = null;
        public string category = null;
        public Bitmap texture = null;
        public bool top = false, bottom = false, south = false, east = false, north = false, west = false;
        public Color avgColor;
        public bool used = true;
    }

    public class Block
    {
        public string displayName;
        public string[] textureIDs;
    }

    public class Data
    {
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        public List<Block> blocks = new List<Block>();
        private int textureResolution;
        public int TextureResolution { get { return textureResolution; } }

        private MainWindow window;

        public Data(MainWindow window)
        {
            this.window = window;

            window.ConsoleLog("Loading config.txt");
            ParseConfig();
            window.ConsoleLog("Loading blockdata.txt");
            ParseBlockData();
            ParseBlocks();
        }

        public void SaveChosenTexturesList()
        {
            window.ConsoleLog("Saving chosen textures to choosentextures.txt");
            StreamWriter file = new StreamWriter("chosentextures.txt");
            foreach (KeyValuePair<string, Texture> pair in textures)
            {
                if (pair.Value.used)
                    file.WriteLine(pair.Key);
            }
            file.Close();
            file.Dispose();
        }

        public void LoadChosenTexturesList(string filename)
        {
            window.ConsoleLog("Loading chosen textures from " + filename);
            try
            {
                using (StreamReader file = new StreamReader(filename))
                {

                    foreach (KeyValuePair<string, Texture> pair in textures)
                    {
                        pair.Value.used = false;
                    }

                    while (!file.EndOfStream)
                    {
                        textures[file.ReadLine()].used = true;
                    }
                }
            }
            catch (IOException)
            {
                window.ConsoleLogWarning("No file found.");
            }
        }

        private void ParseBlockData()
        {
            StreamReader file = null;
            try
            {
                file = new StreamReader("blockdata.txt");
            }
            catch (Exception)
            {
                window.ConsoleLogError("Could not open blockdata.txt file; no textures are loaded");
                file.Dispose();
                file = null;
            }

            if (file != null)
            {
                Texture curTexture;

                while (true)
                {
                    curTexture = ParseNextTexture(file);
                    if (curTexture == null)
                        break;
                    textures.Add(curTexture.id, curTexture);
                }
            }

            file.Close();
            file.Dispose();
        }

        private void ParseConfig()
        {
            try
            {
                using (StreamReader config = new StreamReader("config.txt"))
                {
                    string line;
                    while (true)
                    {
                        line = ReadLineFromFile(config);
                        if (line == null)
                            break;
                        string[] parameters = line.Split(new string[] { ": " }, StringSplitOptions.None);
                        if (parameters[0] == "textureResolution")
                        {
                            if (!int.TryParse(parameters[1], out textureResolution))
                            {
                                throw new Exception("could not parse config");
                            }
                        }
                        else
                        {
                            throw new Exception("could not parse config");
                        }
                    }
                }
            }
            catch (Exception)
            {
                window.ConsoleLogError("Could not read config file. Default values will be used instead.");
                textureResolution = 16;
            }
        }

        private void ParseBlocks()
        {
            try
            {
                using (StreamReader config = new StreamReader("textureblockmerge.txt"))
                {
                    string line;
                    while (true)
                    {
                        line = ReadLineFromFile(config);
                        if (line == null)
                            break;

                        string[] parameters = line.Split(new string[] { ", " }, StringSplitOptions.None);

                        Block block = new Block
                        {
                            textureIDs = new string[parameters.Length - 1],
                            displayName = parameters[0],
                        };
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            block.textureIDs[i - 1] = parameters[i];
                        }
                        blocks.Add(block);
                    }
                }
            }
            catch (Exception)
            {
                window.ConsoleLogError("Could not read textureblockmerge file. No textures will be merged into blocks.");
            }

            blocks.Sort((x, y) => (x.displayName.CompareTo(y.displayName)));
        }

        private string ReadLineFromFile(StreamReader file)
        {
            string line;
            do
            {
                line = file.ReadLine();
                if (line == null)
                    return null;
            }
            while (line.StartsWith("#") || String.IsNullOrWhiteSpace(line));
            return line;
        }

        private Texture ParseNextTexture(StreamReader file)
        {
            Texture texture;

            skipTexture:
            texture = new Texture();

            string line = ReadLineFromFile(file);
            if (line == null)
                return null;

            string[] parameters = line.Split(new string[] { ", " }, StringSplitOptions.None);

            foreach (string param in parameters)
            {
                string[] pair = param.Split(':');
                if (pair.Length != 2)
                {
                    window.ConsoleLogError("Parameter \"" + param + "\" could not be parsed. The texture will be ignored.");
                    goto skipTexture;
                }

                switch (pair[0])
                {
                    case "name":
                        texture.id = pair[1];
                        break;
                    case "rotation":
                        switch (pair[1])
                        {
                            case "any":
                                texture.top = true;
                                texture.bottom = true;
                                texture.west = true;
                                texture.south = true;
                                texture.north = true;
                                texture.east = true;
                                break;
                            case "side":
                                texture.west = true;
                                texture.south = true;
                                texture.north = true;
                                texture.east = true;
                                break;
                            case "top":
                                texture.top = true;
                                break;
                            case "bottom":
                                texture.bottom = true;
                                break;
                            case "topbottom":
                                texture.top = true;
                                texture.bottom = true;
                                break;
                            default:
                                window.ConsoleLogError("Unknown rotation \"" + pair[1] + "\"! Texture will be ignored.");
                                goto skipTexture;
                        }
                        break;
                    case "category":
                        texture.category = pair[1];
                        break;
                    case "display-name":
                        texture.displayName = pair[1];
                        break;
                    default:
                        window.ConsoleLogError("Unknown parameter \"" + pair[0] + "\"! Texture will be ignored.");
                        goto skipTexture;
                }
            }

            if (texture.id == null)
            {
                window.ConsoleLogError("Texture name is not present. Texture will be ignored.");
                goto skipTexture;
            }
            if (texture.category == null)
            {
                window.ConsoleLogError("Texture category is not present. Texture will be ignored.");
                goto skipTexture;
            }
            if (texture.displayName == null)
            {
                window.ConsoleLogError("Texture display name is not present. File name will be used instead.");
                texture.displayName = texture.id;
            }


            try
            {
                texture.texture = new Bitmap(@"textures/" + texture.id + ".png");
            }
            catch (Exception)
            {
                window.ConsoleLogError("Image for texture \"" + texture.id + "\" not found. Check textures directory. Texture will be ignored.");
                goto skipTexture;
            }

            if (texture.texture.Width != textureResolution || texture.texture.Height != textureResolution)
                window.ConsoleLogWarning("Dimensions of image \"" + texture.id + "\" don't match those specified in config file.");
            texture.avgColor = FindAverageColor(texture.texture);

            return texture;
        }

        private Color FindAverageColor(Bitmap image)
        {
            System.Int64 totalR = 0, totalG = 0, totalB = 0;

            for (int i = 0; i < image.Size.Width; i++)
            {
                for (int j = 0; j < image.Size.Height; j++)
                {
                    Color pixel = image.GetPixel(i, j);
                    totalR += (long)Math.Pow(pixel.R, 2);
                    totalG += (long)Math.Pow(pixel.G, 2);
                    totalB += (long)Math.Pow(pixel.B, 2);
                }
            }
            int area = image.Size.Height * image.Size.Width;

            totalR = (long)(Math.Sqrt(totalR / area));
            totalG = (long)(Math.Sqrt(totalG / area));
            totalB = (long)(Math.Sqrt(totalB / area));

            return Color.FromArgb((int)totalR, (int)totalG, (int)totalB);
        }
    }
}

