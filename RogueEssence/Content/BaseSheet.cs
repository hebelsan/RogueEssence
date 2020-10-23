﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace RogueEssence.Content
{
    public class BaseSheet : IDisposable
    {

        protected static GraphicsDevice device;
        protected static Texture2D defaultTex;

        protected Texture2D baseTexture { get; private set; }

        public int Width { get { return baseTexture.Width; } }
        public int Height { get { return baseTexture.Height; } }

        public long MemSize { get; private set; }

        public static void InitBase(GraphicsDevice graphicsDevice, Texture2D tex)
        {
            device = graphicsDevice;
            defaultTex = tex;
        }

        public BaseSheet(int width, int height)
        {
            baseTexture = new Texture2D(device, width, height);
            MemSize = -1;
        }

        protected BaseSheet(Texture2D tex)
        {
            baseTexture = tex;
            MemSize = -1;
        }

        public virtual void Dispose()
        {
            if (baseTexture != defaultTex)
                baseTexture.Dispose();
        }

        ~BaseSheet()
        {
            Dispose();
        }

        //frompath (import) will take a raw png
        //fromstream (load) will also take a raw png from stream
        //save will save as png

        public static BaseSheet Import(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Texture2D tex = ImportTex(stream);
                return new BaseSheet(tex);
            }
        }

        public static BaseSheet Load(BinaryReader reader)
        {
            long length = reader.ReadInt64();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(reader.ReadBytes((int)length), 0, (int)length);
                ms.Position = 0;
                Texture2D tex = Texture2D.FromStream(device, ms);
                return new BaseSheet(tex);
            }
        }

        public static BaseSheet LoadError()
        {
            return new BaseSheet(defaultTex);
        }

        public virtual void Save(BinaryWriter writer)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                baseTexture.SaveAsPng(stream, baseTexture.Width, baseTexture.Height);
                MemSize = stream.Position;
                writer.Write(MemSize);
                writer.Write(stream.ToArray());
            }
        }

        public void Export(Stream stream)
        {
            ExportTex(stream, baseTexture);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle? sourceRect)
        {
            Draw(spriteBatch, pos, sourceRect, Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle? sourceRect, Color color)
        {
            Draw(spriteBatch, pos, sourceRect, color, new Vector2(1));
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle? sourceRect, Color color, Vector2 scale)
        {
            Draw(spriteBatch, pos, sourceRect, color, scale, SpriteEffects.None);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle? sourceRect, Color color, Vector2 scale, SpriteEffects effect)
        {
            spriteBatch.Draw(baseTexture, pos, sourceRect, color, 0f, Vector2.Zero, scale, effect, 0);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle sourceRect, Color color, Vector2 scale, float rotation)
        {
            Draw(spriteBatch, pos, sourceRect, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), color, scale, rotation);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Rectangle sourceRect, Vector2 origin, Color color, Vector2 scale, float rotation)
        {
            spriteBatch.Draw(baseTexture, pos, sourceRect, color, rotation, origin, scale, SpriteEffects.None, 0);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle destRect, Rectangle? sourceRect, Color color)
        {
            spriteBatch.Draw(baseTexture, destRect, sourceRect, color, 0f, Vector2.Zero, SpriteEffects.None, 0);
        }

        public void DrawDefault(SpriteBatch spriteBatch, Rectangle destRect)
        {
            spriteBatch.Draw(defaultTex, destRect, Color.White);
        }

        public bool IsBlank(int srcPx, int srcPy, int srcW, int srcH)
        {
            Color[] color = new Color[srcW * srcH];
            baseTexture.GetData<Color>(0, new Rectangle(srcPx, srcPy, srcW, srcH), color, 0, color.Length);
            for (int ii = 0; ii < srcW * srcH; ii++)
            {
                if (color[ii].A > 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Multiplies all colors by the alpha, or divides if reversed.
        /// Used to conform with XNA's particular method of rendering.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="reverse"></param>
        private static void premultiply(Texture2D tex, bool reverse)
        {
            Color[] color = new Color[tex.Width * tex.Height];
            tex.GetData<Color>(0, null, color, 0, color.Length);
            for (int ii = 0; ii < tex.Width * tex.Height; ii++)
            {
                if (reverse)
                    color[ii] = new Color(color[ii].R * 255 / color[ii].A, color[ii].G * 255 / color[ii].A, color[ii].B * 255 / color[ii].A, color[ii].A);
                else
                    color[ii] = new Color(color[ii].R * color[ii].A / 255, color[ii].G * color[ii].A / 255, color[ii].B * color[ii].A / 255, color[ii].A);
            }
            tex.SetData<Color>(0, null, color, 0, color.Length);
        }


        public static Texture2D ImportTex(Stream stream)
        {
            Texture2D tex = Texture2D.FromStream(device, stream);
            premultiply(tex, false);
            return tex;
        }

        public static void ExportTex(Stream stream, Texture2D tex)
        {
            Texture2D tempTex = CreateTexCopy(tex);
            premultiply(tempTex, true);
            tempTex.SaveAsPng(stream, tempTex.Width, tempTex.Height);
            tempTex.Dispose();
        }

        public static Texture2D CreateTexCopy(Texture2D source)
        {
            Texture2D copy = new Texture2D(device, source.Width, source.Height);
            Color[] color = new Color[source.Width * source.Height];
            source.GetData<Color>(0, null, color, 0, color.Length);
            copy.SetData<Color>(0, null, color, 0, color.Length);
            return copy;
        }

        public void Blit(BaseSheet source, int srcPx, int srcPy, int srcW, int srcH, int destX, int destY)
        {
            BaseSheet.Blit(source.baseTexture, baseTexture, srcPx, srcPy, srcW, srcH, destX, destY);
        }

        public static void Blit(Texture2D source, Texture2D dest, int srcPx, int srcPy, int srcW, int srcH, int destX, int destY)
        {
            Color[] color = new Color[srcW * srcH];
            source.GetData<Color>(0, new Rectangle(srcPx, srcPy, srcW, srcH), color, 0, color.Length);
            dest.SetData<Color>(0, new Rectangle(destX, destY, srcW, srcH), color, 0, color.Length);
        }

        public void BlitColor(Color srcColor, int srcW, int srcH, int destX, int destY)
        {
            BaseSheet.BlitColor(srcColor, baseTexture, srcW, srcH, destX, destY);
        }

        public static void BlitColor(Color srcColor, Texture2D dest, int srcW, int srcH, int destX, int destY)
        {
            Color[] color = new Color[srcW * srcH];
            for (int ii = 0; ii < color.Length; ii++)
                color[ii] = srcColor;
            dest.SetData<Color>(0, new Rectangle(destX, destY, srcW, srcH), color, 0, color.Length);
        }

        public Color GetPixel(int x, int y)
        {
            Color[] color = new Color[1];
            baseTexture.GetData<Color>(0, new Rectangle(x, y, 1, 1), color, 0, 1);
            return color[0];
        }
    }
}
