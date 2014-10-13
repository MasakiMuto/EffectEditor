using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Masa.ParticleEngine;
using SharpDX.Toolkit.Graphics;

namespace EffectEditor
{
	public partial class XNAControl : Control
	{
		GraphicsDevice device;
		Effect effect;
		internal EffectProject EffectProject { get; private set; }
		public MainWindow Window { get; set; }

		public IEnumerable<PMIData> PMIDatas
		{
			get { return EffectProject.PMIDict.Values; }
		}

		public XNAControl()
		{
			InitializeComponent();
			Disposed += new EventHandler(XNAControl_Disposed);
		}

		void XNAControl_Disposed(object sender, EventArgs e)
		{
			DisposeDevice();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);
		}


		public void Draw()
		{
			if (device == null) return;
			try
			{
				EffectProject.Update();
			}
			catch (Exception e)
			{
				ShowExceptionBox(e);
				StopEffect();
				return;
			}

			//device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Util.FromWColor(Window.backgroundColor.SelectedColor), 1, 0);
			BeginDraw();
			EffectProject.Draw();
			try
			{
				device.Present();
			}
			catch
			{
				Reset();
				GC.Collect();
			}
		}

		void BeginDraw()
		{
			device.ClearState();
			device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Util.FromWColor(Window.backgroundColor.SelectedColor), 1, 0);
			device.SetRenderTargets(device.DepthStencilBuffer, device.BackBuffer);
			device.SetViewport(new SharpDX.ViewportF(0, 0, device.BackBuffer.Width, device.BackBuffer.Height));
		}

		protected override void OnCreateControl()
		{
			if (DesignMode == false)
			{
				InitDevice();
				LoadContent();
			}
			base.OnCreateControl();
		}

		void DisposeDevice()
		{
			if (device != null)
			{
				device.Dispose();
				device = null;
			}
		}

		void InitDevice()
		{
			var format = PixelFormat.R8G8B8A8.UNorm;
			var pp = new PresentationParameters()
			{
				BackBufferWidth = (int)this.Width,
				BackBufferHeight = (int)this.Height,
				DepthStencilFormat = DepthFormat.Depth24Stencil8,
				DeviceWindowHandle = this.Handle,
				//RenderTargetUsage = SharpDX.DXGI.Usage.BackBuffer,
				IsFullScreen = false,
				BackBufferFormat = format,
				//BackBufferFormat = SurfaceFormat.Color,
				PresentationInterval = PresentInterval.Immediate,
			};

			try
			{
				device = GraphicsDevice.New(GraphicsAdapter.Default);
				var target = RenderTarget2D.New(device, pp.BackBufferWidth, pp.BackBufferHeight, pp.BackBufferFormat);
				//device.Presenter = new RenderTargetGraphicsPresenter(device, target, DepthFormat.Depth16);

				device.Presenter = new SwapChainGraphicsPresenter(device, pp);
				device.SetRenderTargets(device.DepthStencilBuffer, device.BackBuffer);
				//device.SetRenderTargets(DepthStencilBuffer.New(device, pp.BackBufferWidth, pp.BackBufferHeight, pp.DepthStencilFormat),
				//	target);
				device.SetViewport(new SharpDX.ViewportF(0, 0, device.BackBuffer.Width, device.BackBuffer.Height));
				device.ClearState();
				//device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, pp);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			//genPosition = new Vector2(200, 240);
		}

		public void ChangeSize(int width, int height)
		{
			Width = width;
			Height = height;
			Reset();
		}

		void Reset()
		{
			var dict = EffectProject.PMIDict;
			var texturePath = EffectProject.TexturePath;
			var scriptPath = this.ScriptPath;

			InitDevice();
			LoadContent();
			EffectProject.PMIDict = dict;

			TexturePath = texturePath;
			EffectProject.Reset();
			EffectProject.ScriptPath = scriptPath;
		}


	}

	class Handler : System.Windows.Forms.IWin32Window
	{
		public IntPtr Handle
		{
			get { return MainWindow.WindowHandle; }
		}
	}
}
