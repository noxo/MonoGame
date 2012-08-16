// #region License
// /*
// Microsoft Public License (Ms-PL)
// XnaTouch - Copyright © 2009 The XnaTouch Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
// #endregion License
// 
using System;
using System.Text;
using System.Collections.Generic;


using GL20 = OpenTK.Graphics.ES20.GL;
using ALL20 = OpenTK.Graphics.ES20.All;
using OpenTK.Graphics.ES20;
using Microsoft.Xna.Framework;
using OpenTK;

namespace Microsoft.Xna.Framework.Graphics
{
	public class SpriteBatch : GraphicsResource
	{
		SpriteBatcher _batcher;
		SpriteSortMode _sortMode;
		BlendState _blendState;
		SamplerState _samplerState;
		DepthStencilState _depthStencilState;
		RasterizerState _rasterizerState;
		Effect _effect;
		Matrix _matrix = Matrix.Identity;
		Rectangle tempRect = new Rectangle (0, 0, 0, 0);
		Vector2 texCoordTL = new Vector2 (0, 0);
		Vector2 texCoordBR = new Vector2 (0, 0);
		
		//OpenGLES2 variables
		int program;
		Matrix matWVPScreen, matWVPFramebuffer, matProjection, matViewScreen, matViewFramebuffer;
		int uniformWVP, uniformTex;

		public SpriteBatch (GraphicsDevice graphicsDevice)
		{
			if (graphicsDevice == null) {
				throw new ArgumentException ("graphicsDevice");
			}	
			
			this.graphicsDevice = graphicsDevice;
			
			_batcher = new SpriteBatcher ();
			InitGL20();

		}
		
		/// <summary>
		///Initialize shaders and program on OpenGLES2.0
		/// </summary>
		private void InitGL20 ()
		{
			string vertexShaderSrc = @" uniform mat4 uMVPMatrix;
											attribute vec4 aPosition;
											attribute vec2 aTexCoord;
											attribute vec4 aTint;
											varying vec2 vTexCoord;
											varying vec4 vTint;
											void main()
											{
												vTexCoord = aTexCoord;
												vTint = aTint;
												gl_Position = uMVPMatrix * aPosition;
											}";
            
			string fragmentShaderSrc = @"precision mediump float;
											varying vec2 vTexCoord;
											varying vec4 vTint;
											uniform sampler2D sTexture;
											void main()
											{
												vec4 baseColor = texture2D(sTexture, vTexCoord);
												gl_FragColor = baseColor * vTint;
											}";
				
			int vertexShader = LoadShader (ShaderType.VertexShader, vertexShaderSrc);
			int fragmentShader = LoadShader (ShaderType.FragmentShader, fragmentShaderSrc);
			
			program = GL20.CreateProgram ();
			
			if (program == 0)
				throw new InvalidOperationException ("Unable to create program");
	
			GL20.AttachShader (program, vertexShader);
			GL20.AttachShader (program, fragmentShader);
	            
			//Set position
			GL20.BindAttribLocation (program, _batcher.attributePosition, "aPosition");
			GL20.BindAttribLocation (program, _batcher.attributeTexCoord, "aTexCoord");
			GL20.BindAttribLocation (program, _batcher.attributeTint, "aTint");
			
			GL20.LinkProgram (program);
	
			int linked = 0;
			//GL20.GetProgram(

			GL20.GetProgram (program, ProgramParameter.LinkStatus, out linked);

			if (linked == 0) {
				// link failed
				int length = 0;
				GL20.GetProgram (program, ProgramParameter.InfoLogLength, out length);
				if (length > 0) {
					var log = new StringBuilder (length);
					GL20.GetProgramInfoLog (program, length, out length, log);

				}
	
				GL20.DeleteProgram (program);
				throw new InvalidOperationException ("Unable to link program");
			}
	
			UpdateWorldMatrixOrientation ();
			GetUniformVariables ();
			
		}
	
		/// <summary>
		/// Build the shaders
		/// </summary>
		private int LoadShader (ShaderType type, string source)
		{
			int shader = GL20.CreateShader (type);

			if (shader == 0)
				throw new InvalidOperationException (string.Format (
					"Unable to create shader: {0}", GL20.GetError ())
				);
	        
			// Load the shader source
			int length = 0;
			GL20.ShaderSource (shader, 1, new string[] {source}, (int[])null);
	           
			// Compile the shader
			GL20.CompileShader (shader);
	               
			int compiled = 0;
			GL20.GetShader (shader, ShaderParameter.CompileStatus, out compiled);
			if (compiled == 0) {
				length = 0;
				GL20.GetShader (shader, ShaderParameter.InfoLogLength, out length);
				if (length > 0) {
					var log = new StringBuilder (length);
					GL20.GetShaderInfoLog (shader, length, out length, log);
#if DEBUG					
	                    Console.WriteLine("GL2" + log.ToString ());
#endif
				}
	
				GL20.DeleteShader (shader);
				throw new InvalidOperationException ("Unable to compile shader of type : " + type.ToString ());
			}
	
			return shader;
	        
		}
	
