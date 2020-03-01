using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using HyoutaPluginBase;
using HyoutaTools.Tales.Vesperia.FPS4;
using HyoutaTools.Tales.Vesperia.Texture;
using HyoutaUtils;

namespace ToGLocInject {
	internal static class TextureProcessing {
		private enum TexConvMethod {
			Downscale2x,
			DownscaleTwoThirds,
			CropExpandCanvas,
			Clear,
			Delegate,
		}

		public delegate Bitmap TexConvDelegate(Bitmap wtex, Bitmap utex);

		private class TexConvRules {
			public int WTexId;
			public int UTexId;
			public TexConvMethod Method;
			public TexConvDelegate Delegate;
		}

		private static Color ChopBitsRGB5A3(Color color) {
			if (color.A == 255) {
				return Color.FromArgb(0xFF, color.R & 0xF8, color.G & 0xF8, color.B & 0xF8);
			} else {
				return Color.FromArgb(color.A & 0xE0, color.R & 0xF0, color.G & 0xF0, color.B & 0xF0);
			}
		}

		private static void ChopBitsRGB5A3(System.Drawing.Bitmap newImage) {
			for (int y = 0; y < newImage.Height; ++y) {
				for (int x = 0; x < newImage.Height; ++x) {
					newImage.SetPixel(x, y, ChopBitsRGB5A3(newImage.GetPixel(x, y)));
				}
			}
		}

		private static (List<System.Drawing.Color> colors, Dictionary<System.Drawing.Color, int> lookup) MedianCut(List<System.Drawing.Color> allColors, int stages) {
			MedianCutNode node = MedianCutInternal(allColors, stages);
			List<List<Color>> bucketedColors = new List<List<Color>>();
			node.GetColors(bucketedColors);

			List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
			foreach (List<Color> cs in bucketedColors) {
				colors.Add(cs[0]); // TODO: use average instead
			}

			Dictionary<System.Drawing.Color, int> colorLookupTable = new Dictionary<System.Drawing.Color, int>();
			for (int ci = 0; ci < bucketedColors.Count; ++ci) {
				foreach (Color c in bucketedColors[ci]) {
					colorLookupTable.Add(c, ci);
				}
			}

			return (colors, colorLookupTable);
		}

		private class MedianCutNode {
			public MedianCutNode LeftChild;
			public MedianCutNode RightChild;
			public List<Color> LeftColors;
			public List<Color> RightColors;

			public void GetColors(List<List<Color>> colors) {
				if (LeftChild != null) {
					LeftChild.GetColors(colors);
				}
				if (RightChild != null) {
					RightChild.GetColors(colors);
				}
				if (LeftColors != null && LeftColors.Count > 0) {
					colors.Add(LeftColors);
				}
				if (RightColors != null && RightColors.Count > 0) {
					colors.Add(RightColors);
				}
			}
		}

		private static MedianCutNode MedianCutInternal(List<System.Drawing.Color> colors, int stages) {
			if (colors.Count == 0) {
				return new MedianCutNode();
			}

			int minr = colors.Min(x => x.R);
			int maxr = colors.Max(x => x.R);
			int ming = colors.Min(x => x.G);
			int maxg = colors.Max(x => x.G);
			int minb = colors.Min(x => x.B);
			int maxb = colors.Max(x => x.B);
			int mina = colors.Min(x => x.A);
			int maxa = colors.Max(x => x.A);
			int diffr = maxr - minr;
			int diffg = maxg - ming;
			int diffb = maxb - minb;
			int diffa = maxa - mina;
			int biggestDiffIdx = 3;
			int biggestDiff = diffa;
			int biggestDiffMin = mina;
			int biggestDiffMax = maxa;
			if (biggestDiff < diffg) {
				biggestDiff = diffg;
				biggestDiffIdx = 1;
				biggestDiffMin = ming;
				biggestDiffMax = maxg;
			}
			if (biggestDiff < diffr) {
				biggestDiff = diffr;
				biggestDiffIdx = 0;
				biggestDiffMin = minr;
				biggestDiffMax = maxr;
			}
			if (biggestDiff < diffb) {
				biggestDiff = diffb;
				biggestDiffIdx = 2;
				biggestDiffMin = minb;
				biggestDiffMax = maxb;
			}

			int cutoff = (biggestDiffMin + biggestDiffMax + 1) / 2;
			List<Color> left = new List<Color>();
			List<Color> right = new List<Color>();
			foreach (Color c in colors) {
				if (c.GetColorComponent(biggestDiffIdx) < cutoff) {
					left.Add(c);
				} else {
					right.Add(c);
				}
			}

			MedianCutNode node = new MedianCutNode();
			if (stages <= 1) {
				node.LeftColors = left;
				node.RightColors = right;
			} else {
				node.LeftChild = MedianCutInternal(left, stages - 1);
				node.RightChild = MedianCutInternal(right, stages - 1);
			}

			return node;
		}

