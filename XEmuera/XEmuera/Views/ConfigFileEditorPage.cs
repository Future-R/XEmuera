using System;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace XEmuera.Views
{
	public class ConfigFileEditorPage : ContentPage
	{
		private readonly string filePath;
		private readonly Editor editor;

		private Encoding fileEncoding;
		private bool isLoading;
		private bool isDirty;

		public ConfigFileEditorPage(string gameName, string filePath)
		{
			this.filePath = filePath;

			Title = $"{gameName} / emuera.config";

			editor = new Editor
			{
				AutoSize = EditorAutoSizeOption.Disabled,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				FontSize = 14,
				FontFamily = Device.RuntimePlatform == Device.Android ? "monospace" : null,
			};
			editor.TextChanged += Editor_TextChanged;

			Content = new Grid
			{
				Padding = new Thickness(10),
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
				},
				Children =
				{
					new Label
					{
						Text = filePath,
						FontSize = 12,
						TextColor = Color.Gray,
						LineBreakMode = LineBreakMode.MiddleTruncation,
					},
					editor,
				}
			};
			Grid.SetRow(editor, 1);

			ToolbarItems.Add(new ToolbarItem("Save", null, async () => await SaveAsync()));
			ToolbarItems.Add(new ToolbarItem("Reload", null, async () => await ReloadAsync()));

			LoadFile();
		}

		protected override bool OnBackButtonPressed()
		{
			if (!isDirty)
				return base.OnBackButtonPressed();

			Device.BeginInvokeOnMainThread(async () =>
			{
				bool discard = await DisplayAlert("放弃修改？", "未保存的 emuera.config 修改将丢失。", "放弃", "继续编辑");
				if (discard)
					await Navigation.PopAsync();
			});

			return true;
		}

		private void Editor_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!isLoading)
				isDirty = true;
		}

		private void LoadFile()
		{
			try
			{
				isLoading = true;
				fileEncoding = File.Exists(filePath)
					? EncodingHelper.DetectEncoding(filePath)
					: EncodingHelper.Utf8BomEncoding;
				editor.Text = File.Exists(filePath)
					? File.ReadAllText(filePath, fileEncoding)
					: string.Empty;
				isDirty = false;
			}
			catch (Exception e)
			{
				editor.Text = e.ToString();
				isDirty = false;
			}
			finally
			{
				isLoading = false;
			}
		}

		private async System.Threading.Tasks.Task ReloadAsync()
		{
			if (isDirty)
			{
				bool confirm = await DisplayAlert("重新加载？", "未保存的修改将丢失。", "重新加载", "取消");
				if (!confirm)
					return;
			}

			LoadFile();
		}

		private async System.Threading.Tasks.Task SaveAsync()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				fileEncoding ??= EncodingHelper.Utf8BomEncoding;
				File.WriteAllText(filePath, editor.Text ?? string.Empty, fileEncoding);
				isDirty = false;
				await DisplayAlert("已保存", "emuera.config 已保存。重新进入游戏后会生效。", "确定");
			}
			catch (Exception e)
			{
				await DisplayAlert("保存失败", e.ToString(), "确定");
			}
		}
	}
}
