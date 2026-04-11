using System;
using Xamarin.Forms;

namespace XEmuera.Views
{
	public class GameItemViewCell : ViewCell
	{
		public event EventHandler LongPressed;

		public void RaiseLongPressed()
		{
			LongPressed?.Invoke(this, EventArgs.Empty);
		}
	}
}
