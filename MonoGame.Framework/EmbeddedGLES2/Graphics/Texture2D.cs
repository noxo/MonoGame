#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License
using System.IO;
using System;
using System.Drawing;
using Microsoft.Xna.Framework.Content;
using GL20 = OpenTK.Graphics.ES20.GL;
using ALL20 = OpenTK.Graphics.ES20.All;
using OpenTK.Graphics.ES20;
using Path = System.IO.Path;
using System.Runtime.InteropServices;
using GLPixelFormat = OpenTK.Graphics.ES20.PixelFormat;

namespace Microsoft.Xna.Framework.Graphics
{
	public class Texture2D : Texture
	{
		private ESImage texture;
		protected int _width;
		protected int _height;
		private bool _mipmap;
		
		internal bool IsSpriteFontTexture { get; set; }
		
		// my change
		// --------
		public uint ID {
			get { 
				if (texture == null)
					return (uint)_textureId;
				else
					return texture.Name;
			}
		}
		// --------
		internal ESImage Image {
			get {
				return texture;
			}
		}
		
		public Rectangle SourceRect {
			get {
				return new Rectangle (0, 0, texture.ImageWidth, texture.ImageHeight);
			}
		}
		
		public Rectangle Bounds {
			get {
				return new Rectangle (0, 0, texture.ImageWidth, texture.ImageHeight);
			}
		}

		internal Texture2D(ESImage theImage)
		{
			texture = theImage;
			_width = texture.ImageWidth;
			_height = texture.ImageHeight;
			_format = texture.Format;
			_textureId = (int)theImage.Name;
		}

		internal Texture2D (GraphicsDevice graphicsDevice, ESImage theImage)
		{
			SetInfoFromESImage (theImage);
			this.graphicsDevice = graphicsDevice;

			Disposing += Texture2D_Disposing;
		}

		void SetInfoFromESImage (ESImage image)
		{
			texture = image;
			_width = texture.ImageWidth;
			_height = texture.ImageHeight;
			_format = texture.Format;
			_textureId = (int)image.Name;
		}

		void Texture2D_Disposing (object sender, EventArgs e)
		{
			if (texture != null) {
				texture.Dispose ();
				texture = null;
			}
		}
		
		public Texture2D (GraphicsDevice graphicsDevice, int width, int height) : 
			this (graphicsDevice, width, height, false, SurfaceFormat.Color)
		{
			
		}
		
