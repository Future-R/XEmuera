using MinorShift.Emuera;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XEmuera.Resources;
using XEmuera.Forms;

namespace XEmuera.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainMenuPage : ContentPage
	{
		readonly ObservableCollection<MainMenuItem> MenuList = new ObservableCollection<MainMenuItem>();

		public MainMenuPage()
		{
			InitializeComponent();

			MainMenuListView.ItemsSource = MenuList;

			GameUtils.EmueraSwitched -= CheckGameReboot;
			GameUtils.EmueraSwitched -= RefreshMainMenuListView;
			GameUtils.EmueraSwitched += RefreshMainMenuListView;
			GameUtils.EmueraSwitched += CheckGameReboot;

			RefreshMainMenuListView();
		}

		private void CheckGameReboot()
        {
			if (Program.Reboot)
			{
				Program.Reboot = false;
				GameUtils.StartEmuera();
			}
		}

		private void RefreshMainMenuListView()
		{

			MenuList.Clear();

			if (GameUtils.IsEmueraPage)
			{
				MenuList.Add(new MainMenuItem
				{
					Title = StringsText.Reboot,
					Value = nameof(StringsText.Reboot),
				});

				MenuList.Add(new MainMenuItem
				{
					Title = StringsText.GotoTitle,
					Value = nameof(StringsText.GotoTitle),
				});
			}

			MenuList.Add(new MainMenuItem
			{
				Title = StringsText.Settings,
				Value = nameof(SettingsPage),
			});

			if (!string.IsNullOrWhiteSpace(GameUtils.CurrentGamePath) || GameUtils.HasPreferences(GameUtils.PrefKeyLastGamePath))
			{
				MenuList.Add(new MainMenuItem
				{
					Title = "解释器日志",
					Value = "InterpreterLog",
				});
			}
		}

		private async void MainMenuListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (!(MainMenuListView.SelectedItem is MainMenuItem item))
				return;

			MainMenuListView.SelectedItem = null;

			switch (item.Value)
			{
				case nameof(SettingsPage):
					await GameUtils.MainPage.Detail.Navigation.PushAsync(new SettingsPage());
					break;
				case "InterpreterLog":
					{
						string gamePath = GameUtils.CurrentGamePath;
						if (string.IsNullOrWhiteSpace(gamePath))
							gamePath = GameUtils.GetPreferences(GameUtils.PrefKeyLastGamePath, null);

						if (string.IsNullOrWhiteSpace(gamePath))
						{
							MessageBox.Show("当前没有可用的日志路径。", "Interpreter Log");
							break;
						}

						string logPath = System.IO.Path.Combine(gamePath, "emuera.log");
						if (!System.IO.File.Exists(logPath))
						{
							MessageBox.Show("还没有生成 emuera.log。", "Interpreter Log");
							break;
						}

						var page = new TxtFilePage(() => System.IO.File.ReadAllText(logPath, EncodingHelper.DetectEncoding(logPath)))
						{
							Title = "Interpreter Log"
						};
						await GameUtils.MainPage.Detail.Navigation.PushAsync(page);
						break;
					}
				case nameof(StringsText.Reboot):
					if (GameUtils.IsEmueraPage)
						GlobalStatic.MainWindow.MainMenu_Reboot();
					break;
				case nameof(StringsText.GotoTitle):
					if (GameUtils.IsEmueraPage)
						GlobalStatic.MainWindow.MainMenu_GotoTitle();
					break;
				default:
					break;
			}
		}

		private class MainMenuItem
		{
			public string Title { get; set; }
			public string Value { get; set; }
		}
	}
}
