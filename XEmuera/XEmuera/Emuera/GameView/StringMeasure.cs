using System;
using System.Collections.Generic;
using System.Text;
//using System.Drawing;
using MinorShift.Emuera.Sub;
using MinorShift._Library;
using XEmuera.Forms;
using XEmuera.Drawing;
using SkiaSharp;

namespace MinorShift.Emuera.GameView
{

	/// <summary>
	/// テキスト長計測装置
	/// 1819 必要になるたびにCreateGraphicsする方式をやめてあらかじめGraphicsを用意しておくことにする
	/// </summary>
	internal sealed class StringMeasure : IDisposable
	{
		private struct MeasureKey : IEquatable<MeasureKey>
		{
			public readonly string Text;
			public readonly string FontName;
			public readonly float FontSize;
			public readonly FontStyle FontStyle;

			public MeasureKey(string text, string fontName, float fontSize, FontStyle fontStyle)
			{
				Text = text;
				FontName = fontName;
				FontSize = fontSize;
				FontStyle = fontStyle;
			}

			public bool Equals(MeasureKey other)
			{
				return Text == other.Text
					&& FontName == other.FontName
					&& FontSize.Equals(other.FontSize)
					&& FontStyle == other.FontStyle;
			}

			public override bool Equals(object obj)
			{
				return obj is MeasureKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = Text != null ? Text.GetHashCode() : 0;
					hashCode = (hashCode * 397) ^ (FontName != null ? FontName.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ FontSize.GetHashCode();
					hashCode = (hashCode * 397) ^ (int)FontStyle;
					return hashCode;
				}
			}
		}

		private readonly Dictionary<MeasureKey, float> measureCache = new Dictionary<MeasureKey, float>();

		private readonly Queue<MeasureKey> measureCacheOrder = new Queue<MeasureKey>();

		private const int MeasureCacheLimit = 4096;

		private const int CachedStringLengthLimit = 256;

		public StringMeasure()
		{
			//textDrawingMode = Config.TextDrawingMode;
			//layoutSize = new Size(Config.WindowX * 2, Config.LineHeight);
			//layoutRect = new RectangleF(0, 0, Config.WindowX * 2, Config.LineHeight);
			//fontDisplaySize = Config.Font.Size / 2 * 1.04f;//実際には指定したフォントより若干幅をとる？
			////bmp = new Bitmap(Config.WindowX, Config.LineHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//graph = Graphics.FromImage(bmp);
			//if (textDrawingMode == TextDrawingMode.WINAPI)
			//	GDI.GdiMesureTextStart(graph);
		}

		//readonly TextDrawingMode textDrawingMode;
		//readonly StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
		//readonly CharacterRange[] ranges = new CharacterRange[] { new CharacterRange(0, 1) };
		//readonly Size layoutSize;
		//readonly RectangleF layoutRect;
		//readonly float fontDisplaySize;

		//readonly Graphics graph = null;
		//readonly Bitmap bmp = null;

		public int GetDisplayLength(string s, Font font)
		{
			return GetDisplayLength(s, font, 0, out _);
		}

		public int GetDisplayLength(string s, Font font, float subPixel, out float nextSubPixel)
		{
			if (string.IsNullOrEmpty(s))
			{
				nextSubPixel = subPixel;
				return 0;
			}

			if (s.Contains('\t'))
				s = s.Replace("\t", "        ");

			float measured = GetMeasuredWidth(s, font) + subPixel;
			int width = (int)measured;
			if (width == 0 && measured > 0)
				width = 1;
			nextSubPixel = measured - width;
			return width;
		}

		private float GetMeasuredWidth(string s, Font font)
		{
			if (s.Length <= CachedStringLengthLimit)
			{
				var key = new MeasureKey(s, font.FontModel.Name, font.Size, font.Style);
				if (measureCache.TryGetValue(key, out var cached))
					return cached;

				float measured = DrawTextUtils.MeasureText(s, font);
				AddMeasureCache(key, measured);
				return measured;
			}

			return DrawTextUtils.MeasureText(s, font);
		}

		private void AddMeasureCache(MeasureKey key, float value)
		{
			if (measureCache.ContainsKey(key))
				return;
			if (measureCache.Count >= MeasureCacheLimit && measureCacheOrder.Count > 0)
			{
				var oldest = measureCacheOrder.Dequeue();
				measureCache.Remove(oldest);
			}
			measureCache[key] = value;
			measureCacheOrder.Enqueue(key);
		}


		bool disposed = false;
		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			//if (textDrawingMode == TextDrawingMode.WINAPI)
			//	GDI.GdiMesureTextEnd(graph);
			//graph.Dispose();
			//bmp.Dispose();
			//sf.Dispose();

			measureCache.Clear();
			measureCacheOrder.Clear();
		}
	}
}
