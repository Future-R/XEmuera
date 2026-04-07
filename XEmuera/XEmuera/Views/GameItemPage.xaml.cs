using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XEmuera.Models;
using Xamarin.CommunityToolkit.Extensions;
using XEmuera.Resources;
using Xamarin.Essentials;

namespace XEmuera.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class GameItemPage : ContentPage
	{
		public GameItemPage()
		{
			InitializeComponent();

			Title = StringsText.GameList;

			GameItemListView.ItemsSource = GameItemModel.AllModels;

			GameUtils.LoadCompleted -= RefreshGameList;
			GameUtils.LoadCompleted += RefreshGameList;

			RefreshGameList();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			RefreshGameList();
		}

		private void RefreshGameList()
		{
			Task.Run(() => GameItemModel.Load());
		}

		private void ListView_Refreshing(object sender, EventArgs e)
		{
			Task.Run(() =>
			{
				GameItemModel.Load();
				MainThread.BeginInvokeOnMainThread(() => GameItemListView.EndRefresh());
			});
		}

		bool CloseApplication;

		protected override bool OnBackButtonPressed()
		{
			if (CloseApplication)
				GameUtils.PlatformService.CloseApplication();

			Task.Run(async () =>
			{
				CloseApplication = true;
				CloseApplication = await this.DisplaySnackBarAsync(StringsText.BackButtonToQuit, null, null);
			});

			return true;
		}

		private void GameItemListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (!(GameItemListView.SelectedItem is GameItemModel item))
				return;

			GameItemListView.SelectedItem = null;

			GameUtils.StartEmuera(item.Path);
		}
	}
}
