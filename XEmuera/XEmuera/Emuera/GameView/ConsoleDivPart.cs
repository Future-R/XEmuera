using SkiaSharp;
using System;
using System.Drawing;
using System.Text;
using XEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{
	class ConsoleDivPart : AConsoleDisplayPart
	{
		private readonly int xOffset;
		private readonly int yOffset;
		private readonly int divWidth;
		private readonly int divHeight;
		private readonly Color backgroundColor;
		private readonly ConsoleDisplayLine[] children;

		public ConsoleDivPart(MixedNum xPos, MixedNum yPos, MixedNum width, MixedNum height, int color, bool isRelative, ConsoleDisplayLine[] childLines)
		{
			Str = string.Empty;
			IsRelative = isRelative;
			xOffset = MixedNumToPixel(xPos, 0);
			yOffset = MixedNumToPixel(yPos, 0);
			divWidth = Math.Abs(MixedNumToPixel(width, 0));
			divHeight = Math.Abs(MixedNumToPixel(height, 0));
			backgroundColor = color >= 0
				? Color.FromArgb(255, (color >> 16) & 0xFF, (color >> 8) & 0xFF, color & 0xFF)
				: Color.Transparent;
			children = childLines ?? Array.Empty<ConsoleDisplayLine>();
			AltText = BuildAltText(xPos, yPos, width, height, color);
		}

		public bool IsRelative { get; }

		public override int Top => yOffset;

		public override int Bottom => yOffset + divHeight;

		public override bool CanDivide => false;

		public override void SetWidth(StringMeasure sm, float subPixel)
		{
			Width = 0;
			XsubPixel = subPixel;
		}

		public override void DrawTo(SKCanvas graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
		{
			int drawX = (IsRelative ? PointX : 0) + xOffset;
			int drawY = (IsRelative ? pointY : 0) + yOffset;
			int childPointY = drawY;

			graph.Save();
			if (divWidth > 0 && divHeight > 0)
			{
				if (backgroundColor.A > 0)
					DrawBitmapUtils.DrawRect(graph, new SolidBrush(backgroundColor), new Rectangle(drawX, drawY, divWidth, divHeight));
				graph.ClipRect(new SKRect(drawX, drawY, drawX + divWidth, drawY + divHeight));
			}

			foreach (var child in children)
			{
				child.ShiftPositionX(drawX);
				child.DrawTo(graph, childPointY, isBackLog, true, mode);
				child.ShiftPositionX(-drawX);
				childPointY += Config.LineHeight;
			}

			graph.Restore();
		}

		private static int MixedNumToPixel(MixedNum num, int defaultValue)
		{
			if (num == null)
				return defaultValue;
			return num.isPx ? num.num : Config.FontSize * num.num / 100;
		}

		private static string BuildAltText(MixedNum xPos, MixedNum yPos, MixedNum width, MixedNum height, int color)
		{
			StringBuilder builder = new StringBuilder("<div");
			AppendMixedNum(builder, "xpos", xPos);
			AppendMixedNum(builder, "ypos", yPos);
			AppendMixedNum(builder, "width", width);
			AppendMixedNum(builder, "height", height);
			if (color >= 0)
				builder.Append(" color='#").Append(color.ToString("X6")).Append("'");
			builder.Append(">");
			return builder.ToString();
		}

		private static void AppendMixedNum(StringBuilder builder, string name, MixedNum value)
		{
			if (value == null)
				return;
			builder.Append(' ').Append(name).Append("='").Append(value.num);
			if (value.isPx)
				builder.Append("px");
			builder.Append("'");
		}
	}
}
