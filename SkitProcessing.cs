using System;
using System.Collections.Generic;
using System.IO;
using HyoutaPluginBase;
using HyoutaUtils;

namespace HyoutaTools.Tales.Graces.TranslationPort {
	internal static class SkitProcessing {
		private static bool MatchesSkitFormat(string s) {
			int idx = s.IndexOf("\x1F(1,");
			if (idx < 0) {
				return false;
			}

			int idx2 = s.IndexOf(")", idx);
			if (idx2 < 0) {
				return false;
			}

			if (idx2 + 1 != s.Length) {
				return false;
			}

			return true;
		}

		private static void PutString(List<string> newscs, string v, int currentIndex) {
			while (!(currentIndex < newscs.Count)) {
				newscs.Add("");
			}
			newscs[currentIndex] = v;
		}

		// some skit files reuse the same JP string in cases where the english text is different, this expands this to have a separate entry for each JP string
		public static (Stream newTssStream, SCS.SCS wscsnew) MultiplyOutSkitTss(DuplicatableStream stream, SCS.SCS wscsorig) {
			stream.Position = 0;
			Stream s = stream.CopyToMemory();
			uint magic = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint codeStart = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint unknown3 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint dataStart = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint unknown5 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint codeLength = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint dataLength = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			uint unknown8 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);

			List<(long pos, int number, long len)> strings = new List<(long pos, int number, long len)>();
			s.Position = codeStart;
			while (s.Position < (codeStart + codeLength + 0xC - 1)) {
				if (s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian) == 0x01000000) {
					if (s.PeekUInt64().FromEndian(EndianUtils.Endianness.BigEndian) == 0x0E000008040C0004) {
						s.DiscardBytes(8);
						uint offsetOfOffset = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
						long pos = s.Position;
						s.Position = dataStart + offsetOfOffset;
						uint offset = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
						long stringpos = dataStart + offset;
						s.Position = stringpos;
						string str = s.ReadNulltermString(TextUtils.GameTextEncoding.ShiftJIS);
						// must have specific format
						if (MatchesSkitFormat(str)) {
							int num = SCS.SCS.DecodeNumber(str.Substring(4, str.Length - 5));
							strings.Add((stringpos, num, str.Length));
						}
						s.Position = pos;
					}
				}
			}

			SortedSet<int> reservedNumbers = new SortedSet<int>();
			for (int i = 0; i < wscsorig.Entries.Count; ++i) {
				reservedNumbers.Add(i);
			}
			foreach (var d in strings) {
				if (reservedNumbers.Contains(d.number)) {
					reservedNumbers.Remove(d.number);
				}
			}

			List<string> newscs = new List<string>(wscsorig.Entries);

			List<(int oldIdx, int newIdx)> idxs = new List<(int oldIdx, int newIdx)>();
			int currentIndex = 0;
			foreach (var d in strings) {
				while (reservedNumbers.Contains(currentIndex)) {
					++currentIndex;
				}

				string numstr = SCS.SCS.EncodeNumber(currentIndex);
				string resultstr = "\x1F(1," + numstr + ")";
				if (resultstr.Length > d.len) {
					throw new Exception("don't know how to inject this");
				}
				s.Position = d.pos;
				s.WriteShiftJisNullterm(resultstr);
				PutString(newscs, wscsorig.Entries[d.number], currentIndex);
				++currentIndex;
			}

			s.Position = 0;
			return (s, new SCS.SCS(newscs));
		}

	}
}
