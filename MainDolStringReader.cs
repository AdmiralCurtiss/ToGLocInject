using HyoutaPluginBase;
using HyoutaTools.Generic;
using HyoutaUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HyoutaTools.Tales.Graces.TranslationPort {
	internal static class MainDolStringReader {
		public static List<MainDolString> ReadElfStringsUs(DuplicatableStream filestream) {
			IRomMapper dol = new Ps3ElfMapper();
			var stream = filestream.CopyToMemory();
			var data = new List<MainDolString>();
			ReadStringsInBlocks(data, dol, stream, 0x841FA4, 0x0A00, 0x14, 1, true); // battle field names?
			ReadStringsInBlocks(data, dol, stream, 0x8429A8, 0x0244, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x842C2C, 0x006C, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x84421C, 0x0F60, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x845198, 0x151C, 0x1C, 4, true); // equipment effects
			ReadStringsInBlocks(data, dol, stream, 0x8467B0, 0x0074, 0x04, 1, true); // attack types
			ReadStringsInBlocks(data, dol, stream, 0x846824, 0x0200, 0x08, 1, true); // cooking effects
			ReadStringsInBlocks(data, dol, stream, 0x846A1C, 0x044C, 0x14, 2, true); // item traits
			ReadStringsInBlocks(data, dol, stream, 0x846E70, 0x004C, 0x04, 1, true); // gem names
			ReadStringsInBlocks(data, dol, stream, 0x846ECC, 0x00C0, 0x10, 4, true); // tutorials short
			ReadStringsInBlocks(data, dol, stream, 0x846F8C, 0x0EA0, 0x20, 7, true); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x847E2C, 0x0594, 0x1C, 7, true); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x8483C0, 0x01F0, 0x08, 1, true); // books
			{
				// US ver has this as three entries per synopsis, JP ver has just one, so to match this properly merge the US entries together
				var tmp = new List<MainDolString>();
				ReadStringsInBlocks(tmp, dol, stream, 0x8485B0, 0x021C, 0x0C, 3, true); // books [synopsis]
				for (int i = 0; i < tmp.Count; i += 3) {
					StringBuilder sb = new StringBuilder();
					sb.Append(tmp[i].Text);
					for (int j = 1; j < 3; ++j) {
						if (!string.IsNullOrWhiteSpace(tmp[i + j].Text)) {
							sb.Append('\n');
							sb.Append(tmp[i + j].Text);
						}
					}
					data.Add(new MainDolString(0, 0, sb.ToString(), 0));
				}
			}
			ReadStringsInBlocks(data, dol, stream, 0x8487CC, 0x0BB8, 0x0C, 1, true); // books [sidequests]
			ReadStringsInBlocks(data, dol, stream, 0x849384, 0x094C, 0x1C, 2, true); // discoveries
			ReadStringsInBlocks(data, dol, stream, 0x849CD4, 0x0520, 0x04, 1, true); // enemy descriptions
			ReadStringsInBlocks(data, dol, stream, 0x84B538, 0x1464, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84C99C, 0x2154, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84EAF0, 0x02F4, 0x1C, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84EDE4, 0x13D8, 0x28, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x8501BC, 0x14AC, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x851668, 0x071C, 0x1C, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x851D84, 0x1770, 0x18, 2, true); // valuable items
			ReadStringsInBlocks(data, dol, stream, 0x8534F4, 0x024C, 0x0C, 2, true); // grade shop
			ReadStringsInBlocks(data, dol, stream, 0x85377C, 0x2370, 0x24, 3, true); // requests
			ReadStringsInBlocks(data, dol, stream, 0x855B30, 0x6670, 0x58, 3, true); // artes [technically has an extra string at the end for the A-Arte Tree but we don't care about it]
			ReadStringsInBlocks(data, dol, stream, 0x85C250, 0x0078, 0x0C, 2, true); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x85C2C8, 0x00A0, 0x08, 2, true); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x879D38, 0x0270, 0x0C, 2, true); // title effects (?)
			ReadStringsInBlocks(data, dol, stream, 0x85C3D8, 0x1D960, 0x70, 2, true); // titles
			return data;

			//ReadStringsInBlocks( data, dol, stream, 0x5624E8, 0x0258, 0x14, 1 ); // debug strings
			//ReadStringsInBlocks( data, dol, stream, 0x562740, 0x0154, 0x04, 1 ); // debug strings
			//ReadStringsInBlocks( data, dol, stream, 0x570DAC, 0x0040, 0x04, 1 ); // menu strings // seem to no longer exist?
		}

		public static List<MainDolString> ReadElfStringsJp(DuplicatableStream filestream) {
			IRomMapper dol = new Ps3ElfMapper();
			var stream = filestream.CopyToMemory();
			var data = new List<MainDolString>();
			ReadStringsInBlocks(data, dol, stream, 0x842DF4, 0x0A00, 0x14, 1, true); // battle field names?
			ReadStringsInBlocks(data, dol, stream, 0x8437F8, 0x0244, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x843A7C, 0x006C, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x845068, 0x083C, 0x04, 1, true); // menu strings
			for (int i = 0; i < 4; ++i) {
				data.Add(new MainDolString(0, 0, null, 0)); // JP and US desync here because of extra strings in US, compensate
			}
			ReadStringsInBlocks(data, dol, stream, 0x8458A4, 0x0714, 0x04, 1, true); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x845FD4, 0x151C, 0x1C, 4, true); // equipment effects
			ReadStringsInBlocks(data, dol, stream, 0x8475EC, 0x0074, 0x04, 1, true); // attack types
			ReadStringsInBlocks(data, dol, stream, 0x847660, 0x0200, 0x08, 1, true); // cooking effects
			ReadStringsInBlocks(data, dol, stream, 0x847858, 0x044C, 0x14, 2, true); // item traits
			ReadStringsInBlocks(data, dol, stream, 0x847CAC, 0x004C, 0x04, 1, true); // gem names
			ReadStringsInBlocks(data, dol, stream, 0x847D08, 0x00C0, 0x10, 4, true); // tutorials short
			ReadStringsInBlocks(data, dol, stream, 0x847DC8, 0x0EA0, 0x20, 7, true); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x848C68, 0x0594, 0x1C, 7, true); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x8491FC, 0x01F0, 0x08, 1, true); // books
			ReadStringsInBlocks(data, dol, stream, 0x8493EC, 0x00B4, 0x04, 1, true); // books [synopsis]
			ReadStringsInBlocks(data, dol, stream, 0x8494A0, 0x0BB8, 0x0C, 1, true); // books [sidequests]
			ReadStringsInBlocks(data, dol, stream, 0x84A058, 0x094C, 0x1C, 2, true); // discoveries
			ReadStringsInBlocks(data, dol, stream, 0x84A9A8, 0x0520, 0x04, 1, true); // enemy descriptions
			ReadStringsInBlocks(data, dol, stream, 0x84C20C, 0x1464, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84D670, 0x2154, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84F7C4, 0x02F4, 0x1C, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x84FAB8, 0x13D8, 0x28, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x850E90, 0x14AC, 0x24, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x85233C, 0x071C, 0x1C, 2, true); // items
			ReadStringsInBlocks(data, dol, stream, 0x852A58, 0x1770, 0x18, 2, true); // valuable items
			ReadStringsInBlocks(data, dol, stream, 0x8541C8, 0x024C, 0x0C, 2, true); // grade shop
			ReadStringsInBlocks(data, dol, stream, 0x854450, 0x2370, 0x24, 3, true); // requests
			ReadStringsInBlocks(data, dol, stream, 0x856800, 0x6670, 0x58, 3, true); // artes
			ReadStringsInBlocks(data, dol, stream, 0x85CF20, 0x0078, 0x0C, 2, true); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x85CF98, 0x00A0, 0x08, 2, true); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x87AA08, 0x0270, 0x0C, 2, true); // title effects (?)
			ReadStringsInBlocks(data, dol, stream, 0x85D0A8, 0x1D960, 0x70, 2, true); // titles
			return data;

			//ReadStringsInBlocks( data, dol, stream, 0x5624E8, 0x0258, 0x14, 1 ); // debug strings
			//ReadStringsInBlocks( data, dol, stream, 0x562740, 0x0154, 0x04, 1 ); // debug strings
			//ReadStringsInBlocks( data, dol, stream, 0x570DAC, 0x0040, 0x04, 1 ); // menu strings // seem to no longer exist?
		}

		public static List<MainDolString> ReadDolStrings(DuplicatableStream filestream) {
			var dol = new GameCube.Dol(filestream);
			var stream = filestream.CopyToMemory();
			var data = new List<MainDolString>();
			ReadStringsInBlocks(data, dol, stream, 0x535A68, 0x0024, 0x04, 1, false); // wii home archive references
			ReadStringsInBlocks(data, dol, stream, 0x576224, 0x1044, 0x04, 1, false); // item icon strings...?
			ReadStringsInBlocks(data, dol, stream, 0x537480, 0x0054, 0x04, 1, false); // wii error messages
			ReadStringsInBlocks(data, dol, stream, 0x5D5E08, 0x0070, 0x04, 1, false); // wii error messages??
			ReadStringsInBlocks(data, dol, stream, 0x60CF58, 0x0008, 0x04, 1, false); // wii error messages
			ReadStringsInBlocks(data, dol, stream, 0x5605E4, 0x0A00, 0x14, 1, false); // battle field names?
			ReadStringsInBlocks(data, dol, stream, 0x56106C, 0x0210, 0x04, 1, false); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x5612BC, 0x0028, 0x04, 1, false); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x5624E8, 0x0258, 0x14, 1, false); // debug strings
			ReadStringsInBlocks(data, dol, stream, 0x562740, 0x0154, 0x04, 1, false); // debug strings
			ReadStringsInBlocks(data, dol, stream, 0x56FEA4, 0x0EBC, 0x04, 1, false); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x570DAC, 0x0040, 0x04, 1, false); // menu strings
			ReadStringsInBlocks(data, dol, stream, 0x570F94, 0x14AC, 0x1C, 4, false); // equipment effects
			ReadStringsInBlocks(data, dol, stream, 0x572440, 0x0074, 0x04, 1, false); // attack types
			ReadStringsInBlocks(data, dol, stream, 0x5724B8, 0x01F8, 0x08, 1, false); // cooking effects
			ReadStringsInBlocks(data, dol, stream, 0x5726E4, 0x044C, 0x14, 2, false); // item traits
			ReadStringsInBlocks(data, dol, stream, 0x572E38, 0x0044, 0x04, 1, false); // gem names
			ReadStringsInBlocks(data, dol, stream, 0x572EE8, 0x0090, 0x10, 4, false); // tutorials short
			ReadStringsInBlocks(data, dol, stream, 0x572F78, 0x0B80, 0x20, 7, false); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x573AF8, 0x0604, 0x1C, 7, false); // tutorials long
			ReadStringsInBlocks(data, dol, stream, 0x574100, 0x01A8, 0x08, 1, false); // books
			ReadStringsInBlocks(data, dol, stream, 0x5742A8, 0x008C, 0x04, 1, false); // books [synopsis]
			ReadStringsInBlocks(data, dol, stream, 0x574338, 0x0A38, 0x0C, 1, false); // books [sidequests]
			ReadStringsInBlocks(data, dol, stream, 0x574FA8, 0x0888, 0x1C, 2, false); // discoveries
			ReadStringsInBlocks(data, dol, stream, 0x575844, 0x0444, 0x04, 1, false); // enemy descriptions
			ReadStringsInBlocks(data, dol, stream, 0x577268, 0x1464, 0x24, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x5786D0, 0x1E18, 0x24, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x57A504, 0x02D8, 0x1C, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x57A808, 0x1158, 0x28, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x57B984, 0x13B0, 0x24, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x57CD50, 0x02D8, 0x1C, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x57D040, 0x1308, 0x18, 2, false); // items
			ReadStringsInBlocks(data, dol, stream, 0x585C98, 0x0240, 0x0C, 2, false); // grade shop
			ReadStringsInBlocks(data, dol, stream, 0x585F14, 0x2370, 0x24, 3, false); // requests
			ReadStringsInBlocks(data, dol, stream, 0x5884A8, 0x55B0, 0x50, 3, false); // artes
			ReadStringsInBlocks(data, dol, stream, 0x58DA58, 0x0078, 0x0C, 2, false); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x58DAD0, 0x0078, 0x08, 2, false); // strategy
			ReadStringsInBlocks(data, dol, stream, 0x596B40, 0x0258, 0x0C, 2, false); // title effects (?)
			ReadStringsInBlocks(data, dol, stream, 0x58DB80, 0x8FC0, 0x28, 2, false); // titles

			//ReadStringsInBlocks(data, dol, stream, 0x56127C, 0x0040, 0x04, 1, false); // voiceline refs?
			//ReadStringsInBlocks(data, dol, stream, 0x535530, 0x0108, 0x04, 1, false); // letters...?
			//ReadStringsInBlocks(data, dol, stream, 0x5D5EE0, 0x0090, 0x18, 1, false); // title screen model refs?
			//ReadStringsInBlocks(data, dol, stream, 0x553654, 0x033C, 0x0C, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x553ab0, 0x000C, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x5562E0, 0x0018, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556304, 0x0208, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556514, 0x0444, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556964, 0x0200, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556B6C, 0x0120, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556C94, 0x0028, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x556CC4, 0x03A8, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x557074, 0x0070, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x5570EC, 0x0190, 0x04, 1, false); // disc file paths?
			//ReadStringsInBlocks(data, dol, stream, 0x557284, 0x00B8, 0x04, 1, false); // disc file paths?

			// sanity check: should have no duplicate rom pointer positions
			{
				HashSet<uint> a = new HashSet<uint>();
				foreach (var d in data) {
					if (a.Contains(d.RomPointerPosition)) {
						throw new Exception("duplicate strings loaded!");
					}
					a.Add(d.RomPointerPosition);
				}
			}
			return data;
		}

		public static void ReadStrings(List<MainDolString> output, IRomMapper dol, Stream stream, long start, long bytecount) {
			stream.Position = start;
			while (stream.Position < start + bytecount) {
				long p = stream.Position;
				uint a = stream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				if (a != 0) {
					uint rom;
					if (dol.TryMapRamToRom(a, out rom)) {
						long tmp = stream.Position;
						stream.Position = rom;
						string s = stream.ReadNulltermString(TextUtils.GameTextEncoding.ShiftJIS);
						uint bytesRead = (uint)(stream.Position - rom);
						stream.Position = tmp;
						output.Add(new MainDolString((uint)p, rom, s, bytesRead));
					}
				}
			}
		}

		private static void ReadStringsInBlocks(List<MainDolString> output, IRomMapper dol, Stream stream, long pos, int byteSizeTotal, int blockSize, int stringsPerBlock, bool keepInvalid) {
			stream.Position = pos;
			int count = byteSizeTotal / blockSize;
			long[] positions = new long[stringsPerBlock];
			uint[] ramAddresses = new uint[stringsPerBlock];
			uint[] romAddresses = new uint[stringsPerBlock];
			string[] strings = new string[stringsPerBlock];
			uint[] stringByteCounts = new uint[stringsPerBlock];
			for (int i = 0; i < count; ++i) {
				for (int j = 0; j < stringsPerBlock; ++j) {
					positions[j] = stream.Position;
					ramAddresses[j] = stream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				}
				stream.DiscardBytes((uint)(blockSize - 4 * stringsPerBlock));
				bool isValid = false;
				for (int j = 0; j < stringsPerBlock; ++j) {
					if (ramAddresses[j] == 0) {
						romAddresses[j] = 0;
						strings[j] = null;
						stringByteCounts[j] = 0;
					} else {
						romAddresses[j] = dol.MapRamToRom(ramAddresses[j]);

						long tmp = stream.Position;
						stream.Position = romAddresses[j];
						string s = stream.ReadNulltermString(TextUtils.GameTextEncoding.ShiftJIS);
						uint bytesRead = (uint)(stream.Position - romAddresses[j]);
						stream.Position = tmp;

						strings[j] = s;
						stringByteCounts[j] = bytesRead;
						isValid = true;
					}
				}
				if (isValid || keepInvalid) {
					for (int j = 0; j < stringsPerBlock; ++j) {
						output.Add(new MainDolString((uint)positions[j], romAddresses[j], strings[j], stringByteCounts[j]));
					}
				}
			}
		}
	}
}