		public Texture2D (GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
		{
			this.graphicsDevice = graphicsDevice;
			
			// This is needed in OpenGL ES 1.1 as it only supports power of 2 textures
			int xTexSize = 1;
			int yTexSize = 1;
			while (width > xTexSize && height > yTexSize) {
				if (width > xTexSize)
					xTexSize *= 2;
				if (height > yTexSize)
					yTexSize *= 2;
			}

			this._width = xTexSize;
			this._height = yTexSize;
			
			this._format = format;
			this._mipmap = mipMap;

			texture = new ESImage (width, height);

			Disposing += Texture2D_Disposing;
		}

		public Color GetPixel (int x, int y)
		{
			var result = new Color (0, 0, 0, 0);

			return result;
		}

		public void SetPixel (int x, int y, byte red, byte green, byte blue, byte alpha)
		{
			throw new NotImplementedException ();
		}

		public void SetData<T> (T[] data)
		{
			SetData (data, 0, data.Length, SetDataOptions.None);
		}

		public int Width {
			get {
				return texture != null ? texture.Size.Width : _width;
			}
		}

		public int Height {
			get {
				return  texture != null ? texture.Size.Height : _height;
			}
		}

		public TextureUsage TextureUsage {
			get { 
				throw new NotImplementedException ();
			}
		}

		public static Texture2D FromFile (GraphicsDevice graphicsDevice, string filename, int width, int height)
		{
			Bitmap image = (Bitmap)Bitmap.FromFile(filename);
			if (image == null)
			{
				throw new ContentLoadException("Error loading file: " + filename);
			}
			Console.WriteLine("Loading texture "+ filename +" from file..");
			// TODO resize
			ESImage theTexture = new ESImage(image);
			Texture2D result = new Texture2D(theTexture);
			result.Name = Path.GetFileNameWithoutExtension(filename);
			return result;
		}

		internal void Reload (string filename)
		{
			texture.Dispose ();
			texture = null;

			Bitmap image = (Bitmap)Bitmap.FromFile(filename);
			if (image == null)
			{
				throw new ContentLoadException("Error loading file: " + filename);
			}
			Console.WriteLine("Loading texture "+ filename +" from file..");
			// TODO resize
			ESImage theTexture = new ESImage(image);
			SetInfoFromESImage (theTexture);
		}

		internal void Reload ()
		{
			texture.ForceRetryToCreateTexture ();
		}

		public static Texture2D FromFile (GraphicsDevice graphicsDevice, Stream textureStream, int numberBytes)
		{
			throw new NotImplementedException ();
		}


		public static Texture2D FromFile (GraphicsDevice graphicsDevice, string filename)
		{
			Bitmap image = (Bitmap)Bitmap.FromFile(filename);
			if (image == null)
			{
				throw new ContentLoadException("Error loading file: " + filename);
			}
			Console.WriteLine("Loading texture "+ filename +" from file..");
			// TODO resize
			ESImage theTexture = new ESImage(image);
			Texture2D result = new Texture2D(theTexture);
			result.Name = Path.GetFileNameWithoutExtension(filename);
			return result;
		}
		
		private void SetData (int index, byte red, byte green, byte blue, byte alpha, byte[] textureData)
		{
			switch (_format) {				
			case SurfaceFormat.Color /*kTexture2DPixelFormat_RGBA8888*/:
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt3:
				index *= 4;
				textureData [index] = red;
				textureData [index + 1] = green;
				textureData [index + 2] = blue;
				textureData [index + 3] = alpha;				
				break;
				
			// TODO: Implement the rest of these but lack of knowledge and examples prevents this for now
			case SurfaceFormat.Bgra4444 /*kTexture2DPixelFormat_RGBA4444*/:
				break;
			case SurfaceFormat.Bgra5551 /*kTexture2DPixelFormat_RGB5A1*/:
				break;
			case SurfaceFormat.Alpha8 /*kTexture2DPixelFormat_A8*/:
				break;
			default:
				throw new NotSupportedException ("Texture format");
				;					
			}
		}
		
		private byte[] AllocColorData ()
		{
			switch (_format) {				
			case SurfaceFormat.Color /*kTexture2DPixelFormat_RGBA8888*/:
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt3:
				return new byte[(_width * _height) * 4];
				
			// TODO: Implement the rest of these but lack of knowledge and examples prevents this for now
			case SurfaceFormat.Bgra4444 /*kTexture2DPixelFormat_RGBA4444*/:
				break;
			case SurfaceFormat.Bgra5551 /*kTexture2DPixelFormat_RGB5A1*/:
				break;
			case SurfaceFormat.Alpha8 /*kTexture2DPixelFormat_A8*/:
				break;
			default:
				throw new NotSupportedException ("Texture format");
				;					
			}
			
			return null;
		}
		
		
		/*
		* perform an in-place swap from Quadrant 1 to Quadrant III format
		* (upside-down PostScript/GL to right side up QD/CG raster format)
		* We do this in-place, which requires more copying, but will touch
		* only half the pages.
		* 
		* Pixel reformatting may optionally be done here if needed.
		*/
		private void flipImageData (byte[] mData, int mWidth, int mHeight, int mByteWidth)
		{

			long top, bottom;
			byte[] buffer;
			long topP;
			long bottomP;
			long rowBytes;

			top = 0;
			bottom = mHeight - 1;
			rowBytes = mWidth * mByteWidth;
			buffer = new byte[rowBytes];

			while (top < bottom) {
				topP = top * rowBytes;
				bottomP = bottom * rowBytes;

				/*
				* Save and swap scanlines.
				*
				* This code does a simple in-place exchange with a temp buffer.
				* If you need to reformat the pixels, replace the first two Array.Copy
				* calls with your own custom pixel reformatter.
				*/

				Array.Copy (mData, topP, buffer, 0, rowBytes);
				Array.Copy (mData, bottomP, mData, topP, rowBytes);
				Array.Copy (buffer, 0, mData, bottomP, rowBytes);

				++top;
				--bottom;

			}
		}

		public void SetData<T> (T[] data, int startIndex, int elementCount, SetDataOptions options)
		{
			if (data == null) {
				throw new ArgumentNullException ("Argument data can not be null.");
			}
			
			if (startIndex < 0 || startIndex > data.Length - 1) {
				throw new ArgumentNullException ("Argument startIndex in invalid.");
			}			

			if (elementCount < 0 || (startIndex + elementCount) > data.Length) {
				throw new ArgumentNullException ("Argument elementCount is invalid.");
			}			
			
			byte[] textureData = AllocColorData ();
			// we now have a texture not based on an outside image source
			// now we check what type was passed
			if (typeof(T) == typeof(Color)) {

				for (int x = startIndex; x < elementCount; x++) {
					var color = (Color)(object)data [x];
					SetData (x, color.R, color.G, color.B, color.A, textureData);
					
				}
				
				// For RenderTextures we need to flip the data.
				if (texture == null) {
					flipImageData (textureData, _width, _height, 4);
				}
			}
			
			// when we are all done we need apply the changes
			Apply (textureData);
		}

		public void SetData<T> (int level, Rectangle? rect, T[] data, int startIndex, int elementCount, SetDataOptions options)
		{
			if (data == null) {
				throw new ArgumentException ("data cannot be null");
			}

			if (data.Length < startIndex + elementCount) {
				throw new ArgumentException ("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
			}

			Rectangle r;
			if (rect != null) {
				r = rect.Value;
			} else {
				r = new Rectangle (0, 0, Width, Height);
			}
		}

		private byte[] GetImageData (int level)
		{

			int framebufferId = -1;
			int renderBufferID = -1;
            
			GL20.GenFramebuffers (1, out framebufferId);
			GL20.BindFramebuffer (FramebufferTarget.Framebuffer, framebufferId);
			//renderBufferIDs = new int[currentRenderTargets];
			GL20.GenRenderbuffers (1, out renderBufferID);
	
			// attach the texture to FBO color attachment point
			GL20.FramebufferTexture2D (FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, TextureTarget.Texture2D, ID, 0);

			// create a renderbuffer object to store depth info
			GL20.BindRenderbuffer (RenderbufferTarget.Renderbuffer, renderBufferID);
// !!!!!!!!!!! RenderbufferInternalFormat.DepthComponent24Oe
			GL20.RenderbufferStorage (RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, _width, _height);

			// attach the renderbuffer to depth attachment point
			GL20.FramebufferRenderbuffer (FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, renderBufferID);

			FramebufferErrorCode status = GL20.CheckFramebufferStatus (FramebufferTarget.Framebuffer);
	
			if (status != FramebufferErrorCode.FramebufferComplete)
				throw new Exception ("Error creating framebuffer: " + status);	

			byte[] imageInfo;
			int sz = 0;

			switch (_format) {
				case SurfaceFormat.Color: //kTexture2DPixelFormat_RGBA8888
				case SurfaceFormat.Dxt3:

					sz = 4;
					imageInfo = new byte[(_width * _height) * sz];
					break;
				case SurfaceFormat.Bgra4444: //kTexture2DPixelFormat_RGBA4444
					sz = 2;
					imageInfo = new byte[(_width * _height) * sz];

					break;
				case SurfaceFormat.Bgra5551: //kTexture2DPixelFormat_RGB5A1
					sz = 2;
					imageInfo = new byte[(_width * _height) * sz];
					break;
				case SurfaceFormat.Alpha8:  // kTexture2DPixelFormat_A8 
					sz = 1;
					imageInfo = new byte[(_width * _height) * sz];
					break;
				default:
					throw new NotSupportedException ("Texture format");
			}

			GL20.ReadPixels (0, 0, _width, _height, PixelFormat.Rgba, PixelType.UnsignedByte, imageInfo);
			GL20.FramebufferRenderbuffer (FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
			GL20.DeleteRenderbuffers (1, ref renderBufferID);
			GL20.DeleteFramebuffers (1, ref framebufferId);
			GL20.BindFramebuffer (FramebufferTarget.Framebuffer, 0);
			
			return imageInfo;

		}

		public void GetData<T> (T[] data)
		{

			if (data == null) {
				throw new ArgumentException ("data cannot be null");
			}

			if (data == null) {
				throw new ArgumentException ("data cannot be null");
			}

			GetData (0, null, data, 0, data.Length);

		}

		public void GetData<T> (T[] data, int startIndex, int elementCount)
		{
			GetData<T> (0, null, data, startIndex, elementCount);
		}

		public void GetData<T> (int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
		{
			if (data == null) {
				throw new ArgumentException ("data cannot be null");
			}

			if (data.Length < startIndex + elementCount) {
				throw new ArgumentException ("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
			}

			Rectangle r;
			if (rect != null) {
				r = rect.Value;
			} else {
				r = new Rectangle (0, 0, Width, Height);
			}

			byte[] imageInfo = GetImageData (0);
			
			// Get the Color values
			if (typeof(T) == typeof(uint)) {
				Color[] colors = new Color[elementCount];
				GetData<Color> (level, rect, colors, startIndex, elementCount);
				uint[] final = data as uint[];
				for (int i = 0; i < final.Length; i++) {
					final [i] = (uint)
					(
						colors [i].R << 24 |
						colors [i].G << 16 |
						colors [i].B << 8 |
						colors [i].A
					);
				}
			}
            // Get the Color values
            else if ((typeof(T) == typeof(Color))) {


				// Left this here for documentation - Not sure what it does but the
				// routine looks important

				//Convert "RRRRRRRRRGGGGGGGGBBBBBBBBAAAAAAAA" to "RRRRRGGGGGGBBBBB"
				/*
                if(pixelFormat == SurfaceFormat.Rgb32) {
                    tempData = Marshal.AllocHGlobal(height * width * 2);

                    int d32;
                    short d16;
                    int inPixel32Count=0,outPixel16Count=0;
                    for(i = 0; i < width * height; ++i, inPixel32Count+=sizeof(int))
                    {
                        d32 = Marshal.ReadInt32(imageData,inPixel32Count);
                        short R = (short)((((d32 >> 0) & 0xFF) >> 3) << 11);
                        short G = (short)((((d32 >> 8) & 0xFF) >> 2) << 5);
                        short B = (short)((((d32 >> 16) & 0xFF) >> 3) << 0);
                        d16 = (short)  (R | G | B);
                        Marshal.WriteInt16(tempData,outPixel16Count,d16);
                        outPixel16Count += sizeof(short);
                    }
                    Marshal.FreeHGlobal(imageData);
                    imageData = tempData;			
                }									
                */

				int rWidth = r.Width;
				int rHeight = r.Height;

				if (texture == null) {
					// For rendertargets we need to loop through and load the elements
					// backwards because the texture data is flipped vertically and horizontally
					var dataEnd = (rWidth * rHeight) - 1;
					var dataPos = 0;
					var dataRowColOffset = 0;
					for (int y = r.Top; y < rHeight; y++) {
						for (int x = r.Left; x < rWidth; x++) {
							var result = new Color (0, 0, 0, 0);
							dataRowColOffset = ((y * rWidth) + x);
							switch (_format) {
							case SurfaceFormat.Color: //kTexture2DPixelFormat_RGBA8888
							case SurfaceFormat.Dxt3:

								dataPos = dataRowColOffset * 4;

								result.R = imageInfo [dataPos];
								result.G = imageInfo [dataPos + 1];
								result.B = imageInfo [dataPos + 2];
								result.A = imageInfo [dataPos + 3];
								break;
							default:
								throw new NotSupportedException ("Texture format");
							}
							data [dataEnd - dataRowColOffset] = (T)(object)result;
						}


					}
				} else {
					// Loop through and extract the data but we need to load it 
					var dataRowColOffset = 0;
					var sz = 0;
					var pixelOffset = 0;
					for (int y = r.Top; y < rHeight; y++) {
						for (int x = r.Left; x < rWidth; x++) {
							var result = new Color (0, 0, 0, 0);
							dataRowColOffset = ((y * r.Width) + x);
							switch (_format) {
							case SurfaceFormat.Color: //kTexture2DPixelFormat_RGBA8888
							case SurfaceFormat.Dxt3:
								sz = 4;
								pixelOffset = dataRowColOffset * sz;
								result.R = imageInfo [pixelOffset];
								result.G = imageInfo [pixelOffset + 1];
								result.B = imageInfo [pixelOffset + 2];
								result.A = imageInfo [pixelOffset + 3];
								break;
							case SurfaceFormat.Bgra4444: //kTexture2DPixelFormat_RGBA4444
                                    //								sz = 2;
                                    //								pos = ((y * imageSize.Width) + x) * sz;
                                    //								pixelOffset = new IntPtr (imageData.ToInt64 () + pos);
                                    //	
                                    //								Marshal.Copy (pixelOffset, pixel, 0, 4);	
                                    //	
                                    //								result.R = pixel [0];
                                    //								result.G = pixel [1];
                                    //								result.B = pixel [2];
                                    //								result.A = pixel [3];
								sz = 2;
								pixelOffset = dataRowColOffset * sz;
								result.R = imageInfo [pixelOffset];
								result.G = imageInfo [pixelOffset + 1];
								result.B = imageInfo [pixelOffset + 2];
								result.A = imageInfo [pixelOffset + 3];
								break;
							case SurfaceFormat.Bgra5551: //kTexture2DPixelFormat_RGB5A1
                                    //								sz = 2;
                                    //								pos = ((y * imageSize.Width) + x) * sz;
                                    //								pixelOffset = new IntPtr (imageData.ToInt64 () + pos);
                                    //								Marshal.Copy (pixelOffset, pixel, 0, 4);	
                                    //	
                                    //								result.R = pixel [0];
                                    //								result.G = pixel [1];
                                    //								result.B = pixel [2];
                                    //								result.A = pixel [3];
								sz = 2;
								pixelOffset = dataRowColOffset * sz;
								result.R = imageInfo [pixelOffset];
								result.G = imageInfo [pixelOffset + 1];
								result.B = imageInfo [pixelOffset + 2];
								result.A = imageInfo [pixelOffset + 3];
								break;
							case SurfaceFormat.Alpha8:  // kTexture2DPixelFormat_A8 
                                    //								sz = 1;
                                    //								pos = ((y * imageSize.Width) + x) * sz;
                                    //								pixelOffset = new IntPtr (imageData.ToInt64 () + pos);								
                                    //								Marshal.Copy (pixelOffset, pixel, 0, 4);	
                                    //	
                                    //								result.A = pixel [0];
								sz = 1;
								pixelOffset = dataRowColOffset * sz;
								result.A = imageInfo [pixelOffset];
								break;
							default:
								throw new NotSupportedException ("Texture format");
							}
							data [dataRowColOffset] = (T)(object)result;
						}
					}
				}
			} else {
				throw new NotImplementedException ("GetData not implemented for type.");
			}
		}

		public void Apply ()
		{
			if (texture == null)
				return;

			if (_textureId == 0) {
				texture.RetryToCreateTexture ();
				if (texture.Name != 0) {
					_textureId = (int)texture.Name;
				}
			}

			GL20.BindTexture (TextureTarget.Texture2D, (uint)_textureId);
                
			var ss = graphicsDevice.SamplerStates [0];

			switch (ss.Filter) {
			case TextureFilter.Anisotropic:
				throw new NotImplementedException ();
				break;
			case TextureFilter.LinearMipPoint:

				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.LinearMipmapLinear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
				break;
			case TextureFilter.Linear:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.Linear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);
				break;
			case TextureFilter.Point:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.Nearest);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Nearest);
				break;
			case TextureFilter.PointMipLinear:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.NearestMipmapLinear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Nearest);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
				break;
			case TextureFilter.MinPointMagLinearMipLinear:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.NearestMipmapLinear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
				break;
			case TextureFilter.MinPointMagLinearMipPoint:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.NearestMipmapNearest);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);
				//GL20.TexParameter (TextureTarget.Texture2D, ALL20.GenerateMipmapHint, (int)ALL20.True);
				break;
			case TextureFilter.MinLinearMagPointMipLinear:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.LinearMipmapLinear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Nearest);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
				break;
			case TextureFilter.MinLinearMagPointMipPoint:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.LinearMipmapNearest);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Nearest);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
				break;
			default:
				throw new NotImplementedException ();
			}

			switch (ss.AddressU) {
			case TextureAddressMode.Clamp:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)OpenTK.Graphics.ES20.TextureWrapMode.ClampToEdge);
				break;
			case TextureAddressMode.Mirror:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)OpenTK.Graphics.ES20.TextureWrapMode.MirroredRepeat);
				break;
			case TextureAddressMode.Wrap:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)OpenTK.Graphics.ES20.TextureWrapMode.Repeat);
				break;
			default:
				throw new NotImplementedException ();
			}

			switch (ss.AddressV) {
			case TextureAddressMode.Clamp:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)OpenTK.Graphics.ES20.TextureWrapMode.ClampToEdge);
				break;
			case TextureAddressMode.Mirror:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)OpenTK.Graphics.ES20.TextureWrapMode.MirroredRepeat);
				break;
			case TextureAddressMode.Wrap:
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)OpenTK.Graphics.ES20.TextureWrapMode.Repeat);
				break;
			default:
				throw new NotImplementedException ();
			}

			switch (ss.AddressW) {
//			case TextureAddressMode.Clamp:
//				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapROes, (float)OpenTK.Graphics.ES20.TextureWrapMode.ClampToEdge);
//				break;
//			case TextureAddressMode.Mirror:
//				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapROes, (float)OpenTK.Graphics.ES20.TextureWrapMode.MirroredRepeat);
//				break;
//			case TextureAddressMode.Wrap:
//				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapROes, (float)OpenTK.Graphics.ES20.TextureWrapMode.Repeat);
//				break;
			default:
				throw new NotImplementedException ();
			}
            
  
		}
		
		private void Apply (byte[] textureData)
		{
			var s = texture.Size;
			GL20.BindTexture (TextureTarget.Texture2D, (uint)this.ID);			
			if (_mipmap) {
				// Taken from http://www.flexicoder.com/blog/index.php/2009/11/iphone-mipmaps/
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.LinearMipmapNearest);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);
				//GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.GenerateMipmapHint, (int)ALL20.True);
			} else {
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ALL20.Linear);
				GL20.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ALL20.Linear);                
			}	
			int len = textureData.Length;
			IntPtr data = Marshal.AllocHGlobal (len);
			Marshal.Copy (textureData, 0, data, len);
			GL20.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)s.Width, (int)s.Height, 0, GLPixelFormat.Rgba, PixelType.UnsignedByte, data);
			Marshal.FreeHGlobal (data);
				
				
			
		}
	}
}