		private static (List<System.Drawing.Color> colors, Dictionary<System.Drawing.Color, int> lookup) GeneratePalette256(System.Drawing.Bitmap img) {
			List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
			for (int y = 0; y < img.Height; ++y) {
				for (int x = 0; x < img.Width; ++x) {
					var col = img.GetPixel(x, y);
					if (!colors.Contains(col)) {
						colors.Add(col);
					}
				}
			}

			if (colors.Count <= 256) {
				Dictionary<System.Drawing.Color, int> colorLookupTable = new Dictionary<System.Drawing.Color, int>();
				for (int ci = 0; ci < colors.Count; ++ci) {
					colorLookupTable.Add(colors[ci], ci);
				}

				return (colors, colorLookupTable);
			}

			return MedianCut(colors, 8);
		}

		public static Stream ProcessTexture(FileFetcher _fc, string name, DuplicatableStream jstream, DuplicatableStream ustream) {
			DuplicatableStream wstream = _fc.GetFile(name, Version.W);
			return ProcessTexture(name, ustream, wstream);
		}

		public static Stream ProcessTexture(string name, DuplicatableStream ustream, DuplicatableStream wstream) {
			FPS4 w = new FPS4(wstream.Duplicate());
			TXM wtxm = new TXM(w.GetChildByIndex(0).AsFile.DataStream.Duplicate());
			TXV wtxv = new TXV(wtxm, w.GetChildByIndex(1).AsFile.DataStream.Duplicate(), false);
			FPS4 u = new FPS4(ustream.Duplicate());
			TXM utxm = new TXM(u.GetChildByIndex(0).AsFile.DataStream.Duplicate());
			TXV utxv = new TXV(utxm, u.GetChildByIndex(1).AsFile.DataStream.Duplicate(), false);
			List<TexConvRules> convs = new List<TexConvRules>();
			if (name == "rootR.cpk/mg/tex/karuta.tex") {
				convs.Add(new TexConvRules() { WTexId = 2, UTexId = 1, Method = TexConvMethod.Delegate, Delegate = (wtex, utex) => DoKarutaHalfAndHalf(wtex, utex, 82, 134, 294) });
				convs.Add(new TexConvRules() { WTexId = 4, UTexId = 3, Method = TexConvMethod.Delegate, Delegate = (wtex, utex) => DoKarutaHalfAndHalf(wtex, utex, 86, 134, 294) });
				convs.Add(new TexConvRules() { WTexId = 7, UTexId = 7, Method = TexConvMethod.DownscaleTwoThirds });
				convs.Add(new TexConvRules() { WTexId = 13, UTexId = 13, Method = TexConvMethod.Delegate, Delegate = (wtex, utex) => DoKaruta13(wtex, utex) });
				convs.Add(new TexConvRules() { WTexId = 17, UTexId = 17, Method = TexConvMethod.DownscaleTwoThirds });
				convs.Add(new TexConvRules() { WTexId = 22, UTexId = 23, Method = TexConvMethod.Delegate, Delegate = (wtex, utex) => DownscaleTwoThirds(CropExpandCanvas(utex, 2, -2, (uint)(utex.Width + 3), (uint)(utex.Height - 3))) });
			} else if (name == "rootR.cpk/mnu/tex/main.tex") {
				convs.Add(new TexConvRules() { WTexId = 110, UTexId = 61, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 111, UTexId = 62, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 112, UTexId = 63, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 113, UTexId = 64, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 114, UTexId = 65, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 115, UTexId = 66, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 116, UTexId = 67, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 117, UTexId = 68, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 118, UTexId = 69, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 119, UTexId = 70, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 124, UTexId = 75, Method = TexConvMethod.Downscale2x });
			} else if (name == "rootR.cpk/mnu/tex/shop.tex") {
				convs.Add(new TexConvRules() { WTexId = 1, UTexId = 1, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 2, UTexId = 2, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 3, UTexId = 3, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 4, UTexId = 4, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 5, UTexId = 5, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 6, UTexId = 6, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 7, UTexId = 7, Method = TexConvMethod.Downscale2x });
			} else if (name == "rootR.cpk/mnu/tex/skill.tex") {
				convs.Add(new TexConvRules() { WTexId = 0, UTexId = 0, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 1, UTexId = 1, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 2, UTexId = 2, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 3, UTexId = 3, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 4, UTexId = 4, Method = TexConvMethod.Downscale2x });
				convs.Add(new TexConvRules() { WTexId = 5, UTexId = 5, Method = TexConvMethod.Downscale2x });
			} else if (name == "rootR.cpk/mnu/tex/snd_test.tex") {
				convs.Add(new TexConvRules() { WTexId = 1, UTexId = 1, Method = TexConvMethod.Downscale2x });
			} else if (name == "rootR.cpk/SysSub/JA/TitleTexture.tex") {
				convs.Add(new TexConvRules() { WTexId = 1, UTexId = 1, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 2, UTexId = 4, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 3, UTexId = 6, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 4, UTexId = 8, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 6, UTexId = 12, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 7, UTexId = 14, Method = TexConvMethod.CropExpandCanvas });
				convs.Add(new TexConvRules() { WTexId = 8, UTexId = 16, Method = TexConvMethod.CropExpandCanvas });
			} else if (name.EndsWith(".CLG")) {
				convs.Add(new TexConvRules() { WTexId = 0, UTexId = 0, Method = TexConvMethod.DownscaleTwoThirds });
				convs.Add(new TexConvRules() { WTexId = 1, UTexId = 1, Method = TexConvMethod.Clear });
			}

			MemoryStream s = wstream.Duplicate().CopyToMemory();
			s.Position = 0;
			foreach (TexConvRules c in convs) {
				var wm = wtxm.TXMRegulars[c.WTexId];
				var um = utxm.TXMRegulars[c.UTexId];
				var wv = wtxv.textures.Where(x => x.TXM == wm).First();
				var uv = utxv.textures.Where(x => x.TXM == um).First();
				System.Drawing.Bitmap newImage = null;
				switch (c.Method) {
					case TexConvMethod.Downscale2x: {
						newImage = FontProcessing.DownscaleInteger(uv.GetBitmaps()[0], 2);
					}
					break;
					case TexConvMethod.DownscaleTwoThirds: {
						newImage = DownscaleTwoThirds(uv.GetBitmaps()[0]);
					}
					break;
					case TexConvMethod.CropExpandCanvas: {
						newImage = DoCropExpandCanvas(uv.GetBitmaps()[0], wm.Width, wm.Height);
					}
					break;
					case TexConvMethod.Clear: {
						newImage = new Bitmap(wv.GetBitmaps()[0]);
						for (int y = 0; y < newImage.Height; ++y) {
							for (int x = 0; x < newImage.Width; ++x) {
								newImage.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
							}
						}
					}
					break;
					case TexConvMethod.Delegate: {
						newImage = c.Delegate(wv.GetBitmaps()[0], uv.GetBitmaps()[0]);
					}
					break;
					default: {
						throw new Exception("don't know how to convert " + uv.TXM.Name);
					}
				}
				if (newImage != null) {
					HyoutaTools.Util.Assert(newImage.Width == wm.Width);
					HyoutaTools.Util.Assert(newImage.Height == wm.Height);
					if (wm.Format == HyoutaTools.Tales.Vesperia.Texture.TextureFormat.Indexed8Bits_RGB5A3) {
						ChopBitsRGB5A3(newImage);
						var palette = GeneratePalette256(newImage);

						s.Position = w.Files[1].Location.Value + wm.TxvLocation;
						foreach (var loc in new HyoutaTools.Textures.PixelOrderIterators.TiledPixelOrderIterator(newImage.Width, newImage.Height, 8, 4)) {
							int cval = 0;
							if (loc.X < newImage.Width && loc.Y < newImage.Height) {
								cval = palette.lookup[newImage.GetPixel(loc.X, loc.Y)];
							}
							s.WriteByte((byte)cval);
						}

						for (int ci = 0; ci < 256; ++ci) {
							ushort cval = 0;
							if (ci < palette.colors.Count) {
								cval = HyoutaTools.Textures.ColorFetchingIterators.ColorFetcherRGB5A3.ColorToRGB5A3(palette.colors[ci]);
							}
							s.WriteUInt16(cval.ToEndian(EndianUtils.Endianness.BigEndian));
						}
					} else if (wm.Format == HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeRGBA8) {
						s.Position = w.Files[1].Location.Value + wm.TxvLocation;
						byte[] tmpb = new byte[0x40];
						int tmpp = 0;
						foreach (var loc in new HyoutaTools.Textures.PixelOrderIterators.TiledPixelOrderIterator(newImage.Width, newImage.Height, 4, 4)) {
							Color col = newImage.GetPixel(loc.X, loc.Y);
							tmpb[tmpp * 2 + 0] = col.A;
							tmpb[tmpp * 2 + 1] = col.R;
							tmpb[tmpp * 2 + 0 + 0x20] = col.G;
							tmpb[tmpp * 2 + 1 + 0x20] = col.B;
							++tmpp;
							if (tmpp == 16) {
								tmpp = 0;
								s.Write(tmpb);
							}
						}
						if (tmpp != 0) {
							throw new Exception("Unexpected tile size for " + wm.Name);
						}
					} else {
						Console.WriteLine("don't know how to encode into " + wm.Format);
					}
				}
			}

			s.Position = 0;
			return s;
		}

