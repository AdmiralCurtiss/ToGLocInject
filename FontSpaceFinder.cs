using HyoutaPluginBase;
using HyoutaUtils;
using HyoutaUtils.Streams;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// this game stores a lot of its menu/system strings in its main executable
// that means we need to find extra space to put the much longer english text
//
// the font texture appears to be always loaded into memory at a fixed address
// and has ~45 KB of unused space, so we can abuse that and put the text that
// doesn't fit into the executable in there

// FIXME: this works fine in dolphin but fails on a real console, texture memory location seems to be not deterministic...

namespace HyoutaTools.Tales.Graces.TranslationPort {
	internal static class FontSpaceFinder {
		private enum TileIdentification {
			UsedTile,
			UnusedTile,
		}

		private static bool IsPixelUsed(int x, int y) {
			if (!IsPixelInBounds(x, y)) {
				return false;
			}
			int tilex = x / 25;
			int intilex = x % 25;
			int tiley = y / 25;
			int intiley = y % 25;
			bool isBorder = intilex == 24 || intiley == 24;
			bool isSpaceX = tilex == 40;
			bool isSpaceY = tiley == 4;
			bool leftOfSpace = tilex == 39 && intilex == 24;
			bool upOfSpace = tiley == 3 && intiley == 24;
			if (isBorder && ((leftOfSpace && upOfSpace) || (leftOfSpace && isSpaceY) || (upOfSpace && isSpaceX))) {
				return true;
			}
			if (tiley == 0) {
				return true;
			}
			if (tiley == 1 && tilex < 13) {
				return true;
			}
			if (tilex == 40 && tiley == 4) {
				return true;
			}
			return false;
		}
		private static bool IsPixelInBounds(int x, int y) {
			return x >= 0 && x < 1024 && y >= 0 && y < 128;
		}

		private static TileIdentification IdentifyPixel(int x, int y) {
			int adjacentSafetyPixels = 4;
			if (IsPixelUsed(x, y)) {
				return TileIdentification.UsedTile;
			}
			for (int ax = 1; ax <= adjacentSafetyPixels; ++ax) {
				for (int ay = 1; ay <= adjacentSafetyPixels; ++ay) {
					if (IsPixelUsed(x - ax, y - ax) || IsPixelUsed(x - ax, y + ax) || IsPixelUsed(x + ax, y - ax) || IsPixelUsed(x + ax, y + ax)) {
						return TileIdentification.UsedTile;
					}
				}
			}
			return TileIdentification.UnusedTile;
		}

		public static List<MemChunk> FindFreeMemoryInFontTexture(MemoryStream fontStream) {
			DuplicatableStream textureWiiStream = new DuplicatableByteArrayStream(fontStream.CopyToByteArray());
			HyoutaTools.Tales.Vesperia.FPS4.FPS4 textureWiiFps4 = new HyoutaTools.Tales.Vesperia.FPS4.FPS4(textureWiiStream);
			HyoutaTools.Tales.Vesperia.Texture.TXM textureWiiTxm = new HyoutaTools.Tales.Vesperia.Texture.TXM(textureWiiFps4.GetChildByIndex(0).AsFile.DataStream);
			HyoutaTools.Tales.Vesperia.Texture.TXV textureWiiTxv = new HyoutaTools.Tales.Vesperia.Texture.TXV(textureWiiTxm, textureWiiFps4.GetChildByIndex(1).AsFile.DataStream, false);
			Bitmap bitmapWii = textureWiiTxv.textures[2].GetBitmaps()[0];
			{
				for (int y = 0; y < bitmapWii.Height; ++y) {
					for (int x = 0; x < bitmapWii.Width; ++x) {
						Color color;
						switch (IdentifyPixel(x, y)) {
							case TileIdentification.UsedTile:
								color = Color.FromArgb(0, 255, 0);
								break;
							case TileIdentification.UnusedTile:
								color = Color.FromArgb(0, 0, 255);
								break;
							default:
								throw new Exception("???");
						}
						bitmapWii.SetPixel(x, y, color);
					}
				}
			}

			var pxit = new HyoutaTools.Textures.PixelOrderIterators.TiledPixelOrderIterator(bitmapWii.Width, bitmapWii.Height, 8, 8);
			MemoryStream stream = new MemoryStream();
			byte storage = 0;
			bool even = false;
			foreach (var px in pxit) {
				if (px.X < bitmapWii.Width && px.Y < bitmapWii.Height) {
					Color col = bitmapWii.GetPixel(px.X, px.Y);
					bool pixelUnused = col.B > 0;
					var colidx = pixelUnused ? 0xF : 0x0;
					if (!even) {
						storage = (byte)colidx;
					} else {
						storage = (byte)(storage << 4 | (byte)colidx);
						stream.WriteByte(storage == 0xFF ? (byte)1 : (byte)0);
					}
					even = !even;
				}
			}

			List<MemChunk> chunks = new List<MemChunk>();
			uint offset = textureWiiFps4.Files[1].Location.Value + textureWiiTxv.textures[2].TXM.TxvLocation;
			long len = stream.Length;
			long startOfLastSafeBlock = -1;
			var fontMapper = new FontMapper();
			stream.Position = 0;
			for (long i = 0; i <= len; ++i) {
				bool safeToWriteTo = i == len ? false : stream.ReadUInt8() == 1;
				if (safeToWriteTo && startOfLastSafeBlock == -1) {
					// start of block
					startOfLastSafeBlock = i;
				} else if (!safeToWriteTo && startOfLastSafeBlock != -1) {
					// end of block
					long start = startOfLastSafeBlock;
					long length = i - startOfLastSafeBlock;
					MemChunk mc = new MemChunk();
					mc.Address = (uint)(start + offset);
					mc.FreeBytes = (uint)length;
					mc.File = fontStream;
					mc.Mapper = fontMapper;
					mc.IsInternal = false;
					chunks.Add(mc);
					startOfLastSafeBlock = -1;
				}
			}

			return chunks;
		}
	}

	internal class FontMapper : HyoutaTools.Generic.IRomMapper {
		public bool TryMapRamToRom(uint ramAddress, out uint value) {
			throw new NotImplementedException();
		}

		public bool TryMapRomToRam(uint romAddress, out uint value) {
			value = 0x908A36E0 + romAddress;
			return true;
		}
	}
}
