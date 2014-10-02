using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Masa.ParticleEngine;
using Masa.ScriptEngine;
using SharpDX.Toolkit.Graphics;
using SharpDX;
using TextureFunc = System.Func<string, SharpDX.Toolkit.Graphics.Texture2D>;
using Masa.Lib;

namespace EffectEditor
{
	internal class EffectProject
	{
		ScriptManager script;
		ScriptEffectManager particle;
		readonly GraphicsDevice device;
		Vector2 generatePosition;
		Effect effect;
		TextureFunc textureGetFunction;
		internal Dictionary<string, PMIData> PMIDict { get; set; }
		readonly Random rand;
		public static bool Is2D { get; set; }

		public Action<string> OnScriptPathChanged { get; set; }
		string scriptPath;
		/// <summary>
		/// 最後に使ったスクリプトファイルのあるディレクトリへの、プロジェクトファイルのパスを基準とした相対パス
		/// </summary>
		public string ScriptPath
		{
			get
			{
				return scriptPath;
			}
			set
			{
				scriptPath = value;
				if (OnScriptPathChanged != null)
				{
					OnScriptPathChanged(scriptPath);
				}
			}
		}

		/// <summary>
		/// テクスチャがあるディレクトリへのプロジェクトファイルのパスを基準とした相対パス
		/// </summary>
		public string TexturePath
		{
			get;
			set;
		}

		public EffectProject(GraphicsDevice gd, TextureFunc textureFunc)
		{
			generatePosition = new Vector2(gd.BackBuffer.Width, gd.BackBuffer.Height) * .5f;
			device = gd;
			//var content = new ContentManager(new GraphicService(device));
			Is2D = true;
			script = new ScriptManager();
			textureGetFunction = textureFunc;
			rand = new Random();
			PMIDict = new Dictionary<string, PMIData>();
			TexturePath = "";
			ScriptPath = "";
		}

		public void SetEffect(Effect ef)
		{
			effect = ef;
		}

		public static EffectProject LoadFromFile(string fileName, GraphicsDevice gd, TextureFunc textureFunc)
		{
			var prj = new EffectProject(gd, textureFunc);
			prj.Load(fileName);
			return prj;
		}

		public void Load(string fileName)
		{
			using (var str = File.OpenRead(fileName))
			{
				XDocument xml = XDocument.Load(str);
				TexturePath = xml.Root.Element("texture").Value;
				var script = xml.Root.Element("script");
				ScriptPath = script != null ? script.Value : "";
				var is2d = xml.Root.Element("is2d");
				Is2D = is2d != null ? is2d.BoolValue() : true;
				PMIDict = ParticleManagerInitializerManager.LoadPMIDatas(fileName).ToDictionary(i => i.Name);
			};
			MakeParticleManager();
		}

		public void PlayEffect(string fileName)
		{
			if (particle != null)
			{
				particle.LoadScript(fileName);
				particle.Set(script.PathToKey(fileName), generatePosition);
			}
		}

		public void StopEffect()
		{
			if (particle != null)
			{
				particle.ClearParticle();
			}
		}

		public PMIData AddParticleManager(string name, string texture, ushort num, float r, float g, float b, float a, ParticleBlendMode blend, int layer)
		{
			var data = new PMIData(texture, name, num, new Vector4(r, g, b, a), blend, layer);
			PMIDict[name] = data;
			MakeParticleManager();
			return data;
		}

		public void UpdateParticleManager(string baseName, string newName, PMIData data)
		{
			if (baseName != newName)
			{
				PMIDict.Remove(baseName);
				PMIDict[newName] = data;
				data.Name = newName;
			}
			MakeParticleManager();
		}

		public void RemoveParticleManager(string name)
		{
			if (PMIDict.ContainsKey(name))
			{
				PMIDict.Remove(name);
				MakeParticleManager();
			}
		}

		internal void Reset()
		{
			MakeParticleManager();
		}

		void MakeParticleManager()
		{
			ParticleManagerInitializer[] pmi = PMIDict.Values.Select(i => i.CreatePMI(textureGetFunction)).ToArray();
			if (Is2D)
			{
				particle = new ScriptEffectManager(
					script, device, effect, ParticleMode.TwoD,
					device.Viewport.Width, device.Viewport.Height, pmi, rand);
			}
			else
			{
				particle = new ScriptEffectManager(
					script, device, effect, ParticleMode.ThreeD,
					Matrix.PerspectiveFovRH(SharpDX.MathUtil.PiOverFour, device.Viewport.AspectRatio, 1, 100),
					//Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1, 100),
					new Vector2(device.Viewport.Width, device.Viewport.Height), pmi, rand);
				//particle = new ScriptEffectManager(script, device, effect, ParticleMode.ThreeD, 400, 480, PMIDict.Values.
				//Select(i => i.CreatePMI(textureGetFunction)).ToArray(),
				//rand);
			}
		}

		public void SaveToFile(string fileName)
		{
			XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("effect_project"));
			var root = xml.Root;

			root.Add(new XElement("texture", TexturePath));
			root.Add(new XElement("script", ScriptPath));
			root.Add(new XElement("is2d", Is2D));
			root.Add(PMIDict.Values.Select
				(i => new XElement
					(
						 ParticleManagerInitializerManager.PMITag,
						 new XElement("texture", i.TextureName),
						 new XElement("name", i.Name),
						 new XElement("mass", i.Mass),
						 new XElement("blend", Enum.GetName(typeof(ParticleBlendMode), i.Blend)),
						 new XElement("color",
							 new XElement("r", i.Color.X),
							 new XElement("g", i.Color.Y),
							 new XElement("b", i.Color.Z),
							 new XElement("a", i.Color.W)
						 ),
						 new XElement("layer", i.Layer)
					)
				)
			);



			xml.Save(fileName);
		}

		public void Update()
		{
			if (particle != null)
			{
				particle.Update();
			}
		}

		public void Draw()
		{
			if (particle != null)
			{
				if (Is2D)
				{
					particle.Draw();
				}
				else
				{
					particle.Draw(GetView());
				}
			}
		}

		Matrix GetView()
		{
			Vector3 Position = Vector3.Zero;

			return Matrix.LookAtRH(Position - Vector3.ForwardRH, Position, Vector3.Up);
			//return Matrix.CreateLookAt(Position - Vector3.Forward * 10, Position, Vector3.Up);
			//return Matrix.CreateLookAt(Position, Target, Upper);
		}

	}
}
