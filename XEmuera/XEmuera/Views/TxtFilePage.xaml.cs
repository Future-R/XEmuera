using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using XEmuera.Forms;

namespace XEmuera.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TxtFilePage : ContentPage
	{
		private readonly Func<string> textLoader;

		public TxtFilePage(string resource)
			: this(() =>
			{
				var stream = GameUtils.GetManifestResourceStream(resource);
				using (StreamReader streamReader = new StreamReader(stream))
				{
					return streamReader.ReadToEnd();
				}
			}, allowRefresh: false)
		{
		}

		public TxtFilePage(Func<string> loader, bool allowRefresh = true)
		{
			InitializeComponent();

			BindingContext = this;
			textLoader = loader;

			ToolbarItems.Add(new ToolbarItem("Copy", null, async () =>
			{
				await Clipboard.SetTextAsync(TextView.Text ?? string.Empty);
			}));

			if (allowRefresh)
			{
				ToolbarItems.Add(new ToolbarItem("Refresh", null, LoadText));
			}

			LoadText();
		}

		private void LoadText()
		{
			try
			{
				TextView.Text = textLoader?.Invoke() ?? string.Empty;
			}
			catch (Exception e)
			{
				TextView.Text = e.ToString();
			}
		}
	}
}