		private static Bitmap DoCropExpandCanvas(Bitmap img, uint width, uint height) {
			int leftOffset = (int)((width - img.Width) / 2);
			int topOffset = (int)((height - img.Height) / 2);
			return CropExpandCanvas(img, leftOffset, topOffset, width, height);
		}

		private static Bitmap CropExpandCanvas(Bitmap img, int leftOffset, int topOffset, uint width, uint height) {
			Bitmap n = new Bitmap((int)width, (int)height);
			for (int y = 0; y < n.Height; ++y) {
				for (int x = 0; x < n.Width; ++x) {
					int chkx = x - leftOffset;
					int chky = y - topOffset;
					Color c;
					if (chkx >= 0 && chkx < img.Width && chky >= 0 && chky < img.Height) {
						c = img.GetPixel(chkx, chky);
					} else {
						c = Color.FromArgb(0, 0, 0, 0);
					}
					n.SetPixel(x, y, c);
				}
			}
			return n;
		}

		private static Bitmap DoKarutaHalfAndHalf(Bitmap wtex, Bitmap utex, uint wx, uint ux, uint uw) {
			Bitmap left = FontProcessing.Crop(wtex, 0, 0, (int)wx, wtex.Height);
			Bitmap right = DownscaleTwoThirds(FontProcessing.Crop(utex, (int)ux, 0, (int)uw, utex.Height));
			long diff = wtex.Width - (left.Width + right.Width);
			if (diff > 0) {
				Bitmap mid = new Bitmap((int)diff, wtex.Height);
				for (int y = 0; y < mid.Height; ++y) {
					for (int x = 0; x < mid.Width; ++x) {
						mid.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
					}
				}
				left = StitchHorizontal(left, mid);
			}
			return StitchHorizontal(left, right);
		}

