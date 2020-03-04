using System;
using System.Collections.Generic;
using System.IO;
using HyoutaPluginBase;
using HyoutaTools.Tales.Graces.SCS;
using HyoutaUtils;

namespace ToGLocInject {
	internal static class ScenarioProcessing {
		public static (Stream newScenarioFileStream, string newScenarioFilePath, SCS wscsnew, List<List<int>> new_multidefined_widxs) MultiplyOutScenarioFile(List<(int widx, int jidx)> widx_with_multidefined_j, FileFetcher _fc, string f, SCS wscs, List<(int index, string entry)> j, List<(int index, string entry)> u) {
			string sopath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f)), Path.GetFileNameWithoutExtension(f) + ".so").Replace('\\', '/');
			HyoutaPluginBase.FileContainer.IFile wfile = _fc.TryGetFile(sopath, Version.W);
			if (wfile == null) {
				throw new Exception("failed to find script file for " + f);
			}

			List<(long pos, int number, long len)> parsedNumbers = ParseScriptfile(wfile.DataStream.Duplicate());

			bool injected = false;
			Stream ms = wfile.DataStream.CopyToMemory();
			List<string> newscs = new List<string>(wscs.Entries);
			List<List<int>> new_multidefined_widxs = new List<List<int>>();
			foreach (var v in widx_with_multidefined_j) {
				List<(long pos, int number, long len)> thisindex = new List<(long pos, int number, long len)>();
				foreach (var t in parsedNumbers) {
					if (t.number == v.widx) {
						thisindex.Add(t);
					}
				}
				if (thisindex.Count <= 1) {
					continue;
				}

				List<int> wscsidxes = new List<int>();
				wscsidxes.Add(v.widx);
				for (int i = 1; i < thisindex.Count; ++i) {
					int newnumber = newscs.Count;

					// overwrite number in scriptfile
					var d = thisindex[i];
					string numstr = SCS.EncodeNumber(newnumber);
					string resultstr = "\x1F(0," + numstr + ")";
					if (resultstr.Length > d.len) {
						if ((d.number == 31 || d.number == 32 || d.number == 33) && f.Contains("zoneR.cpk")) {
							// these are harmless, just ignore them
							continue;
						}

						if (d.number == 34 && f == "map1R.cpk/mapfile_lanR.cpk/map/sce/R/ja/e834_080.scs") {
							// here a system string and a much later string match
							// luckly, we have two early 1-scs-digit strings that are different in JP but match in EN, so we can hijack that
							var all34 = parsedNumbers.FindAll(x => x.number == 34);
							var all36 = parsedNumbers.FindAll(x => x.number == 36);
							var all60 = parsedNumbers.FindAll(x => x.number == 60);
							HyoutaTools.Util.Assert(all34.Count == 2 && all34[0].pos < all34[1].pos && all36.Count == 1 && all60.Count == 1);
							HyoutaTools.Util.Assert(newscs[36] == newscs[60]);

							ms.Position = all60[0].pos;
							ms.WriteShiftJisNullterm("\x1F(0," + SCS.EncodeNumber(36) + ")");
							ms.Position = all34[1].pos;
							ms.WriteShiftJisNullterm("\x1F(0," + SCS.EncodeNumber(60) + ")");
							newscs[60] = u.Find(x => x.index == 414).entry;
							injected = true;

							continue;
						}

						if (d.number == 60 && f == "map1R.cpk/mapfile_otheR.cpk/map/sce/R/ja/othe_t03.scs") {
							// similar thing here, we have two 60s that need to be different, but the 36 matches the first 60, so remap that instead
							var all36 = parsedNumbers.FindAll(x => x.number == 36);
							var all60 = parsedNumbers.FindAll(x => x.number == 60);
							HyoutaTools.Util.Assert(all36.Count == 1 && all60.Count == 2 && all60[0].pos < all60[1].pos);
							HyoutaTools.Util.Assert(newscs[36] == newscs[60]);

							ms.Position = all60[0].pos;
							ms.WriteShiftJisNullterm("\x1F(0," + SCS.EncodeNumber(36) + ")");
							newscs[60] = u.Find(x => x.index == 272).entry;
							injected = true;

							continue;
						}

						Console.WriteLine("don't know how to inject new number for " + d.number + " (" + newscs[v.widx] + ") in " + f);
						continue;
					}
					ms.Position = d.pos;
					ms.WriteShiftJisNullterm(resultstr);

					// append new string in wscs
					newscs.Add(newscs[v.widx]);

					wscsidxes.Add(newnumber);
					injected = true;
				}
				if (wscsidxes.Count > 1) {
					new_multidefined_widxs.Add(wscsidxes);
				}
			}

			if (injected) {
				ms.Position = 0;
				return (ms, sopath, new SCS(newscs), new_multidefined_widxs);
			}

			return (null, null, null, null);
		}

		private static bool MatchesTextFormat(string s) {
			int idx = s.IndexOf("\x1F(0,");
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

		public static List<(long pos, int number, long len)> ParseScriptfile(DuplicatableStream stream) {
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
			while (s.Position < (dataStart + dataLength)) {
				int b = s.ReadByte();
				if (b == 0x1F) {
					long pos = s.Position;
					uint v = s.PeekUInt24();
					if (v == 0x2C3028) {
						s.Position = pos - 1;
						string str = s.ReadNulltermString(TextUtils.GameTextEncoding.ShiftJIS);
						if (MatchesTextFormat(str)) {
							int num = SCS.DecodeNumber(str.Substring(4, str.Length - 5));
							strings.Add((pos - 1, num, str.Length));
						}
						s.Position = pos;
					}
				}
			}

			return strings;
		}
	}
}
