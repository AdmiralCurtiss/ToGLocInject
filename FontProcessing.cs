using HyoutaPluginBase;
using HyoutaTools.Tales.Vesperia.FPS4;
using HyoutaTools.Tales.Vesperia.Texture;
using HyoutaUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ToGLocInject {
	internal class FontProcessing {
		private static int Value(Color col) {
			return col.A + col.R;
		}

		private static Color Posterize(Color col, HashSet<Color> colors) {
			List<(int value, Color color)> v = new List<(int value, Color color)>();
			foreach (Color c in colors) {
				v.Add((Value(c), c));
			}

			int dist = int.MaxValue;
			Color rv = col;
			int colv = Value(col);
			foreach (var cv in v) {
				int r = Math.Abs(colv - cv.value);
				if (r < dist) {
					dist = r;
					rv = cv.color;
				}
			}
			return rv;
		}

		private static int MeasureAlphaFromLeft(Bitmap img, int cutoff) {
			for (int x = 0; x < img.Width; ++x) {
				for (int y = 0; y < img.Height; ++y) {
					Color px = img.GetPixel(x, y);
					if (px.A >= cutoff) {
						return x;
					}
				}
			}
			return img.Width;
		}
		private static int MeasureAlphaFromRight(Bitmap img, int cutoff) {
			for (int x = 0; x < img.Width; ++x) {
				for (int y = 0; y < img.Height; ++y) {
					Color px = img.GetPixel(img.Width - x - 1, y);
					if (px.A >= cutoff) {
						return x;
					}
				}
			}
			return img.Width;
		}
		private static int MeasureAlphaFromTop(Bitmap img, int cutoff) {
			for (int y = 0; y < img.Height; ++y) {
				for (int x = 0; x < img.Width; ++x) {
					Color px = img.GetPixel(x, y);
					if (px.A >= cutoff) {
						return y;
					}
				}
			}
			return img.Height;
		}
		private static int MeasureAlphaFromBottom(Bitmap img, int cutoff) {
			for (int y = 0; y < img.Height; ++y) {
				for (int x = 0; x < img.Width; ++x) {
					Color px = img.GetPixel(x, img.Height - y - 1);
					if (px.A >= cutoff) {
						return y;
					}
				}
			}
			return img.Height;
		}

		public static Bitmap PointScale(Bitmap img, int factor) {
			return PointScaleAndCrop(img, factor, 0, 0, img.Width, img.Height);
		}

		public static Bitmap Crop(Bitmap img, int startx, int starty, int width, int height) {
			return PointScaleAndCrop(img, 1, startx, starty, width, height);
		}

		public static Bitmap PointScaleAndCrop(Bitmap img, int factor, int startx, int starty, int width, int height) {
			Bitmap s = new Bitmap(width * factor, height * factor);
			for (int y = 0; y < height; ++y) {
				for (int x = 0; x < width; ++x) {
					Color c = img.GetPixel(x + startx, y + starty);
					for (int iy = 0; iy < factor; ++iy) {
						for (int ix = 0; ix < factor; ++ix) {
							s.SetPixel(x * factor + ix, y * factor + iy, c);
						}
					}
				}
			}
			return s;
		}

		public static (DuplicatableStream metrics, DuplicatableStream texture, Dictionary<char, (int w1, int w2)> charToWidthMap) Run(FileFetcher _fc, Config config) {
			bool debug = config.DebugFontOutputPath != null;
			bool adjustMetrics = debug;

			DuplicatableStream metricsWiiStream = _fc.GetFile("rootR.cpk/sys/FontBinary2.bin", Version.W);
			DuplicatableStream textureWiiStream = _fc.GetFile("rootR.cpk/sys/FontTexture2.tex", Version.W);
			DuplicatableStream texturePs3Stream = _fc.GetFile("rootR.cpk/sys/FontTexture2.tex", Version.U);
			FPS4 metricsWiiFps4 = new FPS4(metricsWiiStream);
			DuplicatableStream metricsWiiData = metricsWiiFps4.GetChildByIndex(1).AsFile.DataStream;
			FPS4 textureWiiFps4 = new FPS4(textureWiiStream);
			FPS4 texturePs3Fps4 = new FPS4(texturePs3Stream);
			TXM textureWiiTxm = new TXM(textureWiiFps4.GetChildByIndex(0).AsFile.DataStream);
			TXV textureWiiTxv = new TXV(textureWiiTxm, textureWiiFps4.GetChildByIndex(1).AsFile.DataStream, false);
			TXM texturePs3Txm = new TXM(texturePs3Fps4.GetChildByIndex(0).AsFile.DataStream);
			TXV texturePs3Txv = new TXV(texturePs3Txm, texturePs3Fps4.GetChildByIndex(1).AsFile.DataStream, false);
			Bitmap bitmapWii = textureWiiTxv.textures[0].GetBitmaps()[0];
			Bitmap bitmapPs3 = texturePs3Txv.textures[0].GetBitmaps()[0];

			if (debug) {
				Directory.CreateDirectory(config.DebugFontOutputPath);
				bitmapWii.Save(Path.Combine(config.DebugFontOutputPath, "wii.png"));
				bitmapPs3.Save(Path.Combine(config.DebugFontOutputPath, "ps3.png"));
			}

			var img_wii = bitmapWii;
			var img_ps3 = bitmapPs3;
			const int tile_extent_in_image = 25;
			const int tile_extent_actual = 24;
			int tiles_x = (img_wii.Width + 1) / tile_extent_in_image;
			int tiles_y = (img_wii.Height + 1) / tile_extent_in_image;
			const int ps3_tile_extent_in_image = 37;
			const int ps3_tile_extent_actual = 36;
			int ps3_tiles_x = (img_ps3.Width + 1) / ps3_tile_extent_in_image;
			int ps3_tiles_y = (img_ps3.Height + 1) / ps3_tile_extent_in_image;

			// split into individual tiles and extract source colors
			HashSet<Color> colors = new HashSet<Color>();
			List<Bitmap> tiles_wii = new List<Bitmap>();
			List<Bitmap> tiles_ps3 = new List<Bitmap>();
			for (int ty = 0; ty < tiles_y; ++ty) {
				for (int tx = 0; tx < tiles_x; ++tx) {
					var bmp = new Bitmap(tile_extent_actual, tile_extent_actual);
					for (int y = 0; y < tile_extent_actual; ++y) {
						for (int x = 0; x < tile_extent_actual; ++x) {
							var px = img_wii.GetPixel(tx * tile_extent_in_image + x, ty * tile_extent_in_image + y);
							colors.Add(px);
							bmp.SetPixel(x, y, px);
						}
					}
					tiles_wii.Add(bmp);
				}
			}
			for (int ty = 0; ty < ps3_tiles_y; ++ty) {
				for (int tx = 0; tx < ps3_tiles_x; ++tx) {
					var bmp = new Bitmap(ps3_tile_extent_actual, ps3_tile_extent_actual);
					for (int y = 0; y < ps3_tile_extent_actual; ++y) {
						for (int x = 0; x < ps3_tile_extent_actual; ++x) {
							var px = img_ps3.GetPixel(tx * ps3_tile_extent_in_image + x, ty * ps3_tile_extent_in_image + y);
							bmp.SetPixel(x, y, px);
						}
					}
					tiles_ps3.Add(bmp);
				}
			}

			// inject ps3 tiles over wii tiles
			List<(int where, int ps3where, string chars)> charsets = new List<(int where, int ps3where, string chars)>();
			charsets.Add((0, 0, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"));
			charsets.Add((243, 74, ",."));
			charsets.Add((246, 77, ":;?!_"));
			charsets.Add((254, 83, "/\\~|…"));
			charsets.Add((260, 118, "‘"));
			charsets.Add((261, 118, "’"));
			charsets.Add((262, 119, "“"));
			charsets.Add((263, 119, "”"));
			charsets.Add((264, 93, "()[]{}"));
			charsets.Add((276, 105, "+"));
			charsets.Add((277, 82, "-")); // copy the dash minus instead of the math minus, looks better in text flow
			charsets.Add((278, 107, "±×÷=≠<>≤≥"));
			charsets.Add((289, 118, "'\""));
			charsets.Add((293, 122, "$%#&*@"));

			Dictionary<char, (int w1, int w2)> charToWidthMap = new Dictionary<char, (int w1, int w2)>();

			byte[] metrics = new byte[metricsWiiData.Length];
			metricsWiiData.Read(metrics, 0, metrics.Length);

			foreach (var charset in charsets) {
				int where = charset.where;
				int ps3where = charset.ps3where;
				foreach (char ch in charset.chars) {
					var wiitile = tiles_wii[where];
					var averagescaled = DownscaleTileFromPs3ToWiiWithUnweightedAverageScaling(tiles_ps3[ps3where]);
					//var downscaled = DownscaleTileFromPs3ToWiiWithWeightedScaling(tiles_ps3[ps3where]);
					var downscaled = averagescaled;
					PosterizeImage(wiitile, downscaled, colors, tile_extent_actual);
					PosterizeImage(averagescaled, averagescaled, colors, tile_extent_actual);
					if (debug) {
						wiitile.Save(Path.Combine(config.DebugFontOutputPath, string.Format("wii_new_{0:D4}.png", where)));
					}

					int cutoff_1 = 180;
					int cutoff_2 = 220;
					int leftwhere = where * 8 + 0;
					int rightwhere = where * 8 + 1;
					int leftwhere2 = where * 8 + 4;
					int rightwhere2 = where * 8 + 5;

					// forcing vertical extents to be the same for all, because text looks *really weird* in english if lines have different heights
					// for digits, forcing horizontal extents to be the same as well so they look nice in vertical lists
					bool isDigit = ch == '0' || ch == '1' || ch == '2' || ch == '3' || ch == '4' || ch == '5' || ch == '6' || ch == '7' || ch == '8' || ch == '9';
					if (isDigit) {
						metrics[leftwhere] = ch == '1' ? (byte)(6) : ch == '2' ? (byte)(6) : (byte)(7);
						metrics[rightwhere] = ch == '1' ? (byte)(8) : ch == '2' ? (byte)(8) : (byte)(7);
						//metrics[leftwhere2] = ch == '1' ? (byte)(7) : ch == '2' ? (byte)(7) : (byte)(8);
						//metrics[rightwhere2] = ch == '1' ? (byte)(9) : ch == '2' ? (byte)(9) : (byte)(8);
					} else {
						metrics[leftwhere] = (byte)MeasureAlphaFromLeft(averagescaled, cutoff_1);
						metrics[rightwhere] = (byte)MeasureAlphaFromRight(averagescaled, cutoff_1);
						//metrics[leftwhere2] = (byte)MeasureAlphaFromLeft(averagescaled, cutoff_2);
						//metrics[rightwhere2] = (byte)MeasureAlphaFromRight(averagescaled, cutoff_2);
					}

					switch (ch) {
						case 'j':
						case ';':
							metrics[leftwhere] += 1;
							break;
						case 'A':
						case 'P':
						case 'Q':
						case 'T':
						case 'Y':
						case 'W':
						case 'f':
						case 's':
						case 'w':
						case 'y':
						case '[':
							metrics[rightwhere] += 1;
							break;
						case 't':
							metrics[leftwhere] += 1;
							metrics[rightwhere] += 1;
							break;
						default:
							break;
					}

					metrics[leftwhere2] = metrics[leftwhere];
					metrics[rightwhere2] = metrics[rightwhere];

					switch (ch) {
						case '.':
						case ',':
							metrics[leftwhere2] += 1;
							metrics[rightwhere] += 1;
							break;
						default:
							break;
					}

					metrics[where * 8 + 2] = 0; //(byte)MeasureAlphaFromTop(test, cutoff_1);
					metrics[where * 8 + 3] = 4; //(byte)MeasureAlphaFromBottom(test, cutoff_1);
					metrics[where * 8 + 6] = 0; //(byte)MeasureAlphaFromTop(test, cutoff_2);
					metrics[where * 8 + 7] = 4; //(byte)MeasureAlphaFromBottom(test, cutoff_2);

					int width1 = tile_extent_actual - (metrics[leftwhere] + metrics[rightwhere]);
					int width2 = tile_extent_actual - (metrics[leftwhere2] + metrics[rightwhere2]);
					charToWidthMap.Add(ch, (width1, width2));

					++where;
					++ps3where;
				}
			}

			// manually generate good metrics for space; see also code patches in main.dol
			metrics[0x6F70] = 10;
			metrics[0x6F71] = 10;
			metrics[0x6F72] = 0;
			metrics[0x6F73] = 4;
			metrics[0x6F74] = 10;
			metrics[0x6F75] = 10;
			metrics[0x6F76] = 0;
			metrics[0x6F77] = 4;
			charToWidthMap.Add(' ', (tile_extent_actual - 20, tile_extent_actual - 20));

			// write out visual representation of font metrics for adjustments
			if (adjustMetrics) {
				foreach (var charset in charsets) {
					int where = charset.where;
					foreach (char ch in charset.chars) {
						int factor = 10;
						var test = PointScale(tiles_wii[where], factor);

						for (int metricsset = 0; metricsset < 2; ++metricsset) {
							Color col = metricsset == 0 ? Color.Red : Color.Yellow;
							int xl = (metrics[where * 8 + metricsset * 4] - 1) * factor + factor - 1;
							int xr = (tiles_wii[where].Width - metrics[where * 8 + metricsset * 4 + 1]) * factor;
							int yt = (metrics[where * 8 + metricsset * 4 + 2] - 1) * factor + factor - 1;
							int yb = (tiles_wii[where].Width - metrics[where * 8 + metricsset * 4 + 3]) * factor;
							for (int y = 0; y < test.Height; ++y) {
								if (xl >= 0 && xl < test.Width) {
									test.SetPixel(xl, y, col);
								}
								if (xr >= 0 && xr < test.Width) {
									test.SetPixel(xr, y, col);
								}
							}
							for (int x = 0; x < test.Width; ++x) {
								if (yt >= 0 && yt < test.Height) {
									test.SetPixel(x, yt, col);
								}
								if (yb >= 0 && yb < test.Height) {
									test.SetPixel(x, yb, col);
								}
							}
						}

						PointScale(test, 3).Save(Path.Combine(config.DebugFontOutputPath, string.Format("metrics_view_{0:D4}.png", where)));

						++where;
					}
				}
			}

			// join indvidiual tiles back into full texture
			int idx = 0;
			for (int ty = 0; ty < tiles_y; ++ty) {
				for (int tx = 0; tx < tiles_x; ++tx) {
					var bmp = tiles_wii[idx];
					for (int y = 0; y < tile_extent_actual; ++y) {
						for (int x = 0; x < tile_extent_actual; ++x) {
							var px = bmp.GetPixel(x, y);
							img_wii.SetPixel(tx * tile_extent_in_image + x, ty * tile_extent_in_image + y, px);
						}
					}
					++idx;
				}
			}

			if (debug) {
				img_wii.Save(Path.Combine(config.DebugFontOutputPath, "wii_new.png"));
			}

			// inject metrics
			DuplicatableStream outputMetricsStream;
			{
				Stream stream = metricsWiiStream.Duplicate().CopyToMemory();
				stream.Position = 0x43E0;
				stream.Write(metrics);

				stream.Position = 0;
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				outputMetricsStream = new HyoutaUtils.Streams.DuplicatableByteArrayStream(data);
			}

			// encode texture
			DuplicatableStream outputTextureStream;
			{
				Stream stream = textureWiiStream.Duplicate().CopyToMemory();
				stream.Position = 0x80100;
				List<(int idx, ushort v)> stuff = new List<(int idx, ushort v)>();
				for (int i = 0; i < 16; ++i) {
					stuff.Add((i, stream.ReadUInt16().FromEndian(EndianUtils.Endianness.BigEndian)));
				}

				stream.Position = 0x100;
				var pxit = new HyoutaTools.Textures.PixelOrderIterators.TiledPixelOrderIterator(img_wii.Width, img_wii.Height, 8, 8);
				byte storage = 0;
				bool even = false;
				foreach (var px in pxit) {
					if (px.X < img_wii.Width && px.Y < img_wii.Height) {
						Color col = img_wii.GetPixel(px.X, px.Y);
						ushort value = HyoutaTools.Textures.ColorFetchingIterators.ColorFetcherGrey8Alpha8.ColorToGrey8Alpha8(col);
						var colidx = stuff.First(x => x.v == value).idx;
						if (!even) {
							storage = (byte)colidx;
						} else {
							storage = (byte)(storage << 4 | (byte)colidx);
							stream.WriteByte(storage);
						}
						even = !even;
					}
				}

				stream.Position = 0;
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				outputTextureStream = new HyoutaUtils.Streams.DuplicatableByteArrayStream(data);
			}

			return (outputMetricsStream, outputTextureStream, charToWidthMap);
		}

		public static Bitmap DownscaleInteger(Bitmap source, int factor) {
			int divfac = factor * factor;
			int shift = divfac / 2;
			int sourceHeight = source.Height;
			int sourceWidth = source.Width;
			int targetHeight = sourceHeight / factor;
			int targetWidth = sourceWidth / factor;
			HyoutaTools.Util.Assert(targetHeight * factor == sourceHeight);
			HyoutaTools.Util.Assert(targetWidth * factor == sourceWidth);

			var target = new Bitmap(targetWidth, targetHeight);

			for (int ty = 0; ty < targetHeight; ++ty) {
				for (int tx = 0; tx < targetWidth; ++tx) {
					int rsum = 0;
					int gsum = 0;
					int bsum = 0;
					int asum = 0;
					for (int sy = ty * factor; sy < ty * factor + factor; ++sy) {
						for (int sx = tx * factor; sx < tx * factor + factor; ++sx) {
							Color c = source.GetPixel(sx, sy);
							rsum += c.R;
							gsum += c.G;
							bsum += c.B;
							asum += c.A;
						}
					}
					rsum = (rsum + shift) / divfac;
					gsum = (gsum + shift) / divfac;
					bsum = (bsum + shift) / divfac;
					asum = (asum + shift) / divfac;
					target.SetPixel(tx, ty, Color.FromArgb(asum, rsum, gsum, bsum));
				}
			}

			return target;
		}

		public static Bitmap Downscale5Weighted(Bitmap source) {
			int factor = 5;
			int w0 = 1;
			int w1 = 4;
			int w2 = 9;
			int w3 = 16;
			int w4 = 25;
			int[] weights = new int[] {
				w0, w1, w2, w1, w0,
				w1, w2, w3, w2, w1,
				w2, w3, w4, w3, w2,
				w1, w2, w3, w2, w1,
				w0, w1, w2, w1, w0,
			};
			int divfac = weights.Sum();
			int shift = divfac / 2;
			int sourceHeight = source.Height;
			int sourceWidth = source.Width;
			int targetHeight = sourceHeight / factor;
			int targetWidth = sourceWidth / factor;
			HyoutaTools.Util.Assert(targetHeight * factor == sourceHeight);
			HyoutaTools.Util.Assert(targetWidth * factor == sourceWidth);

			var target = new Bitmap(targetWidth, targetHeight);

			for (int ty = 0; ty < targetHeight; ++ty) {
				for (int tx = 0; tx < targetWidth; ++tx) {
					int rsum = 0;
					int gsum = 0;
					int bsum = 0;
					int asum = 0;
					for (int sy = 0; sy < factor; ++sy) {
						for (int sx = 0; sx < factor; ++sx) {
							Color c = source.GetPixel(tx * factor + sx, ty * factor + sy);
							int w = weights[sy * factor + sx];
							rsum += c.R * w;
							gsum += c.G * w;
							bsum += c.B * w;
							asum += c.A * w;
						}
					}
					rsum = (rsum + shift) / divfac;
					gsum = (gsum + shift) / divfac;
					bsum = (bsum + shift) / divfac;
					asum = (asum + shift) / divfac;
					target.SetPixel(tx, ty, Color.FromArgb(asum, rsum, gsum, bsum));
				}
			}

			return target;
		}

		private static Bitmap DownscaleTileFromPs3ToWiiWithWeightedScaling(Bitmap bitmap) {
			HyoutaTools.Util.Assert(bitmap.Width == 36 && bitmap.Height == 36);
			Bitmap enlarged = PointScaleAndCrop(bitmap, 4, 3, 6, 30, 30);
			HyoutaTools.Util.Assert(enlarged.Width == 120 && enlarged.Height == 120);
			Bitmap downscaled = Downscale5Weighted(enlarged);
			HyoutaTools.Util.Assert(downscaled.Width == 24 && downscaled.Height == 24);
			return downscaled;
		}

		private static Bitmap DownscaleTileFromPs3ToWiiWithUnweightedAverageScaling(Bitmap bitmap) {
			HyoutaTools.Util.Assert(bitmap.Width == 36 && bitmap.Height == 36);
			Bitmap enlarged = PointScaleAndCrop(bitmap, 4, 3, 6, 30, 30);
			HyoutaTools.Util.Assert(enlarged.Width == 120 && enlarged.Height == 120);
			Bitmap downscaled = DownscaleInteger(enlarged, 5);
			HyoutaTools.Util.Assert(downscaled.Width == 24 && downscaled.Height == 24);
			return downscaled;
		}

		private static Bitmap PosterizeImage(Bitmap test, Bitmap loaded, HashSet<Color> colors, int extent) {
			for (int y = 0; y < extent; ++y) {
				for (int x = 0; x < extent; ++x) {
					test.SetPixel(x, y, Posterize(loaded.GetPixel(x, y), colors));
				}
			}
			return test;
		}
	}
}