		private static Bitmap DoKaruta13(Bitmap wtex, Bitmap utex) {
			// this is very messy but the dimensions just don't match between versions
			// we should probably just inject a differently sized texture instead, really...
			Bitmap cropped1 = FontProcessing.Crop(utex, 10, 4, utex.Width - 24, utex.Height - 5);
			Bitmap upscaled = FontProcessing.PointScale(cropped1, 6);
			Bitmap cropped2 = FontProcessing.Crop(upscaled, 0, 1, upscaled.Width, upscaled.Height - 1);
			Bitmap downscaled = FontProcessing.DownscaleInteger(cropped2, 11);
			return DoCropExpandCanvas(downscaled, (uint)wtex.Width, (uint)wtex.Height);
		}

		private static Bitmap DownscaleTwoThirds(Bitmap img) {
			if (((img.Width * 2) % 3) != 0 || ((img.Height * 2) % 3) != 0) {
				throw new Exception("Cannot cleanly two-thirds scale image!");
			}
			return FontProcessing.DownscaleInteger(FontProcessing.PointScale(img, 2), 3);
		}

		private static Bitmap StitchHorizontal(Bitmap a, Bitmap b) {
			int ah = a.Height;
			int bh = b.Height;
			int aw = a.Width;
			int bw = b.Width;
			if (ah != bh) {
				throw new Exception("Cannot horizontal stitch differing heights.");
			}
			Bitmap n = new Bitmap(aw + bw, ah);
			for (int y = 0; y < ah; ++y) {
				for (int x = 0; x < aw; ++x) {
					n.SetPixel(x, y, a.GetPixel(x, y));
				}
			}
			for (int y = 0; y < bh; ++y) {
				for (int x = 0; x < bw; ++x) {
					n.SetPixel(aw + x, y, b.GetPixel(x, y));
				}
			}
			return n;
		}

