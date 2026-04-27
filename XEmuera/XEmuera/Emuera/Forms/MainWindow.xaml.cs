//need check

using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MinorShift._Library;
using XEmuera;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XEmuera.Forms;
using Xamarin.CommunityToolkit.Extensions;
using System.ComponentModel;
using XEmuera.Views;
using XEmuera.Resources;
using System.Timers;
using XEmuera.Models;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameProc.Function;

namespace MinorShift.Emuera
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainWindow : ContentPage, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler NotifyPropertyChanged;

		public Color MainColor
		{
			get => _mainColor;
			set
			{
				if (_mainColor == value)
					return;
				_mainColor = value;
				InvertMainColor = DisplayUtils.InvertColor(_mainColor).WithAlpha(0x80);
				NotifyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainColor)));
			}
		}
		private Color _mainColor;

		public Color InvertMainColor
		{
			get => _invertMainColor;
			set
			{
				if (_invertMainColor == value)
					return;
				_invertMainColor = value;
				NotifyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InvertMainColor)));
			}
		}
		private Color _invertMainColor;

		bool IsInitGameView;

		bool IsWindowClosing;

		List<TintImageButton> InvisibleToolButtonList;

		private MainWindow()
		{
			InitializeComponent();

			GameUtils.MainLayout = mainLayout;
			GameUtils.MainPicBox = mainPicBox;

			GlobalStatic.MainWindow = this;

			Sys.Init();
			if (!Program.Init())
			{
				LoadSuccess = false;
				return;
			}

			this.SetBinding(BackgroundColorProperty, nameof(MainColor), BindingMode.TwoWay);

			MainColor = Config.ForeColor;

			ToolButtonGroup.BindingContext = this;
			entryGroup.BindingContext = this;

			InvisibleToolButtonList = new List<TintImageButton>
			{
				scroll_vertical_button,
				gallery_view_button,
			};

			LongPressTimer = new Timer(Config.LongPressSkipTime);
			LongPressTimer.Elapsed += LongPressTimer_Elapsed;

			InitGameView();
			InitEmuera();
			StartupFontScale = Math.Max(0.01f, Config.FontScale);
			ApplyRuntimeDisplayScale(Config.FontScale, false);
			SetButtonOpacity(virtual_controller_button, virtual_controller_button.IsToggled ?? false);
			RefreshVirtualControllerState();
		}

		private void LongPressTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			LongPressTimer.Enabled = false;

			if (IsMouseMove(PrevPoint))
				return;

			IsLongPressed = true;
			//PressEnterKey(null);
		}

		static bool LoadSuccess;

		public static MainWindow Load()
		{
			LoadSuccess = true;
			try
			{
				var mainWindow = new MainWindow();
				return LoadSuccess ? mainWindow : null;
			}
			catch (Exception e)
			{
				LoadSuccess = false;
				MessageBox.Show(e.ToString(), "MainWindow.Load");
				return null;
			}
		}

		private double ScaledWindowX;
		private float StartupFontScale;
		private float CurrentRuntimeScale = 1f;

		private void InitGameView()
		{
			int originalWindowX = ConfigData.Instance.GetConfigValue<int>(ConfigCode.WindowX);
			int originalFontSize = ConfigData.Instance.GetConfigValue<int>(ConfigCode.FontSize);
			ScaledWindowX = originalWindowX * Config.FontScale * Config.FontSize / originalFontSize / DisplayUtils.ScreenDensity;

			mainLayout.Children.Clear();

			mainLayout.Children.Add(mainPicBox,
				null,
				null,
				Constraint.RelativeToParent(parent => Math.Max(parent.Width, ScaledWindowX)),
				Constraint.RelativeToParent(parent => parent.Height));

			mainLayout.Children.Add(uiLayout,
				null,
				null,
				Constraint.RelativeToParent(parent => parent.Width),
				Constraint.RelativeToParent(parent => parent.Height));

			mainLayout.Children.Add(virtualControllerLayout,
				null,
				null,
				Constraint.RelativeToParent(parent => parent.Width),
				Constraint.RelativeToParent(parent => parent.Height));
		}

		protected override void OnAppearing()
		{
			GameUtils.IsEmueraPage = true;
			base.OnAppearing();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			GameUtils.IsEmueraPage = false;
			GameUtils.PlatformService.UnlockScreenOrientation();
		}

		private void uiLayout_SizeChanged(object sender, EventArgs e)
		{
			if (IsWindowClosing)
				return;

			RefreshQuickButtonGroup();
			RefreshScrollBarLayout();
		}

		private void ContentPage_SizeChanged(object sender, EventArgs e)
		{
			if (IsWindowClosing)
				return;

			if (IsInitGameView)
			{
				UpdateView();
				return;
			}

			IsInitGameView = true;

			Config.RefreshDisplayConfig();

			//InitEmuera();
		}

		private void UpdateView()
		{
			Config.RefreshDisplayConfig();
			console.setStBar(Config.DrawLineString);
			mainPicBox.TranslationX = 0;
			ApplyRuntimeDisplayScale(Config.FontScale, false);
		}

		public void ApplyRuntimeDisplayScale(float fontScale, bool refreshSurface = true)
		{
			float clampedScale = Math.Max(0.5f, Math.Min(5.0f, fontScale));
			Config.SetRuntimeFontScale(clampedScale);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				Config.ApplyRuntimeDisplaySettings(ConfigData.Instance);
				int originalWindowX = ConfigData.Instance.GetConfigValue<int>(ConfigCode.WindowX);
				int originalFontSize = ConfigData.Instance.GetConfigValue<int>(ConfigCode.FontSize);
				ScaledWindowX = originalWindowX * Config.FontScale * Config.FontSize / originalFontSize / DisplayUtils.ScreenDensity;
				InitGameView();
				mainPicBox.Scale = 1f;
				mainPicBox.TranslationX = 0;
				CurrentRuntimeScale = 1f;
				console?.ReflowDisplayLinesForCurrentScale();
				RefreshQuickButtonGroup();
				RefreshScrollBarLayout();
				RefreshVirtualControllerState();
				if (refreshSurface)
					mainPicBox.InvalidateSurface();
			});
		}

		private void AdjustRuntimeDisplayScale(float delta)
		{
			float newScale = (float)Math.Round(Config.FontScale + delta, 2);
			newScale = Math.Max(0.5f, Math.Min(5.0f, newScale));

			var model = ConfigModel.Get(ConfigCode.FontScale);
			if (model != null)
			{
				model.ConfigItem.Value = newScale;
				model.UpdateValue();
				ApplyRuntimeDisplayScale(newScale);
			}
			else
			{
				ApplyRuntimeDisplayScale(newScale);
			}
		}

		private SKPoint ToLogicalTouchPoint(SKPoint location)
		{
			if (Math.Abs(CurrentRuntimeScale - 1f) < 0.0001f)
				return location;

			float logicalX = location.X / CurrentRuntimeScale;
			float logicalY = (float)(mainPicBox.Height - ((mainPicBox.Height - location.Y) / CurrentRuntimeScale));
			return new SKPoint(logicalX, logicalY);
		}

		public void RefreshQuickButtonGroup()
		{
			quickButtonScrollView.WidthRequest = Math.Min(quickButtonGroup.Width, uiLayout.Width * 3 / 5);
			quickButtonScrollView.HeightRequest = Math.Min(quickButtonGroup.Height, uiLayout.Height - ToolButtonGroup.Height - 30d);

			quickButtonScrollView.ScrollToAsync(0, quickButtonGroup.Height, false);
		}

		private void RefreshScrollBarLayout()
		{
			ScrollBarLayout.WidthRequest = Math.Min(400d, uiLayout.Height - ToolButtonGroup.Height - 40d);
		}

		protected override bool OnBackButtonPressed()
		{
			if (console != null && console.IsError)
			{
				Close();
			}
			else if (!IsInitializing(true))
			{
				MessageBox.ShowOnMainThread(StringsText.QuitGameConfirm, null, result =>
				{
					if (result)
						Close();
				}, MessageBoxButtons.YesNo);
			}

			return true;
		}

		private bool IsInitializing(bool showMessage = false)
		{
			// Process is null mostly because of closing
			if (GlobalStatic.Process == null || GlobalStatic.Process.inInitializeing)
			{
				if (showMessage)
					this.DisplaySnackBarAsync(StringsText.GameIsProcessing, null, null);
				return true;
			}
			return false;
		}

		public void Close()
		{
			IsWindowClosing = true;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				//if (Config.UseKeyMacro)
				//	KeyMacro.SaveMacro();
				FunctionIdentifier.StopAllAudio();
				if (console != null)
				{
					console.Quit();
					console.Dispose();
					//console = null;
				}
				Program.RebootMain();
				Navigation.PopAsync();
				if (!Program.Reboot)
					GC.Collect();
			});
		}

		bool IsMouseMoveAction;

		SKPoint StartPoint;
		SKPoint PrevPoint;
		System.Drawing.Point MouseLocation;
		Point MoveDistance;

		double moveX;
		double moveY;

		double PicBoxMinX;
		double PicBoxMaxX;

		Timer LongPressTimer;
		bool IsLongPressed;

		/// <summary>
		/// 获取画板的点击位置和滑动距离
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void EraPictureBox_Touch(object sender, SKTouchEventArgs e)
		{
			SKPoint logicalPoint = ToLogicalTouchPoint(e.Location);
			MouseLocation = new System.Drawing.Point((int)logicalPoint.X, (int)logicalPoint.Y);

			if (console.MoveMouse(MouseLocation))
				RefreshStrings(true);

			switch (e.ActionType)
			{
				//case SKTouchAction.Entered:
				//	break;
				case SKTouchAction.Pressed:

					IsMouseMoveAction = false;
					IsDragScrollBar = true;
					PrevPoint = logicalPoint;
					StartPoint = PrevPoint;

					IsLongPressed = false;
					if (Config.LongPressSkip)
						LongPressTimer.Enabled = true;

					PicBoxMinX = Math.Min(0d, mainLayout.Width - mainPicBox.Width * CurrentRuntimeScale);
					PicBoxMaxX = 0;

					MoveDistance = Point.Zero;
					moveY = (double)Config.LineHeight / Config.ScrollHeight;

					e.Handled = true;
					return;

				case SKTouchAction.Moved:

					IsMouseMoveAction = true;

					//mainPicBox
					MoveDistance.X += logicalPoint.X - PrevPoint.X;

					if (Config.PanSpeed > 1f && mainPicBox.TranslationX > PicBoxMinX && mainPicBox.TranslationX < PicBoxMaxX)
						MoveDistance.X += MoveDistance.X * (Config.PanSpeed - 1f);

					moveX = MoveDistance.X / DisplayUtils.ScreenDensity;

					if (DisplayUtils.DirectionLimitX((int)moveX, mainPicBox.TranslationX, PicBoxMinX, PicBoxMaxX))
						mainPicBox.TranslationX = Math.Clamp(mainPicBox.TranslationX + moveX, PicBoxMinX, PicBoxMaxX);

					//vScrollBar
					MoveDistance.Y += logicalPoint.Y - PrevPoint.Y;
					int sign = (int)(MoveDistance.Y / moveY);
					if (sign != 0 && DisplayUtils.DirectionLimitY(sign, vScrollBar.Value, vScrollBar.Minimum, vScrollBar.Maximum))
					{
						vScrollBar.Value -= sign;
						MoveDistance.Y %= moveY;
					}

					PrevPoint = logicalPoint;
					e.Handled = true;
					return;

				case SKTouchAction.Released:
					IsDragScrollBar = false;

					if (Config.LongPressSkip)
						LongPressTimer.Enabled = false;

					MouseReleased(e, logicalPoint);
					LeaveMouse();

					e.Handled = true;
					break;

				//case SKTouchAction.Cancelled:
				//	break;
				//case SKTouchAction.Exited:
				//	break;
				//case SKTouchAction.WheelChanged:
				//	break;
				default:
					return;
			}
		}

		private void MouseReleased(SKTouchEventArgs e, SKPoint logicalPoint)
		{
			if (IsMouseMove(logicalPoint))
				return;

			//if (!Config.UseMouse)
			//	return;
			//if (console == null || console.IsInProcess)
			//	return;
			if (console.IsInProcess)
				return;

			if (console.IsWaitingPrimitive)
			//			if (console.IsWaitingPrimitiveMouse)
			{
				if (IsLongPressed) console.MouseDown(MouseLocation, SKMouseButton.Right);
				console.MouseDown(MouseLocation, e.MouseButton);
				return;
			}

			bool isBacklog = vScrollBar.Value != vScrollBar.Maximum;
			string str = console.SelectedString;

			if (isBacklog)
				if (e.MouseButton == SKMouseButton.Left || e.MouseButton == SKMouseButton.Right)
				{
					vScrollBar.Value = vScrollBar.Maximum;
					RefreshStrings(true);
				}
			if (console.IsWaitingEnterKey && !console.IsError && str == null)
			{
				if (isBacklog)
					return;
				if (e.MouseButton == SKMouseButton.Left || e.MouseButton == SKMouseButton.Right)
				{
					if (e.MouseButton == SKMouseButton.Right || IsLongPressed)
						PressEnterKey(true, true);
					else
						PressEnterKey(false, true);
					return;
				}
			}
			//左が押されたなら選択。
			if (str != null && e.MouseButton == SKMouseButton.Left)
			{
				changeTextbyMouse = console.IsWaintingOnePhrase;
				richTextBox1.Text = str;
				//念のため
				if (console.IsWaintingOnePhrase)
					last_inputed = "";
				//右が押しっぱなしならスキップ追加。
				//if ((Control.MouseButtons & SKMouseButton.Right) == SKMouseButton.Right)
				//	PressEnterKey(true, true);
				//else
				PressEnterKey(false, true);
				return;
			}
		}

		private bool IsMouseMove(SKPoint endPoint)
		{
			return IsMouseMoveAction && SKPoint.Distance(StartPoint, endPoint) >= 10;
		}

		public bool IsDragScrollBar { get; private set; }

		private void vScrollBar_DragStarted(object sender, EventArgs e)
		{
			IsDragScrollBar = true;
		}

		private void vScrollBar_DragCompleted(object sender, EventArgs e)
		{
			IsDragScrollBar = false;
		}

		/// <summary>
		/// 指定画板的绘制行数位置
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void scrollBar_ValueChanged(object sender, ValueChangedEventArgs e)
		{
			if (!IsDragScrollBar)
				return;

			//if (console == null)
			//	return;

			int newValue = (int)e.NewValue;

			if (newValue == vScrollBar.Minimum || newValue == vScrollBar.Maximum || (int)e.OldValue != newValue)
				RefreshStrings((vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum));
		}

		private void LeaveMouse()
		{
			Task.Run(() =>
			{
				console.LeaveMouse();
			});
		}

		private void RefreshStrings(bool force_Paint)
		{
			Task.Run(() =>
			{
				console.RefreshStrings(force_Paint);
			});
		}

		public void Refresh()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				mainPicBox.InvalidateSurface();
			});
		}

		/// <summary>
		/// 点击输入法上的确定按钮即可提交输入
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void richTextBox1_Completed(object sender, EventArgs e)
		{
			if (console.IsInProcess)
				return;

			PressEnterKey(false, false);
		}

		private void InvisibleToolButton(TintImageButton button)
		{
			if (InvisibleToolButtonList == null)
				return;
			if (!(button.IsToggled ?? false))
				return;

			foreach (var view in InvisibleToolButtonList)
			{
				if (view != button)
					view.IsToggled = false;
			}
		}

		private void lock_rotation_button_Toggled(object sender, ToggledEventArgs e)
		{
			if (e.Value)
				GameUtils.PlatformService.LockScreenOrientation();
			else
				GameUtils.PlatformService.UnlockScreenOrientation();

			SetButtonOpacity(lock_rotation_button, e.Value);
		}

		private void edit_button_Toggled(object sender, ToggledEventArgs e)
		{
			entryGroup.IsVisible = e.Value;
			SetButtonOpacity(edit_button, e.Value);
		}

		private void scroll_vertical_button_Toggled(object sender, ToggledEventArgs e)
		{
			InvisibleToolButton((TintImageButton)sender);

			ScrollBarLayout.IsVisible = e.Value;
			SetButtonOpacity(scroll_vertical_button, e.Value);
		}

		private void gallery_view_button_Toggled(object sender, ToggledEventArgs e)
		{
			InvisibleToolButton((TintImageButton)sender);

			quickButtonScrollView.IsVisible = e.Value;
			SetButtonOpacity(gallery_view_button, e.Value);

			if (quickButtonScrollView.IsVisible)
				console?.RefreshQuickButtonAsync();
			else
				console?.ClearQuickButtonAsync();
		}

		private void virtual_controller_button_Toggled(object sender, ToggledEventArgs e)
		{
			virtualControllerLayout.IsVisible = e.Value;
			SetButtonOpacity(virtual_controller_button, e.Value);
			RefreshVirtualControllerState();
		}

		private void zoom_out_button_Clicked(object sender, EventArgs e)
		{
			AdjustRuntimeDisplayScale(-0.1f);
		}

		private void zoom_in_button_Clicked(object sender, EventArgs e)
		{
			AdjustRuntimeDisplayScale(0.1f);
		}

		private void ButtonVisibleGroup_Clicked(object sender, EventArgs e)
		{
			var visible = !edit_button.IsVisible;

			lock_rotation_button.IsVisible = visible;
			edit_button.IsVisible = visible;
			scroll_vertical_button.IsVisible = visible;
			gallery_view_button.IsVisible = visible;
			virtual_controller_button.IsVisible = visible;
			zoom_out_button.IsVisible = visible;
			zoom_in_button.IsVisible = visible;

			SetButtonOpacity(menu_show_button, visible);
		}

		public void quickButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			string inputs = ((View)sender).BindingContext as string;
			if (inputs == null)
				return;

			ScrollBacklogToBottom();
			changeTextbyMouse = console.IsWaintingOnePhrase;
			richTextBox1.Text = inputs;
			if (console.IsWaintingOnePhrase)
				last_inputed = "";
			PressEnterKey(false, true);
		}

		private void virtualLeftButton_Clicked(object sender, EventArgs e)
		{
			NavigateVirtualSelection(VirtualSelectionDirection.Left);
		}

		private void virtualUpButton_Clicked(object sender, EventArgs e)
		{
			NavigateVirtualSelection(VirtualSelectionDirection.Up);
		}

		private void virtualRightButton_Clicked(object sender, EventArgs e)
		{
			NavigateVirtualSelection(VirtualSelectionDirection.Right);
		}

		private void virtualDownButton_Clicked(object sender, EventArgs e)
		{
			NavigateVirtualSelection(VirtualSelectionDirection.Down);
		}

		private void virtualConfirmButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			ExecuteCurrentSelectedButtonOnly();
		}

		private void virtualReturnButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			bool wasBacklog = ScrollBacklogToBottom();
			if (wasBacklog)
				return;

			if (TryActivateKeywordButton("返回", "取消", "归返", "结束", "完毕", "否"))
				return;

			if (console.IsWaitingEnterKey && !console.IsError)
				PressEnterKey(true, true);
		}

		private void virtualPageEnterButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateEnterAction();
		}

		private void virtualPageConfirmButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("确认", "确定", "调合", "选定", "是");
		}

		private void virtualPageBackButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("返回", "取消", "归返", "结束", "完毕", "否");
		}

		private void virtualAvgLogButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("日志");
		}

		private void virtualAvgAutoButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("自动");
		}

		private void virtualAvgSkipButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("跳过");
		}

		private void virtualAvgSettingButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("设置");
		}

		private void virtualKeypadSlashButton_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("/", "[/]", "／");
		}

		private void virtualKeypadStarButton_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("*", "[*]", "＊");
		}

		private void virtualKeypadMinusButton_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("-", "[-]", "－");
		}

		private void virtualKeypadPlusButton_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("+", "[+]", "＋");
		}

		private void virtualKeypad7Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("7", "[7]");
		}

		private void virtualKeypad8Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("8", "[8]");
		}

		private void virtualKeypad9Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("9", "[9]");
		}

		private void virtualKeypad4Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("4", "[4]");
		}

		private void virtualKeypad5Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("5", "[5]");
		}

		private void virtualKeypad6Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("6", "[6]");
		}

		private void virtualKeypad1Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("1", "[1]");
		}

		private void virtualKeypad2Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("2", "[2]");
		}

		private void virtualKeypad3Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("3", "[3]");
		}

		private void virtualKeypad0Button_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton("0", "[0]");
		}

		private void virtualKeypadDotButton_Clicked(object sender, EventArgs e)
		{
			ActivateKeypadButton(".", "[.]", "．");
		}

		private void virtualKeypadEnterActionButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateEnterAction();
		}

		private void virtualKeypadAcceptActionButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("确认", "确定", "调合", "选定", "是");
		}

		private void virtualKeypadConfirmButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			ExecuteCurrentSelectedButtonOnly();
		}

		private void virtualKeypadBackButton_Clicked(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			TryActivateKeywordButton("返回", "取消", "归返", "结束", "完毕", "否");
		}

		private void NavigateVirtualSelection(VirtualSelectionDirection direction)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			console.NavigateSelection(direction);
		}

		public void RefreshVirtualControllerState()
		{
			if (virtualControllerLayout == null)
				return;

			bool controlsEnabled = virtualControllerLayout.IsVisible;
			if (!controlsEnabled || console == null)
			{
				virtualPageEnterButton.IsVisible = false;
				virtualPageConfirmButton.IsVisible = false;
				virtualPageBackButton.IsVisible = false;
				virtualKeypadEnterActionButton.IsVisible = false;
				virtualKeypadAcceptActionButton.IsVisible = false;
				virtualKeypadGroup.IsVisible = false;
				virtualAvgButtonGroup.IsVisible = false;
				return;
			}

			bool hasEnterAction = HasEnterAction();
			virtualPageEnterButton.IsVisible = hasEnterAction;

			string confirmLabel = console.GetVisibleButtonKeywordLabel("确认", "确定", "调合", "选定", "是");
			virtualPageConfirmButton.IsVisible = !string.IsNullOrEmpty(confirmLabel);
			virtualPageConfirmButton.Text = string.IsNullOrEmpty(confirmLabel) ? "确定" : confirmLabel;

			string backLabel = console.GetVisibleButtonKeywordLabel("返回", "取消", "归返", "结束", "完毕", "否");
			virtualPageBackButton.IsVisible = !string.IsNullOrEmpty(backLabel);
			virtualPageBackButton.Text = string.IsNullOrEmpty(backLabel) ? "返回" : backLabel;

			string logLabel = console.GetVisibleButtonKeywordLabel("日志");
			string autoLabel = console.GetVisibleButtonKeywordLabel("自动");
			string skipLabel = console.GetVisibleButtonKeywordLabel("跳过");
			string settingLabel = console.GetVisibleButtonKeywordLabel("设置");
			bool isKeypadPanel = console.HasRecentDisplayLineKeyword(20, "[2]", "［2］")
				&& console.HasRecentDisplayLineKeyword(20, "[4]", "［4］")
				&& console.HasRecentDisplayLineKeyword(20, "[6]", "［6］")
				&& console.HasRecentDisplayLineKeyword(20, "[8]", "［8］");
			bool isAvgPanel = !string.IsNullOrEmpty(logLabel)
				&& !string.IsNullOrEmpty(autoLabel)
				&& !string.IsNullOrEmpty(skipLabel)
				&& !string.IsNullOrEmpty(settingLabel);
			virtualKeypadGroup.IsVisible = isKeypadPanel;
			virtualAvgButtonGroup.IsVisible = !isKeypadPanel && isAvgPanel;
			virtualKeypadEnterActionButton.IsVisible = isKeypadPanel && hasEnterAction;
			virtualKeypadAcceptActionButton.IsVisible = isKeypadPanel && !string.IsNullOrEmpty(confirmLabel);
			virtualPageActionGroup.IsVisible = !isKeypadPanel && (virtualPageEnterButton.IsVisible || virtualPageConfirmButton.IsVisible || virtualPageBackButton.IsVisible);
		}

		private bool ScrollBacklogToBottom()
		{
			bool isBacklog = vScrollBar.Value != vScrollBar.Maximum;
			if (isBacklog)
			{
				vScrollBar.Value = vScrollBar.Maximum;
				RefreshStrings(true);
			}
			return isBacklog;
		}

		private bool TryActivateKeywordButton(params string[] keywords)
		{
			if (!console.TrySelectVisibleButtonByKeywords(keywords))
				return false;

			ExecuteVirtualSelectedButton(false);
			return true;
		}

		private bool TryActivateEnterAction()
		{
			if (TryActivateExactButton("Enter", "[Enter]"))
				return true;
			if (console.IsWaitingEnterKey && !console.IsError)
			{
				PressEnterKey(false, true);
				return true;
			}
			return false;
		}

		private bool TryActivateExactButton(params string[] labels)
		{
			if (!console.TrySelectVisibleButtonByLabels(labels))
				return false;

			ExecuteVirtualSelectedButton(false);
			return true;
		}

		private void ActivateKeypadButton(params string[] labels)
		{
			if (console == null || console.IsInProcess)
				return;

			ScrollBacklogToBottom();
			if (TryActivateExactButton(labels))
				return;
			if (TryActivateKeywordButton(labels))
				return;

			string fallbackInput = GetKeypadFallbackInput(labels);
			if (!string.IsNullOrEmpty(fallbackInput))
				PressEnterKey(fallbackInput);
		}

		private bool HasEnterAction()
		{
			return console.HasVisibleButtonLabel("Enter", "[Enter]")
				|| console.HasRecentDisplayLineKeyword(20, "[Enter]", "Enter");
		}

		private void ExecuteCurrentSelectedButtonOnly()
		{
			if (console.IsWaitingPrimitive)
			{
				if (console.SelectingButton != null)
					console.ClickSelectedButton(SKMouseButton.Left);
				return;
			}

			string str = console.SelectedString;
			if (str == null)
				return;

			changeTextbyMouse = console.IsWaintingOnePhrase;
			richTextBox1.Text = str;
			if (console.IsWaintingOnePhrase)
				last_inputed = "";
			PressEnterKey(false, true);
		}

		private static string GetKeypadFallbackInput(params string[] labels)
		{
			if (labels == null)
				return null;

			for (int i = 0; i < labels.Length; i++)
			{
				string label = labels[i];
				if (string.IsNullOrWhiteSpace(label))
					continue;
				if (label.StartsWith("[") && label.EndsWith("]"))
					continue;
				if (label.StartsWith("［") && label.EndsWith("］"))
					continue;
				return label;
			}

			return null;
		}

		private void ExecuteVirtualSelectedButton(bool mesSkip)
		{
			if (console.IsWaitingPrimitive)
			{
				console.ClickSelectedButton(SKMouseButton.Left);
				return;
			}

			string str = console.SelectedString;
			if (str != null)
			{
				changeTextbyMouse = console.IsWaintingOnePhrase;
				richTextBox1.Text = str;
				if (console.IsWaintingOnePhrase)
					last_inputed = "";
				PressEnterKey(mesSkip, true);
				return;
			}

			if (console.IsWaitingEnterKey && !console.IsError)
				PressEnterKey(mesSkip, true);
		}

		private void PressEnterKey(string inputs)
		{
			ScrollBacklogToBottom();

			if (inputs == null)
			{
				PressEnterKey(true, false);
			}
			else
			{
				richTextBox1.Text = inputs;
				PressEnterKey(false, false);
			}
		}

		private void publish_button_Clicked(object sender, EventArgs e)
		{
			if (console.IsInProcess)
				return;

			PressEnterKey(false, false);
		}

		private static void SetButtonOpacity(View button, bool visible)
		{
			button.Opacity = visible ? 1 : 0.5d;
		}

		public void MainMenu_Reboot()
		{
			if (IsInitializing(true))
				return;

			MessageBox.ShowOnMainThread(StringsText.RebootConfirm, StringsText.Reboot, result =>
			{
				if (result)
					this.Reboot();
			}, MessageBoxButtons.OKCancel);
		}

		public void MainMenu_GotoTitle()
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show("スクリプト動作中には使用できません");
				return;
			}
			if (console.notToTitle)
			{
				if (console.byError)
					MessageBox.Show("コード解析でエラーが発見されたため、タイトルへは飛べません");
				else
					MessageBox.Show("解析モードのためタイトルへは飛べません");
				return;
			}

			MessageBox.ShowOnMainThread(StringsText.GotoTitleConfirm, StringsText.GotoTitle, result =>
			{
				if (result)
					this.GotoTitle();
			}, MessageBoxButtons.OKCancel);
		}
	}
}
