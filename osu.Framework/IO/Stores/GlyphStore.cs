﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cyotek.Drawing.BitmapFont;
using osu.Framework.Graphics.Textures;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<RawTexture>
    {
        private string assetName;

        private string fontName;

        const float default_size = 96;

        ResourceStore<byte[]> store;
        private BitmapFont font;

        Dictionary<int, RawTexture> texturePages = new Dictionary<int, RawTexture>();

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null, bool precache = false)
        {
            this.store = store;
            this.assetName = assetName;

            fontName = assetName.Split('/').Last();

            try
            {
                font = new BitmapFont();
                font.LoadText(store.GetStream($@"{assetName}.fnt"));

                if (precache)
                    for (int i = 0; i < font.Pages.Length; i++)
                        getTexturePage(i);
            }
            catch
            {
                throw new FontLoadException(assetName);
            }
        }

        public RawTexture Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{fontName}/"))
                return null;

            Character c;

            if (!font.Characters.TryGetValue(name.Last(), out c))
                return null;

            RawTexture page = getTexturePage(c.TexturePage);

            int width = c.Bounds.Width + c.Offset.X + 1;
            int height = c.Bounds.Height + c.Offset.Y + 1;
            int length = width * height * 4;
            byte[] pixels = new byte[length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int desti = y * width * 4 + x * 4;
                    if (x >= c.Offset.X && y >= c.Offset.Y
                        && x - c.Offset.X < c.Bounds.Width && y - c.Offset.Y < c.Bounds.Height)
                    {
                        int srci = (c.Bounds.Y + y - c.Offset.Y) * page.Width * 4
                            + (c.Bounds.X + x - c.Offset.X) * 4;
                        pixels[desti] = page.Pixels[srci];
                        pixels[desti + 1] = page.Pixels[srci + 1];
                        pixels[desti + 2] = page.Pixels[srci + 2];
                        pixels[desti + 3] = page.Pixels[srci + 3];
                    }
                    else
                    {
                        pixels[desti] = 255;
                        pixels[desti + 1] = 255;
                        pixels[desti + 2] = 255;
                        pixels[desti + 3] = 0;
                    }
                }
            }

            return new RawTexture
            {
                Pixels = pixels,
                PixelFormat = OpenTK.Graphics.ES30.PixelFormat.Rgba,
                Width = width,
                Height = height,
            };
        }

        private RawTexture getTexturePage(int texturePage)
        {
            RawTexture t;
            if (!texturePages.TryGetValue(texturePage, out t))
            {
                using (var stream = store.GetStream($@"{assetName}_{texturePage}.png"))
                    texturePages[texturePage] = t = RawTexture.FromStream(stream);
            }

            return t;
        }

        public Stream GetStream(string name)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FontLoadException : Exception
    {
        public FontLoadException(string assetName) :
            base($@"Couldn't load font asset from {assetName}.")
        {
        }
    }

    public class FontStore : TextureStore
    {
        public FontStore()
        {
        }
   
        public FontStore(GlyphStore glyphStore) : base(glyphStore)
        {
        }
    }
}