		private void GetUniformVariables ()
		{
			uniformWVP = GL20.GetUniformLocation (program, "uMVPMatrix");
			uniformTex = GL20.GetUniformLocation (program, "sTexture");
		}
		
		private void SetUniformMatrix (int location, bool transpose, ref Matrix matrix)
		{
			unsafe {				
				fixed (float* matrix_ptr = &matrix.M11) {
					GL20.UniformMatrix4 (location, 1, transpose, matrix_ptr);
				}
			}
		}
		
		public void Begin ()
		{
			Begin (SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);			
		}
		
		public void Begin (SpriteSortMode sortMode, BlendState blendState)
		{
			Begin (sortMode, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);			
		}
		
		public void Begin (SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
		{	
			Begin (sortMode, blendState, samplerState, depthStencilState, rasterizerState, null, Matrix.Identity);	
		}
		
		public void Begin (SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
		{
			Begin (sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, Matrix.Identity);			
		}
		
		public void Begin (SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
		{
			_sortMode = sortMode;

			_blendState = blendState ?? BlendState.AlphaBlend;
			_depthStencilState = depthStencilState ?? DepthStencilState.None;
			_samplerState = samplerState ?? SamplerState.LinearClamp;
			_rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
			
			if (effect != null)
				_effect = effect;
			_matrix = transformMatrix;
		}
		
		public void End ()
		{
			EndGL20();


		}
		
		private void EndGL20 ()
		{
			// Disable Blending by default = BlendState.Opaque
			GL20.Disable (EnableCap.Blend);
			
			// set the blend mode
			if (_blendState == BlendState.NonPremultiplied) {
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				GL.Enable (EnableCap.Blend);
			}
			
			if (_blendState == BlendState.AlphaBlend) {
				GL.BlendFunc (BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
				GL.Enable (EnableCap.Blend);				
			}
			
			if (_blendState == BlendState.Additive) {
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
				GL.Enable (EnableCap.Blend);	
			}		

			//CullMode

			GL20.FrontFace (FrontFaceDirection.Cw);
			GL20.Enable (EnableCap.CullFace);
			
			
			UpdateWorldMatrixOrientation ();
			
			// Configure ViewPort
			var client = Game.Instance.view.ClientBounds;
			GL20.Viewport (client.X, client.Y, client.Width, client.Height);
			GL20.UseProgram (program);
			
		
                        
			if (GraphicsDevice.DefaultFrameBuffer) {
				GL20.CullFace (CullFaceMode.Back);
				SetUniformMatrix (uniformWVP, false, ref matWVPScreen);
			} else {

				GL20.CullFace (CullFaceMode.Front);
				SetUniformMatrix (uniformWVP, false, ref matWVPFramebuffer);

				// FIXME: Why clear the framebuffer during End?
				//        Doing so makes it so that only the
				//        final Begin/End pair in a frame can
				//        ever be shown.  Is that desirable?
				//GL20.ClearColor(0.0f,0.0f,0.0f,0.0f);
				//GL20.Clear((int) (ALL20.ColorBufferBit | ALL20.DepthBufferBit));
			}

			_batcher.DrawBatch (_sortMode, _samplerState);
			// TODO
			//GL20.Disable (EnableCap.Texture2D);
		}
		

	
		private void UpdateWorldMatrixOrientation ()
		{
			// TODO: It may be desirable to do a last display
			//       orientation here like Android does, to
			//       avoid calculating these matrices again
			//       and again.
			var viewport = graphicsDevice.Viewport;
			matViewScreen =
				Matrix.CreateRotationZ ((float)Math.PI) *
				Matrix.CreateRotationY ((float)Math.PI) *
				Matrix.CreateTranslation (-viewport.Width / 2, viewport.Height / 2, 1);

			matViewFramebuffer = Matrix.CreateTranslation (-viewport.Width / 2, -viewport.Height / 2, 1);

			matProjection = Matrix.CreateOrthographic (viewport.Width, viewport.Height, -1f, 1f);

			// FIXME: It is a significant weakness to have separate
			//        matrices for the screen and framebuffer.  It
			//        means that visual unit tests can't
			//        necessarily be trusted.  Screens are just
			//        another kind of framebuffer.
			matWVPScreen = _matrix * matViewScreen * matProjection;
			matWVPFramebuffer = _matrix * matViewFramebuffer * matProjection;
		}

		public void Draw 
			(
			 Texture2D texture,
			 Vector2 position,
			 Nullable<Rectangle> sourceRectangle,
			 Color color,
			 float rotation,
			 Vector2 origin,
			 Vector2 scale,
			 SpriteEffects effect,
			 float depth 
		)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = depth;
			item.TextureID = (int)texture.ID;
				
			if (sourceRectangle.HasValue) {
				tempRect = sourceRectangle.Value;
			} else {
				tempRect.X = 0;
				tempRect.Y = 0;
				tempRect.Width = texture.Width;
				tempRect.Height = texture.Height;				
			}
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			if ((effect & SpriteEffects.FlipVertically) != 0) {
				float temp = texCoordBR.Y;
				texCoordBR.Y = texCoordTL.Y;
				texCoordTL.Y = temp;
			}
			if ((effect & SpriteEffects.FlipHorizontally) != 0) {
				float temp = texCoordBR.X;
				texCoordBR.X = texCoordTL.X;
				texCoordTL.X = temp;
			}
			
			item.Set
				(
				 position.X,
				 position.Y,
				 -origin.X * scale.X,
				 -origin.Y * scale.Y,
				 tempRect.Width * scale.X,
				 tempRect.Height * scale.Y,
				 (float)Math.Sin (rotation),
				 (float)Math.Cos (rotation),
				 color,
				 texCoordTL,
				 texCoordBR
			);
		}
		
		public void Draw 
			(
			 Texture2D texture,
			 Vector2 position,
			 Nullable<Rectangle> sourceRectangle,
			 Color color,
			 float rotation,
			 Vector2 origin,
			 float scale,
			 SpriteEffects effect,
			 float depth 
		)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = depth;
			item.TextureID = (int)texture.ID;
						
			if (sourceRectangle.HasValue) {
				tempRect = sourceRectangle.Value;
			} else {
				tempRect.X = 0;
				tempRect.Y = 0;
				tempRect.Width = texture.Width;
				tempRect.Height = texture.Height;
			}
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			if ((effect & SpriteEffects.FlipVertically) != 0) {
				float temp = texCoordBR.Y;
				texCoordBR.Y = texCoordTL.Y;
				texCoordTL.Y = temp;
			}
			if ((effect & SpriteEffects.FlipHorizontally) != 0) {
				float temp = texCoordBR.X;
				texCoordBR.X = texCoordTL.X;
				texCoordTL.X = temp;
			}
			item.Set
				(
				 position.X,
				 position.Y,
				 -origin.X * scale,
				 -origin.Y * scale,
				 tempRect.Width * scale,
				 tempRect.Height * scale,
				 (float)Math.Sin (rotation),
				 (float)Math.Cos (rotation),
				 color,
				 texCoordTL,
				 texCoordBR
			);
		}
		
		public void Draw (
         	Texture2D texture,
         	Rectangle destinationRectangle,
         	Nullable<Rectangle> sourceRectangle,
         	Color color,
         	float rotation,
         	Vector2 origin,
         	SpriteEffects effect,
         	float depth
		)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = depth;
			item.TextureID = (int)texture.ID;
						
			if (sourceRectangle.HasValue) {
				tempRect = sourceRectangle.Value;
			} else {
				tempRect.X = 0;
				tempRect.Y = 0;
				tempRect.Width = texture.Width;
				tempRect.Height = texture.Height;
			}
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			if ((effect & SpriteEffects.FlipVertically) != 0) {
				float temp = texCoordBR.Y;
				texCoordBR.Y = texCoordTL.Y;
				texCoordTL.Y = temp;
			}
			if ((effect & SpriteEffects.FlipHorizontally) != 0) {
				float temp = texCoordBR.X;
				texCoordBR.X = texCoordTL.X;
				texCoordTL.X = temp;
			}
			
			item.Set 
				(
				 destinationRectangle.X, 
				 destinationRectangle.Y, 
				 -origin.X * ((float)destinationRectangle.Width / (float)tempRect.Width),  //JEPJ
		         -origin.Y * ((float)destinationRectangle.Height / (float)tempRect.Height), //JEPJ 
				 destinationRectangle.Width,
				 destinationRectangle.Height,
				 (float)Math.Sin (rotation),
				 (float)Math.Cos (rotation),
				 color,
				 texCoordTL,
				 texCoordBR);			
		}
		
		public void Draw (Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = 0.0f;
			item.TextureID = (int)texture.ID;
			
			if (sourceRectangle.HasValue) {
				tempRect = sourceRectangle.Value;
			} else {
				tempRect.X = 0;
				tempRect.Y = 0;
				tempRect.Width = texture.Width;
				tempRect.Height = texture.Height;
			}
			
			
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			item.Set (position.X, position.Y, tempRect.Width, tempRect.Height, color, texCoordTL, texCoordBR);
		}
		
		public void Draw (Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = 0.0f;
			item.TextureID = (int)texture.ID;
			
			if (sourceRectangle.HasValue) {
				tempRect = sourceRectangle.Value;
			} else {
				tempRect.X = 0;
				tempRect.Y = 0;
				tempRect.Width = texture.Width;
				tempRect.Height = texture.Height;
			}		
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			item.Set 
				(
				 destinationRectangle.X, 
				 destinationRectangle.Y, 
				 destinationRectangle.Width, 
				 destinationRectangle.Height, 
				 color, 
				 texCoordTL, 
				 texCoordBR);
		}
		
		public void Draw 
			(
			 Texture2D texture,
			 Vector2 position,
			 Color color
		)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = 0;
			item.TextureID = (int)texture.ID;
			
			tempRect.X = 0;
			tempRect.Y = 0;
			tempRect.Width = texture.Width;
			tempRect.Height = texture.Height;
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			item.Set 
				(
				 position.X,
			     position.Y,
				 tempRect.Width,
				 tempRect.Height,
				 color,
				 texCoordTL,
				 texCoordBR
			);
		}
		