		public static Stream ProcessAreaNameTexture(FileFetcher _fc, string name, DuplicatableStream jstream, DuplicatableStream ustream) {
			DuplicatableStream wstream = _fc.GetFile(name, Version.W);
			FPS4 wani = new FPS4(wstream.Duplicate());
			FPS4 uani = new FPS4(new HyoutaTools.Tales.tlzc.TlzcDecompressor().Decompress(ustream.Duplicate()));
			var clgfileinfo = wani.Files.Where(x => x.FileName.EndsWith(".CLG")).First();
			string clgname = clgfileinfo.FileName;
			FPS4 waniclg = new FPS4(wani.GetChildByName(clgname).AsFile.DataStream);
			FPS4 uaniclg = new FPS4(uani.GetChildByName(clgname).AsFile.DataStream);
			DuplicatableStream wtexstream = waniclg.GetChildByIndex(1).AsFile.DataStream;
			DuplicatableStream utexstream = uaniclg.GetChildByIndex(1).AsFile.DataStream;

			Stream wtexstreammod = ProcessTexture(name + "/" + clgname, utexstream, wtexstream);

			long injectOffset = clgfileinfo.Location.Value + waniclg.Files[1].Location.Value;
			Stream wstreammod = wstream.CopyToMemory();
			wstreammod.Position = injectOffset;
			wtexstreammod.Position = 0;
			StreamUtils.CopyStream(wtexstreammod, wstreammod, wtexstreammod.Length);
			wstreammod.Position = 0;
			return wstreammod;
		}
	}
}
