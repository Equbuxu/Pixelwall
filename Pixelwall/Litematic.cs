using NbtLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pixelwall
{
    public class Litematic
    {
        private struct BlockState
        {
            public string name;
        }
        public string author;
        public string description;
        public string name;
        public int regioncount;

        private int totalBlocks;
        private int totalVolume;
        private int enclosingX;
        private int enclosingY;
        private int enclosingZ;

        private int regionX = 0;
        private int regionY = 0;
        private int regionZ = 0;
        private int regionSizeX;
        private int regionSizeY;
        private int regionSizeZ;

        private List<BlockState> blockStatePalette = new List<BlockState>();
        private BlockState[,,] blockStates;

        public Litematic()
        {
            blockStatePalette.Add(new BlockState() { name = "minecraft:air" });
        }

        public void Save(string path)
        {
            var root = new NbtCompoundTag();

            root.Add("Metadata", CreateMetadata());
            root.Add("Regions", CreateRegions());
            root.Add("MinecraftDataVersion", new NbtIntTag(2230));
            root.Add("Version", new NbtIntTag(5));

            using (var tag = NbtConvert.CreateNbtStream(root))
            {
                try
                {
                    using (var stream = File.OpenWrite(path))
                    {
                        tag.CopyTo(stream);
                    }
                }
                catch (IOException)
                {
                    MessageBox.Show("Couldn't save; the file is used by other program");
                }
            }

        }

        /// <summary>
        /// Does not encode block rotation
        /// </summary>
        public void SetBlock(int x, int y, int z, string id)
        {
            BlockState state = new BlockState();

            if (id == "piston_top_sticky")
                id = "sticky_piston";
            if (id.EndsWith("_vertical"))
                id = id.Replace("_vertical", "");
            if (id.EndsWith("_top"))
                id = id.Replace("_top", ""); //HACK as hacky as it gets, plz rewrite the whole project
            if (id.EndsWith("_side"))
                id = id.Replace("_side", "");
            if (id.EndsWith("_front"))
                id = id.Replace("_front", "");
            if (id.EndsWith("_back"))
                id = id.Replace("_back", "");
            if (id.EndsWith("_bottom"))
                id = id.Replace("_bottom", "");

            id = "minecraft:" + id;
            for (int i = 0; i < blockStatePalette.Count; i++)
            {

                if (blockStatePalette[i].name == id)
                {
                    state = blockStatePalette[i];
                    break;
                }
            }

            if (state.name == null)
            {
                state.name = id;
                blockStatePalette.Add(state);
            }

            blockStates[x, z, y] = state;
        }

        public void SetSize(int x, int y, int z)
        {
            enclosingX = regionSizeX = x;
            enclosingY = regionSizeY = y;
            enclosingZ = regionSizeZ = z;
            blockStates = new BlockState[regionSizeX, regionSizeZ, regionSizeY];
        }

        private NbtCompoundTag CreateMetadata()
        {
            var metadata = new NbtCompoundTag();
            var enclosingSize = new NbtCompoundTag();
            metadata.Add("EnclosingSize", enclosingSize);

            enclosingSize.Add("x", new NbtIntTag(enclosingX));
            enclosingSize.Add("y", new NbtIntTag(enclosingY));
            enclosingSize.Add("z", new NbtIntTag(enclosingZ));

            metadata.Add("Author", new NbtStringTag(author));
            metadata.Add("Description", new NbtStringTag(description));
            metadata.Add("Name", new NbtStringTag(name));

            metadata.Add("RegionCount", new NbtIntTag(1));
            metadata.Add("TotalBlocks", new NbtIntTag(totalBlocks));
            metadata.Add("TotalVolume", new NbtIntTag(totalVolume));

            metadata.Add("TimeCreated", new NbtLongTag(DateTime.UtcNow.Ticks));
            metadata.Add("TimeModified", new NbtLongTag(DateTime.UtcNow.Ticks));

            return metadata;
        }

        private NbtCompoundTag CreateRegions()
        {
            var regions = new NbtCompoundTag();
            var region = new NbtCompoundTag();
            var position = new NbtCompoundTag();
            var size = new NbtCompoundTag();

            regions.Add(name, region);

            region.Add("Position", position);
            position.Add("x", new NbtIntTag(regionX));
            position.Add("y", new NbtIntTag(regionY));
            position.Add("z", new NbtIntTag(regionZ));

            region.Add("Size", size);
            size.Add("x", new NbtIntTag(regionSizeX));
            size.Add("y", new NbtIntTag(regionSizeY));
            size.Add("z", new NbtIntTag(regionSizeZ));

            region.Add("Entities", new NbtListTag(NbtTagType.Compound));
            region.Add("PendingBlockTicks", new NbtListTag(NbtTagType.Compound));
            region.Add("PendingFluidTicks", new NbtListTag(NbtTagType.Compound));
            region.Add("TileEntities", new NbtListTag(NbtTagType.Compound));

            region.Add("BlockStatePalette", EncodePalette());
            region.Add("BlockStates", new NbtLongArrayTag(EncodeBlocks()));

            return regions;
        }

        private long[] EncodeBlocks()
        {
            int bitCount = (int)Math.Ceiling(Math.Log(blockStatePalette.Count, 2));
            if (bitCount < 2)
                bitCount = 2;

            //encode each number as binary
            List<List<bool>> uncompressedNumbers = new List<List<bool>>();
            for (int y = 0; y < blockStates.GetLength(2); y++)
            {
                for (int z = 0; z < blockStates.GetLength(1); z++)
                {
                    for (int x = 0; x < blockStates.GetLength(0); x++)
                    {
                        int index = blockStatePalette.IndexOf(blockStates[x, z, y]);

                        List<bool> bits = new List<bool>();
                        for (int k = bitCount - 1; k >= 0; k--)
                        {
                            bits.Add(((index >> k) & 1) == 1);
                        }
                        uncompressedNumbers.Add(bits);
                    }
                }
            }


            List<bool> uncompressedBits = new List<bool>(bitCount * regionSizeX * regionSizeY * regionSizeZ);
            //add bits in reverse order
            for (int i = 0; i < uncompressedNumbers.Count; i += 1)
            {
                for (int j = bitCount - 1; j >= 0; j--)
                {
                    uncompressedBits.Add(uncompressedNumbers[i][j]);
                }
            }

            //append 0s to a multiple of 64
            int finalLength = (int)Math.Ceiling(uncompressedBits.Count / 64.0);
            int add = finalLength * 64 - uncompressedBits.Count;
            for (int i = 0; i < add; i++)
            {
                uncompressedBits.Add(false);
            }

            //reverse each byte
            for (int i = 0; i < uncompressedBits.Count; i += 8)
            {
                for (int j = 0; j < 4; j++)
                {
                    int opposite = 7 - j;
                    bool temp = uncompressedBits[i + j];
                    uncompressedBits[i + j] = uncompressedBits[i + opposite];
                    uncompressedBits[i + opposite] = temp;
                }
            }

            //convert bool array to long array
            long[] final = new long[(int)Math.Ceiling(uncompressedBits.Count / 64f)];
            for (int i = 0; i < finalLength; i++)
            {
                byte[] bytes = new byte[8];
                for (int j = 0; j < 8; j++)
                {
                    byte result = 0;
                    int index = i * 64 + j * 8;
                    int startIndex = index;

                    for (int k = startIndex; k < startIndex + 8; k++)
                    {
                        if (k < uncompressedBits.Count ? uncompressedBits[k] : false)
                            result |= (byte)(1 << (7 - (k - startIndex)));

                        index++;
                    }
                    bytes[j] = result;
                }
                final[i] = BitConverter.ToInt64(bytes, 0);
            }

            return final;
        }

        private NbtListTag EncodePalette()
        {
            var container = new NbtListTag(NbtTagType.Compound);

            foreach (BlockState state in blockStatePalette)
            {
                var compound = new NbtCompoundTag();
                compound.Add("Name", new NbtStringTag(state.name));
                container.Add(compound);
            }

            return container;
        }
    }
}
