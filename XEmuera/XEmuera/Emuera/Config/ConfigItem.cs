﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using MinorShift.Emuera.Sub;
using XEmuera;
using SkiaSharp;
using EvilMask.Emuera;

namespace MinorShift.Emuera
{

	internal sealed class ConfigItem
	{
		#region EM_私家版_Emuera多言語化改造
		public static ConfigItem Copy(ConfigItem other)
		{
			if (other == null)
				return null;
			ConfigItem ret = new ConfigItem(other.Code, other.Text, other.EngText, other.Value)
			{
				Name = other.Name,
				Fixed = other.Fixed
			};
			return ret;
		}
		#endregion

		public ConfigCode Code { get; private set; }

		public string Name { get; private set; }

		public string Text { get; private set; }

		#region EM_私家版_Emuera多言語化改造
		public string EngText { get; private set; }
		#endregion

		public bool Fixed { get; set; }

		public object Value { get; set; }

		public object DefaultValue { get; private set; }

		#region EM_私家版_Emuera多言語化改造
		public ConfigItem(ConfigCode code, string text, string etext, object value)
		{
			this.Code = code;
			this.Name = code.ToString();
			this.Text = text.ToUpper();
			this.EngText = etext.ToUpper();
			this.Value = value;
			this.DefaultValue = value;
		}
		#endregion

		public void CopyTo(ConfigItem other)
		{
			other.Value = this.Value;
			other.Fixed = this.Fixed;
			other.DefaultValue = this.Value;
		}

		public void SetValue(object value)
		{
			//if (this is ConfigItem<U>)
			Value = value;
			//else
			//    throw new ExeEE("型が一致しない");
		}

		public U GetValue<U>()
		{
			////if (this is ConfigItem<U>)
			return (U)Value;
			//throw new ExeEE("型が一致しない");
		}

		public void ResetDefault()
		{
			Value = DefaultValue;
		}

		internal static string ValueToString(object value)
		{
			if (value is bool b)
			{
				//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
				return b ? "TRUE" : "FALSE";
			}
			if (value is Color c)
			{
				return string.Format("{0},{1},{2}", c.R, c.G, c.B);
			}

			#region EM_私家版_LoadText＆SaveText機能拡張
			if (value is List<string>)
			{
				var sb = new StringBuilder();
				var v = value;
				foreach (var str in (List<string>)v)
				{
					if (sb.Length > 0)
						sb.Append(",");
					sb.Append(str);
				}
				return sb.ToString();
			}
			#endregion
			return value.ToString();
		}

		public override string ToString()
		{
			#region EM_私家版_Emuera多言語化改造
			return (Config.EnglishConfigOutput ? EngText : Text) + ":" + ValueToString(Value);
			#endregion
		}



