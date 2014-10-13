using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace EffectEditor
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public static IntPtr WindowHandle;
		StringWriter ErrorLogger;
		public PreviewWindow XNAControl;

		ProjectControl projectControl;
		ScriptControl scriptControl;
		public string ProjectFileName { get { return projectControl.ProjectFileName; } }
		public string ProjectFileDirectory { get { return Path.GetDirectoryName(ProjectFileName); } }

		//[System.Runtime.InteropServices.DllImport("User32.dll")]
		//static extern IntPtr SendMessage(IntPtr hWnnd, int msg, int wParam, int[] lParam);

		public MainWindow()
		{
			InitializeComponent();
			XNAControl = new PreviewWindow(this);
			XNAControl.Run(previewWindow);
			//var src = System.Windows.Interop.HwndSource.FromVisual(scriptCode);
			//SendMessage(((System.Windows.Interop.HwndSource)src).Handle, 0x00cb, 1, new int[] { 16 });
			scriptCode.CommandBindings.AddRange(new[]
				{
					new CommandBinding(ApplicationCommands.New, (o,e) => scriptControl.NewFile()),
					new CommandBinding(ApplicationCommands.Save, (o,e) => scriptControl.Save()),
					new CommandBinding(ApplicationCommands.SaveAs, (o,e) => scriptControl.SaveAs()),
					new CommandBinding(ApplicationCommands.Open, (o,e) => scriptControl.OpenFile()),
				});
			this.CommandBindings.AddRange(new[]
				{
					new CommandBinding(ApplicationCommands.Close, (o, e) => this.Close()),
					new CommandBinding(ApplicationCommands.New, (o, e) => projectControl.NewProject()),
					new CommandBinding(ApplicationCommands.Save, (o, e) => projectControl.SaveProject()),
					new CommandBinding(ApplicationCommands.SaveAs, (o, e) => projectControl.SaveAsProject()),
					new CommandBinding(ApplicationCommands.Open, (o, e) => projectControl.OpenProjectClick()),
				});
			//XNAControl.onTextureLoaded += (s) => textureListBox.Items.Add(System.IO.Path.GetFileNameWithoutExtension(s));
			ErrorLogger = new StringWriter();
			var error = new System.Diagnostics.TextWriterTraceListener(ErrorLogger);
			//var op = (System.Diagnostics.DefaultTraceListener)System.Diagnostics.Debug.Listeners[0];
			//op.

			projectControl = new ProjectControl(this);
			scriptControl = new ScriptControl(this);

			blendModeSelector.ItemsSource = Enum.GetValues(typeof(Masa.ParticleEngine.ParticleBlendMode));
			System.Diagnostics.Debug.Listeners.Add(error);
			LoadLatestProjects();
			//if (last != null)
			//{
			//	projectFileManager.Open(last);
			//}
			textureColor.SelectedColorChanged += textureColor_SelectedColorChanged;
		}

		void textureColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
		{
			(texturePreviewImage.Effect as ShaderEffectLibrary.MonochromeEffect).FilterColor = e.NewValue;
		}


		

		void LoadLatestProjects()
		{
			var items = projectControl.LoadLatestProjects();
			latestProjects.ItemsSource = items
				.Select(i =>
				{
					var item = new MenuItem();
					item.Click += (obj, e) => projectControl.OpenProject((string)item.Header);
					//item.Click += (obj, e) => OpenProject((string)item.Header);
					item.Header = i;
					return item;
				});
		}
		
		public string Code { get; set; }

		

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			WindowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
			//new System.Windows.Interop.HwndSource(
			itemsList.ItemsSource = XNAControl.PMIDatas;
			XNAControl.EffectProject.OnScriptPathChanged = UpdateScriptList;

			if (latestProjects.Items.Count > 0)
			{
				projectControl.OpenProject(latestProjects.Items.OfType<MenuItem>().First().Header as string);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

		}



		private void playButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateCurrentParticleItemValues();
			XNAControl.PlayScript(scriptCode.Text);
		}

		private void playExitButton_Click(object sender, RoutedEventArgs e)
		{
			XNAControl.StopEffect();
		}

		void RefreshButtonClick(object sender, RoutedEventArgs e)
		{
			XNAControl.ResetParticle();
		}

		#region ScriptFile

		private void scriptCode_TextChanged(object sender, TextChangedEventArgs e)
		{
			SetStatus("Ready...");
			//scriptControl.Changed();
			//changed = true;
		}

		private void scriptCode_Drop(object sender, DragEventArgs e)
		{
			string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files != null)
			{
				scriptControl.OpenFile(files[0]);
				//LoadScript(files[0]);
			}
		}

		private void scriptCode_PreviewDragOver(object sender, DragEventArgs e)
		{
			e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
		}

		void UpdateScriptList(string dir)
		{
			scriptFileList.ItemsSource = Directory.EnumerateFiles(Masa.Lib.Utility.ConvertAbsolutePath(ProjectFileDirectory, dir), "*.mss")
				.Select(i => Path.GetFileNameWithoutExtension(i));
			
		}

		void ScriptFileItemDoubleClicked(object sender, MouseEventArgs e)
		{
			var item = sender as ListBoxItem;
			if (item == null)
			{
				return;
			}
			var file = item.Content + ".mss";
			scriptControl.OpenFile(Path.Combine(Masa.Lib.Utility.ConvertAbsolutePath(ProjectFileDirectory, XNAControl.ScriptPath), file));
		}

		#endregion

		#region ScriptFormat

		private void scriptCode_KeyDown(object sender, KeyEventArgs e)
		{

		}


		private void scriptCode_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//if (e.Key == Key.Enter)
			//{

			//}
			//if (e.Key == Key.Tab)
			//{
			//    e.Handled = true;
			//    //scriptCode.Text += Tab;
			//    //scriptCode.CaretIndex += 4;
			//    //scriptCode.
			//    for (int i = 0; i < 4; i++)
			//    {
			//        var arg = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Space)
			//            {
			//                Source = sender,
			//                RoutedEvent = e.RoutedEvent,
			//            };

			//        //scriptCode.RaiseEvent(arg);
			//    }
			//}
			////Keyboard.PrimaryDevice.

		}

		#endregion

		

		#region ProjectFile

	

		void setTexturePathClick(object sender, RoutedEventArgs e)
		{
			projectControl.SetTexturePath();
		}

		void removeButtonClick(object sender, RoutedEventArgs e)
		{
			projectControl.RemoveParticleItem(itemsList.SelectedItem as Masa.ParticleEngine.PMIData);
		}

		#endregion

		#region Particle Item Setting

		private void OpenTextureClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog()
			{
				DefaultExt = ".png",
				CheckPathExists = true,
				InitialDirectory = XNAControl.GetAbsTexturePath()

			};
			if (dlg.ShowDialog() == true)
			{
				texturePathText.Text = System.IO.Path.GetFileName(dlg.FileName);
				SetStatus("Texture Path : " + dlg.FileName);
				projectControl.SetTextureList();
			}
		}

		private void addItemButtonClick(object sender, RoutedEventArgs e)
		{
			var tex = textureList.SelectedItem as string;
			if (tex == null)
			{
				return;
			}
			var item = XNAControl.AddDefaultParticleItem(tex + ".png");
			projectControl.Changed();
			UpdateParticleItemList(); 
			itemsList.SelectedIndex = itemsList.Items.Count - 1;
			
			//UpdateParticleItemPropertyDisplay();
			//var item = AddItem();
			//if (item != null)
			//{
			//	//itemsList.Items.Add(item);
			//	UpdatePMIDatas();
			//}
		}

		//Masa.ParticleEngine.PMIData AddItem()
		//{
		//	string name = itemNameText.Text;
		//	if (String.IsNullOrWhiteSpace(name)) return null;
		//	string texture = texturePathText.Text;
		//	if (String.IsNullOrWhiteSpace(texture)) return null;
		//	projectControl.Changed();
		//	return XNAControl.AddParticleItem(name, texture, (ushort)particleMassNumber.Value,
		//		textureColor.R / 256f, textureColor.G / 256f,
		//		textureColor.B / 256f, textureColor.A / 256f,
		//		(Masa.ParticleEngine.ParticleBlendMode)blendModeSelector.SelectedItem,
		//		layerNumber.Value.HasValue ? layerNumber.Value.Value : 0);
		//}

		/// <summary>
		/// パーティクル辞書に現在の画面の情報を反映する
		/// </summary>
		/// <param name="data"></param>
		void UpdateParticleItemValues(Masa.ParticleEngine.PMIData data)
		{
			data.Mass = (ushort)particleMassNumber.Value;
			data.TextureName = texturePathText.Text;
			data.Color = Util.FromWColor(textureColor.SelectedColor).ToVector4();
			data.Layer = layerNumber.Value.HasValue ? layerNumber.Value.Value : 0;
			data.Blend = (Masa.ParticleEngine.ParticleBlendMode)blendModeSelector.SelectedItem;
			XNAControl.UpdateParticleItem(data.Name, itemNameText.Text, data);
			//projectControl.Changed();
		}

		public void UpdateCurrentParticleItemValues()
		{
			var item = this.itemsList.SelectedItem as Masa.ParticleEngine.PMIData;
			if (item != null)
			{
				UpdateParticleItemValues(item);
			}
		}


		public void UpdateParticleItemList()
		{
			//int last = itemsList.SelectedIndex;
			itemsList.ItemsSource = XNAControl.PMIDatas;
			itemsList.Items.Refresh();
			UpdateParticleItemPropertyDisplay();
			//itemsList.SelectedIndex = last;
			//UpdatePMIPropery();
			//itemsList.Items.Refresh();
		}

		private void particleListItemClick(object sender, MouseButtonEventArgs e)
		{
			var item = (sender as ListBoxItem);
			if (item != null && item.Content == itemsList.SelectedItem)
			{
				UpdateParticleItemValues(item.Content as Masa.ParticleEngine.PMIData);
				UpdateParticleItemList();
			}
			
		}
		

		private void itemsList_SourceUpdated(object sender, DataTransferEventArgs e)
		{
			UpdateParticleItemPropertyDisplay();
		}

		private void itemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.RemovedItems.OfType<Masa.ParticleEngine.PMIData>().FirstOrDefault();
			if (item != null)
			{
				UpdateParticleItemValues(item);
				UpdateParticleItemList();
			}
			//var item = AddItem();
			//if (item != null)
			//{
			//	UpdatePMIDatas();
			//}
			var n = e.AddedItems.OfType<Masa.ParticleEngine.PMIData>().FirstOrDefault();
			if (n != null)
			{
				UpdateParticleItemPropertyDisplay(n);
			}
		}

		/// <summary>
		/// 画像を読み込む、正しいファイルでなければnull
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		BitmapImage LoadBitmapImage(string fileName)
		{
			try
			{
				return new BitmapImage(new Uri(XNAControl.GetTextureFileName(fileName)));
			}
			catch
			{
				return null;
			}
		}

		void UpdateParticleItemPropertyDisplay(Masa.ParticleEngine.PMIData pmi)
		{
			itemNameText.Text = pmi.Name;
			texturePathText.Text = pmi.TextureName;
			particleMassNumber.Value = pmi.Mass;
			blendModeSelector.SelectedItem = pmi.Blend;
			textureColor.SelectedColor = Util.FromXColor(pmi.Color);
			layerNumber.Value = pmi.Layer;
			try
			{
				var image = LoadBitmapImage(pmi.TextureName);
				(texturePreviewImage.Effect as global::ShaderEffectLibrary.MonochromeEffect).FilterColor = Util.FromXColor(pmi.Color);
				texturePreviewImage.Source = (image != null ? image.Clone() : null);
				image = null;
				GC.Collect();
			}
			catch 
			{
				texturePreviewImage.Source = null;
			}
			projectControl.SetTextureList();
		}

		void ClearParticleItemPropertyDisplay()
		{
			itemNameText.Text = "";
			texturePathText.Text = "";
			textureColor.SelectedColor = Colors.White;
			layerNumber.Value = 0;
			blendModeSelector.SelectedItem = Masa.ParticleEngine.ParticleBlendMode.Add;
			texturePreviewImage.Source = null;
			projectControl.SetTextureList();
		}

		void UpdateParticleItemPropertyDisplay()
		{
			var pmi = itemsList.SelectedItem as Masa.ParticleEngine.PMIData;
			if (pmi == null)
			{
				ClearParticleItemPropertyDisplay();
				return;
			}
			UpdateParticleItemPropertyDisplay(pmi);
		}


		#endregion

		


		public void SetStatus(string txt)
		{
			statusLabel.Content = txt;
		}

		private void window_Activated(object sender, EventArgs e)
		{
			
		}

		private void window_Deactivated(object sender, EventArgs e)
		{
			
		}

		private void is2DCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			EffectProject.Is2D = true;
			ResetEffectProject();
		}

		private void is2DCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			EffectProject.Is2D = false;
			ResetEffectProject();
		}

		void ResetEffectProject()
		{
			if (XNAControl != null && XNAControl.EffectProject != null)
			{
				XNAControl.EffectProject.Reset();
			}
		}

		private void controlSizeChanged(object sender, RoutedEventArgs e)
		{
			int width = controlWidth.Value.GetValueOrDefault(controlWidth.DefaultValue.Value);
			int height = controlHeight.Value.GetValueOrDefault(controlHeight.Value.Value);
			//windowsFormsHost1.Width = width;
			//windowsFormsHost1.Height = height;
			//XNAControl.ChangeSize(width, height);
			UpdateLayout();

			//XNAControl.Width = controlWidth.Value.GetValueOrDefault(controlWidth.DefaultValue.Value);
			//XNAControl.Height = controlHeight.Value.GetValueOrDefault(controlHeight.Value.Value);
			//XNAControl.ResetParticle();

		}

		private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			UpdateCurrentParticleItemValues();
			if (scriptControl.TryClose() && projectControl.TryClose())
			{
				return;
			}
			else
			{
				e.Cancel = true;
			}
		}

		private void textureList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var item = textureList.SelectedItem as string;
			if (string.IsNullOrWhiteSpace(item))
			{
				listTexturePreview.Source = null;
				return;
			}
			var image = LoadBitmapImage(item + ".png");
			listTexturePreview.Source = (image != null ? image.Clone() : null);
			image = null;
			GC.Collect();
		}


	}


}
