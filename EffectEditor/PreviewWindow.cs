using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using Masa.ParticleEngine;
using Microsoft.Windows.Controls;


namespace EffectEditor
{
	public class PreviewWindow : Game
	{
		GraphicsDeviceManager graphicDeviceManager;
		Effect effect;
		internal EffectProject EffectProject { get; private set; }
		public MainWindow MainWindow { get; set; }

		public IEnumerable<PMIData> PMIDatas
		{
			get { return EffectProject.PMIDict.Values; }
		}

		public PreviewWindow(MainWindow mw)
		{
			MainWindow = mw;
			graphicDeviceManager = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 400,
				PreferredBackBufferHeight = 480,
			};
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();
			effect = new Effect(GraphicsDevice, new EffectCompiler().CompileFromFile("particle.fx").EffectData);
			EffectProject = new EffectProject(GraphicsDevice, s => LoadTexture(GetTextureFileName(s)));
			EffectProject.SetEffect(effect);
		}

		Texture2D LoadTexture(string s)
		{
			Stream file = null;
			try
			{
				file = File.OpenRead(s);
				//var tex = Texture2D.FromStream(device, file);
				var tex = Texture2D.Load(GraphicsDevice, file);
				var buffer = new SharpDX.Color[tex.Width * tex.Height];
				tex.GetData(buffer);
				//tex.SetData(buffer.Select(c => c * (c.A / 255f)).ToArray());
				return Texture2D.New(GraphicsDevice, tex.Width, tex.Height, tex.Format, buffer.Select(x => SharpDX.Color.Premultiply(x).ToRgba()).ToArray());
				//return tex;
			}
			catch (Exception)
			{
				return CreateDummyTexture();
			}
			finally
			{
				if (file != null)
				{
					file.Dispose();
				}
			}
		}

		private Texture2D CreateDummyTexture()
		{
			var tex = Texture2D.New<SharpDX.Color>(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8.UInt, new[] { SharpDX.Color.White });
			return tex;
		}


		public string GetAbsTexturePath()
		{
			return Masa.Lib.Utility.ConvertAbsolutePath(Path.GetDirectoryName(MainWindow.ProjectFileName), EffectProject.TexturePath);
		}
		
		

		public string GetTextureFileName(string s)
		{
			return Path.Combine(GetAbsTexturePath(), s);
		}

		protected override void Update(GameTime gameTime)
		{
			if (!IsActive) return;
			EffectProject.Update();
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			if (!IsActive) return;
			GraphicsDevice.Clear(Color.Black);
			EffectProject.Draw();
			base.Draw(gameTime);
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			effect.Dispose();
			base.Dispose(disposeManagedResources);
		}

		
		public static void ShowExceptionBox(Exception e)
		{
			//var box = new Microsoft.SqlServer.MessageBox.ExceptionMessageBox(e);
			//box.Show(new Handler());
			MessageBox.Show(e.ToString());
		}

		public void PlayScript(string lines)
		{
			string path = Path.GetTempFileName();
			File.WriteAllText(path, lines);
			try
			{
				EffectProject.PlayEffect(path);
			}
			catch (Exception e)
			{
				ShowExceptionBox(e);
			}
		}

		public void StopEffect()
		{
			EffectProject.StopEffect();
		}

		
		public Action<string> onTextureLoaded;

		public PMIData AddParticleItem(string name, string texture, ushort mass, float r, float g, float b, float a, ParticleBlendMode blend, int layer)
		{
			return EffectProject.AddParticleManager(name, texture, mass, r, g, b, a, blend, layer);
		}

		public PMIData AddDefaultParticleItem(string textureName)
		{
			float c = 255f / 256f;
			c = 1;
			return AddParticleItem(Path.GetFileNameWithoutExtension(textureName), textureName, 256, c, c, c, c, ParticleBlendMode.Add, 0);
		}

		public void UpdateParticleItem(string baseName, string newName, PMIData item)
		{
			EffectProject.UpdateParticleManager(baseName, newName, item);
		}

		public void ResetParticle()
		{
			if (EffectProject != null)
				EffectProject.Reset();
		}

		public void RemovePartilceItem(string name)
		{
			EffectProject.RemoveParticleManager(name);
		}

		public string TexturePath
		{
			get { return EffectProject.TexturePath; }
			set { EffectProject.TexturePath = value; }
		}

		public string ScriptPath
		{
			get { return EffectProject.ScriptPath; }
			set { EffectProject.ScriptPath = value; }
		}

		public void SaveProject(string name)
		{
			EffectProject.SaveToFile(name);
		}

		public void OpenProject(string name)
		{
			EffectProject.Load(name);
		}

		public void InitProject()
		{
			LoadContent(); 
		}
	}
	
}