		/// ジェネリック化大失敗。なんかうまい方法ないかな～
		public static bool TryParse(ConfigItem item, string param)
		{
			if (string.IsNullOrEmpty(param))
				return false;
			if (item.Fixed)
				return false;

			bool ret = false;
			string str = param.Trim();
			object Value = item.Value;

			if (Value is bool)
			{
				ret = TryStringToBool(str, out bool b);
				if (ret)//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
					Value = b;
			}
			else if (Value is Color)
			{
				ret = TryStringsToColor(str, out Color c);
				if (ret)
					Value = c;
                else
                    throw new CodeEE(Lang.Error.NotExistColorSpecifier.Text);
            }
			else if (Value is char)
			{
				ret = char.TryParse(str, out char c);
				if (ret)
					Value = c;
			}
			else if (Value is char)
			{
				ret = char.TryParse(str, out char c);
				if (ret)
					Value = c;
			}
			else if (Value is int)
			{
				ret = int.TryParse(str, out int i);
				if (ret)
					Value = i;
				else
					throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
			}
			else if (Value is long)
			{
				ret = long.TryParse(str, out long l);
				if (ret)
					Value = l;
				else
					throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
			}
			else if (Value is float)
			{
				ret = float.TryParse(str, out float f);
				if (ret)
					Value = f;
				else
					throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
			}
			else if (Value is List<long> longlist)
			{
				longlist.Clear();
				string[] strs = str.Split('/');
				foreach (string st in strs)
				{
					ret = long.TryParse(st.Trim(), out long l);
					if (ret)
						longlist.Add(l);
					else
					{
						throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
					}
				}
			}
			else if (Value is string)
			{
				ret = true;
				Value = str;
			}
			else if (Value is List<string> stringlist)
			{
				{
					#region EM_私家版_LoadText＆SaveText機能拡張
					ret = true;
					var list = stringlist;
					TryStringToStringList(str, ref list);
					Value = list;
					#endregion
				}
			}
			else if (Value is TextDrawingMode)
			{
				str = str.ToUpper();
				ret = Enum.IsDefined(typeof(TextDrawingMode), str);
				if (ret)
				{
					Value = (TextDrawingMode)Enum.Parse(typeof(TextDrawingMode), str);
				}
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			else if (Value is ReduceArgumentOnLoadFlag)
			{
				str = str.ToUpper();
				ret = Enum.IsDefined(typeof(ReduceArgumentOnLoadFlag), str);
				if (ret)
				{
					Value = (ReduceArgumentOnLoadFlag)Enum.Parse(typeof(ReduceArgumentOnLoadFlag), str);
				}
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			else if (Value is DisplayWarningFlag)
			{
				str = str.ToUpper();
				ret = Enum.IsDefined(typeof(DisplayWarningFlag), str);
				if (ret)
				{
					Value = (DisplayWarningFlag)Enum.Parse(typeof(DisplayWarningFlag), str);
				}
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			else if (Value is UseLanguage)
			{
				str = str.ToUpper();
				ret = Enum.IsDefined(typeof(UseLanguage), str);
				if (ret)
				{
					Value = (UseLanguage)Enum.Parse(typeof(UseLanguage), str);
				}
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			else if (Value is TextEditorType)
			{
				str = str.ToUpper();
				ret = Enum.IsDefined(typeof(TextEditorType), str);
				if (ret)
				{
					Value = (TextEditorType)Enum.Parse(typeof(TextEditorType), str);
				}
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			else if (Value is SKFilterQuality)
			{
				ret = Enum.TryParse(str, true, out SKFilterQuality result);
				if (ret)
					Value = result;
				else
					throw new CodeEE(Lang.Error.InvalidSpecification.Text);
			}
			//else
			//    ShowError("型不明なコンフィグ");
			item.Value = Value;
			return ret;
		}


		#region EM_私家版_LoadText＆SaveText機能拡張
		static private bool TryStringToStringList(string arg, ref List<string> vs)
		{
			string[] tokens = arg.Split(',');
			vs.Clear();
			foreach (var token in tokens)
			{
				vs.Add(token.Trim());
			}
			return true;
		}
		#endregion

		private static void ShowError(string errorMessage)
		{
			if (GameUtils.IsEmueraPage)
				throw new CodeEE(errorMessage);
			//else
			//	MessageBox.QuickShow(errorMessage);
		}

		private static bool TryStringToBool(string arg, out bool p)
		{
			if (arg == null)
			{
				p = false;
				return false;
			}
			string str = arg.Trim();
			if (Int32.TryParse(str, out int i))
			{
				p = (i != 0);
				return true;
			}
			if (str.Equals("NO", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("FALSE", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("後", StringComparison.CurrentCultureIgnoreCase))//"単位の位置"用
			{
				p = false;
				return true;
			}
			if (str.Equals("YES", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("TRUE", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("前", StringComparison.CurrentCultureIgnoreCase))
			{
				p = true;
				return true;
			}
			throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}

		public static bool TryStringsToColor(string str, out Color c)
		{
			string[] tokens = str.Split(',');
			c = Color.Black;
			if (tokens.Length < 3)
				return false;
			if (!Int32.TryParse(tokens[0].Trim(), out Int32 r) || (r < 0) || (r > 255))
				return false;
			if (!Int32.TryParse(tokens[1].Trim(), out Int32 g) || (g < 0) || (g > 255))
				return false;
			if (!Int32.TryParse(tokens[2].Trim(), out Int32 b) || (b < 0) || (b > 255))
				return false;
			c = Color.FromArgb(r, g, b);
			return true;
		}
	}
}