		public void Draw (Texture2D texture, Rectangle rectangle, Color color)
		{
			if (texture == null) {
				throw new ArgumentException ("texture");
			}
			
			SpriteBatchItem item = _batcher.CreateBatchItem ();
			
			item.Depth = 0;
			item.TextureID = (int)texture.ID;
			
			tempRect.X = 0;
			tempRect.Y = 0;
			tempRect.Width = texture.Width;
			tempRect.Height = texture.Height;			
			
			if (texture.Image == null) {
				float texWidthRatio = 1.0f / (float)texture.Width;
				float texHeightRatio = 1.0f / (float)texture.Height;
				// We are initially flipped vertically so we need to flip the corners so that
				//  the image is bottom side up to display correctly
				texCoordTL.X = tempRect.X * texWidthRatio;
				//texCoordTL.Y = (tempRect.Y + tempRect.Height) * texHeightRatio;
				texCoordTL.Y = 1.0f - tempRect.Y * texHeightRatio;
				
				texCoordBR.X = (tempRect.X + tempRect.Width) * texWidthRatio;
				//texCoordBR.Y = tempRect.Y * texHeightRatio;
				texCoordBR.Y = 1.0f - (tempRect.Y + tempRect.Height) * texHeightRatio;
				
			} else {
				texCoordTL.X = texture.Image.GetTextureCoordX (tempRect.X);
				texCoordTL.Y = texture.Image.GetTextureCoordY (tempRect.Y);
				texCoordBR.X = texture.Image.GetTextureCoordX (tempRect.X + tempRect.Width);
				texCoordBR.Y = texture.Image.GetTextureCoordY (tempRect.Y + tempRect.Height);
			}
			
			item.Set
				(
				 rectangle.X,
				 rectangle.Y,
				 rectangle.Width,
				 rectangle.Height,
				 color,
				 texCoordTL,
				 texCoordBR
			);
		}

		public void DrawString (SpriteFont spriteFont, string text, Vector2 position, Color color)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			spriteFont.DrawInto (
				this, text, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		}

		public void DrawString (
			SpriteFont spriteFont, string text, Vector2 position, Color color,
			float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			var scaleVec = new Vector2 (scale, scale);
			spriteFont.DrawInto (this, text, position, color, rotation, origin, scaleVec, effects, depth);
		}

		public void DrawString (
			SpriteFont spriteFont, string text, Vector2 position, Color color,
			float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			spriteFont.DrawInto (this, text, position, color, rotation, origin, scale, effect, depth);
		}

		public void DrawString (SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			spriteFont.DrawInto (
				this, text, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		}

		public void DrawString (
			SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
			float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			var scaleVec = new Vector2 (scale, scale);
			spriteFont.DrawInto (this, text, position, color, rotation, origin, scaleVec, effects, depth);
		}

		public void DrawString (
			SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
			float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
		{
			if (spriteFont == null)
				throw new ArgumentNullException ("spriteFont");

			spriteFont.DrawInto (this, text, position, color, rotation, origin, scale, effect, depth);
		}
	}
}

