using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HyoutaPluginBase;
using HyoutaTools.Generic;
using HyoutaTools.Tales.Graces.SCS;
using HyoutaUtils;
using HyoutaUtils.Streams;

namespace ToGLocInject {
	internal static partial class FileProcessing {
		static bool IsMatching(this MappingType t) {
			return t == MappingType.SelfMatch || t == MappingType.CompMatch || t == MappingType.DirectMatched || t == MappingType.VoiceLineMatched;
		}

		public static int Decode0x1F(string s, string starter) {
			if (s.StartsWith(starter)) {
				string t = s.Substring(3);
				t = t.Substring(0, t.IndexOf(')'));
				return SCS.DecodeNumber(t);
			}
			throw new Exception("?");
		}

		public static List<int> ParseTss(Stream s, bool isSkitFile) {
			List<int> data = new List<int>();
			s.Position = 0;
			while (true) {
				int b = s.ReadByte();
				if (b == -1) {
					break;
				}

				if (b == 0x1F) {
					uint v = s.PeekUInt24();
					if (!isSkitFile && v == 0x2C3028) {
						data.Add(Decode0x1F(s.ReadAsciiNullterm(), "(0,"));
					} else if (isSkitFile && v == 0x2C3128) {
						data.Add(Decode0x1F(s.ReadAsciiNullterm(), "(1,"));
					}
				}
			}
			if (false) {
				List<string> strings = new List<string>();
				s.Position = 0;
				uint magic = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint codeStart = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint unknown3 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint dataStart = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint unknown5 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint codeLength = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint dataLength = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				uint unknown8 = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);

				s.Position = codeStart;
				while (s.Position < (codeStart + codeLength)) {
					if (s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian) == 0x02820000) {
						uint offset = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
						strings.Add(s.ReadNulltermStringFromLocationAndReset(dataStart + offset, TextUtils.GameTextEncoding.ShiftJIS));
					}
				}
			}
			return data;
		}

		public static List<(int index, string entry)> Apply(List<int> indices, List<string> scs) {
			List<(int index, string entry)> data = new List<(int index, string entry)>();
			for (int i = 0; i < indices.Count; ++i) {
				string v = indices[i] == -1 ? "" : scs[indices[i]];
				data.Add((indices[i], v));
			}
			return data;
		}

		public static void CreateCsv(string csvp, List<(int index, string entry)> ea) {
			List<string> lines = new List<string>();
			foreach (var entry in ea) {
				lines.Add(string.Format("{0}\t{1}", entry.index, entry.entry?.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ")));
			}
			File.WriteAllLines(csvp, lines);
		}

		private static bool ShouldPrintMappedEntry(List<MappedEntry> entries, int i) {
			int firstInRange = Math.Max(i - 3, 0);
			int lastInRange = Math.Min(i + 3, entries.Count - 1);
			for (int j = firstInRange; j <= lastInRange; ++j) {
				MappedEntry e = entries[j];
				if (!e.type.IsMatching()) {
					return true;
				}
			}
			return false;
		}

		private static void InjectFile(FileInjectorV0V2 map0inject, FileInjectorV0V2 map1inject, FileInjectorV0V2 rootinject, string f, Stream scsstr) {
			string[] splitf = f.Split(new char[] { '/' });
			FileInjectorV0V2 injector;
			switch (splitf[0]) {
				case "rootR.cpk": injector = rootinject; break;
				case "map0R.cpk": injector = map0inject; break;
				case "map1R.cpk": injector = map1inject; break;
				default: throw new Exception();
			}
			string subcpk = splitf[1].EndsWith(".cpk") ? splitf[1] : null;
			if (subcpk == null) {
				injector.InjectFile(scsstr, f.Split(new char[] { '/' }, 2)[1]);
			} else {
				injector.InjectFileSubcpk(scsstr, subcpk, f.Split(new char[] { '/' }, 3)[2]);
			}
		}

		private static IEnumerable<(int dolidx, bool forceInternal)> GenerateDoltextInjectOrder(List<MainDolString> doltext) {
			// late strings will be written to font tex, early strings will go into the eboot
			for (int i = 10456; i < doltext.Count; ++i) { yield return (i, true); }
			for (int i = 0; i < 4384; ++i) { yield return (i, true); }
			for (int i = 4419; i < 4489; ++i) { yield return (i, true); }
			for (int i = 4637; i < 6888; ++i) { yield return (i, true); }
			for (int i = 7644; i < 10456; ++i) { yield return (i, true); }
			for (int i = 6888; i < 7644; i += 3) { yield return (i, true); } // request names
			for (int i = 6888; i < 7644; i += 3) { yield return (i + 1, false); yield return (i + 2, false); } // request text
			for (int i = 4384; i < 4419; ++i) { yield return (i, false); } // synopsis text
			for (int i = 4489; i < 4637; ++i) { yield return (i, false); } // sidequest text
		}

		private static string ReduceToSingleLine(string text) {
			string t = text.Replace("\r", "\\r").Replace("\n", "\\n");
			for (int i = 0; i < 0x20; ++i) {
				if (i == 10 || i == 13) continue;
				t = t.Replace(((char)i).ToString(), "\\" + "x" + i.ToString("X2"));
			}
			return t;
		}

		private static SCS ConvertDoltextToScs(List<MainDolString> doltext) {
			List<string> t = new List<string>();
			foreach (var v in doltext) {
				t.Add(v.Text);
			}
			return new SCS(t);
		}

		private static SCS ReadBattleNames(DuplicatableStream stream, Version v) {
			// technically this is an fps4 archive but we can shortcut it since the format is similar enough between versions...
			stream.Position = 0x1C;
			long start = stream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			long size = stream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			stream.Position = start;
			List<string> names = new List<string>();
			while (stream.Position < (start + size - 0x18)) {
				long tmp = stream.Position;
				names.Add(stream.ReadShiftJisNullterm());
				stream.Position = tmp + (v == Version.W ? 0x88 : 0xA0);
			}
			return new SCS(names);
		}

		private static void ReplaceStringsWMainDolPart((int start, int end) wr, (int start, int end) jur, ref int unmappedCount, ref List<int> indicesUnmapped, ref List<bool> juConsumedGlobal, ISet<string> acceptableNonReplacements, SCS wscs, List<(int index, string entry)> j, List<(int index, string entry)> u, W prep, bool allowSloppyComp) {
			int wstart = wr.start;
			int wend = wr.end;
			List<string> reducedW = new List<string>();
			for (int i = 0; i < wend - wstart; ++i) {
				reducedW.Add(wscs.Entries[i + wstart]);
			}
			SCS reducedWSCS = new SCS(reducedW);
			SCS reducedWSCSorig = new SCS(reducedW);
			List<(int index, string entry)> reducedJ = new List<(int index, string entry)>();
			List<(int index, string entry)> reducedU = new List<(int index, string entry)>();
			for (int i = jur.start; i < jur.end; ++i) {
				reducedJ.Add(j[i]);
				reducedU.Add(u[i]);
			}
			var rv = ReplaceStringsW(acceptableNonReplacements, reducedWSCS, reducedWSCSorig, reducedJ, reducedU, prep, allowSloppyComp, null);
			for (int i = 0; i < wend - wstart; ++i) {
				wscs.Entries[i + wstart] = reducedWSCS.Entries[i];
			}
			unmappedCount += rv.unmappedCount;
			foreach (int v in rv.indicesUnmapped) {
				indicesUnmapped.Add(v + wstart);
			}
			for (int i = 0; i < rv.juConsumedGlobal.Count; ++i) {
				juConsumedGlobal[i + jur.start] = rv.juConsumedGlobal[i];
			}
		}

		private static (int unmappedCount, List<int> indicesUnmapped, List<bool> juConsumedGlobal, List<(int widx, int jidx)> widx_with_multidefined_j) ReplaceStringsWMainDol(ISet<string> acceptableNonReplacements, SCS wscs, List<(int index, string entry)> j, List<(int index, string entry)> u, W prep, bool allowSloppyComp) {
			int unmappedCount = 0;
			List<int> indicesUnmapped = new List<int>();
			List<bool> juConsumedGlobal = new List<bool>(u.Count);
			for (int i = 0; i < u.Count; ++i) {
				juConsumedGlobal.Add(false);
			}
			ReplaceStringsWMainDolPart((986, 1114), (0, 128), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1114, 1380), (128, 300), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1380, 1466), (300, 386), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1466, 1548), (386, 476), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1548, 1647), (476, 582), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1647, 1748), (582, 693), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1748, 1916), (693, 870), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((1916, 2273), (870, 1242), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((2273, 2329), (1242, 1284), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((2329, 3069), (1284, 2056), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((3069, 3098), (2056, 2085), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((3098, 3160), (2085, 2149), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((3160, 3287), (2149, 2278), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((3287, 4331), (2278, 3502), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((4331, 4637), (3502, 3859), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((4637, 4793), (3859, 4029), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((4793, 5066), (4029, 4357), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((5066, 6888), (4357, 6451), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((6888, 7644), (6451, 7207), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((7644, 8466), (7207, 8101), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((8466, 8616), (8101, 8265), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			ReplaceStringsWMainDolPart((8616, 10456), (8265, 10429), ref unmappedCount, ref indicesUnmapped, ref juConsumedGlobal, acceptableNonReplacements, wscs, j, u, prep, allowSloppyComp);
			return (unmappedCount, indicesUnmapped, juConsumedGlobal, new List<(int widx, int jidx)>());
		}

		private static (int unmappedCount, List<int> indicesUnmapped, List<bool> juConsumedGlobal, List<(int widx, int jidx)> widx_with_multidefined_j) ReplaceStringsW(ISet<string> acceptableNonReplacements, SCS wscs, SCS wscsorig, List<(int index, string entry)> j, List<(int index, string entry)> u, W prep, bool allowSloppyComp, SortedSet<int> multidefined_j_idxs) {
			int replacementCountGlobal = 0;
			List<bool> juConsumed = new List<bool>(u.Count);
			List<bool> juConsumedGlobal = new List<bool>(u.Count);
			List<bool> wOverwritten = new List<bool>(wscs.Entries.Count);
			for (int i = 0; i < u.Count; ++i) {
				juConsumed.Add(j[i].entry == null);
				juConsumedGlobal.Add(j[i].entry == null);
			}
			for (int i = 0; i < wscs.Entries.Count; ++i) {
				bool o = IsAcceptableUnchanged(wscs.Entries[i]);
				wOverwritten.Add(o);
				if (o) {
					++replacementCountGlobal;
				}
			}

			bool didSloppyInit = false;
			List<string> sloppy_j = null;
			List<string> sloppy_w = null;
			List<(int widx, int jidx)> widx_with_multidefined_j = new List<(int widx, int jidx)>();
			while (true) {
				int replacementCount = 0;
				// first map direct, 1:1, marking every used ju and not reusing already used ju strings
				replacementCount += ReplaceStringsWInternal_Strict(wscs, j, u, juConsumed, wOverwritten, multidefined_j_idxs, widx_with_multidefined_j);

				// then do that again, but with a sloppy comparison logic
				if (allowSloppyComp) {
					if (!didSloppyInit) {
						sloppy_w = new List<string>(wscs.Entries.Count);
						sloppy_j = new List<string>(j.Count);
						for (int i = 0; i < wscs.Entries.Count; ++i) {
							if (!wOverwritten[i]) {
								sloppy_w.Add(MakeSloppy(wscs.Entries[i]));
							} else {
								sloppy_w.Add(null);
							}
						}
						for (int i = 0; i < j.Count; ++i) {
							sloppy_j.Add(j[i].entry == null ? null : MakeSloppy(j[i].entry));
						}

						// if we sloppy compare the first time then mark any sloppy-empty strings as mapped
						// (dangerous otherwise, as two sloppy-empty strings that are completely different could match)
						for (int i = 0; i < wscs.Entries.Count; ++i) {
							if (!wOverwritten[i]) {
								wOverwritten[i] = sloppy_w[i] == ""; // pre-assume empty strings as mapped
							}
						}
						didSloppyInit = true;
					}
					replacementCount += ReplaceStringsWInternal_Sloppy(wscs, j, u, juConsumed, wOverwritten, sloppy_j, sloppy_w, multidefined_j_idxs, widx_with_multidefined_j);
				}

				for (int i = 0; i < u.Count; ++i) {
					if (juConsumed[i]) {
						juConsumedGlobal[i] = true;
					}
				}

				// if nothing was replaced we're done here, exit out
				if (replacementCount == 0) {
					break;
				}

				// otherwise do another loop with the full ju set again
				replacementCountGlobal += replacementCount;

				for (int i = 0; i < u.Count; ++i) {
					juConsumed[i] = j[i].entry == null;
				}
			}

			{
				foreach (var x in prep.PrecalculatedReplacements) {
					if (x.u == W.KEEP_ORIGINAL_MARK_MAPPED) {
						wscs.Entries[x.w] = wscsorig.Entries[x.w];
						if (!wOverwritten[x.w]) {
							++replacementCountGlobal;
							wOverwritten[x.w] = true;
						}
					} else if (x.u == W.KEEP_ORIGINAL_MARK_UNMAPPED) {
						wscs.Entries[x.w] = wscsorig.Entries[x.w];
						if (wOverwritten[x.w]) {
							--replacementCountGlobal;
							wOverwritten[x.w] = false;
						}
					} else {
						int replidx = u.FindIndex(y => y.index == x.u);
						string ustr = u[replidx].entry;
						wscs.Entries[x.w] = ustr;
						if (!wOverwritten[x.w]) {
							++replacementCountGlobal;
							wOverwritten[x.w] = true;
						}
					}
				}
				foreach (var x in prep.PrecalculatedReplacementsDirect) {
					string repl = x.u;
					wscs.Entries[x.w] = repl;
					if (!wOverwritten[x.w]) {
						++replacementCountGlobal;
						wOverwritten[x.w] = true;
					}
				}
				foreach (var x in prep.PostProcessing) {
					wscs.Entries[x.w] = x.func(wscsorig.Entries[x.w], wscs.Entries[x.w]);
				}
			}

			int unreplacedCounter = 0;
			List<int> unmappedIndices = new List<int>();
			if (replacementCountGlobal < wscs.Entries.Count) {
				for (int i = 0; i < wscs.Entries.Count; ++i) {
					if (!wOverwritten[i] && !acceptableNonReplacements.Contains(wscs.Entries[i])) {
						++unreplacedCounter;
						unmappedIndices.Add(i);
					}
				}
			}

			return (unreplacedCounter, unmappedIndices, juConsumedGlobal, widx_with_multidefined_j);
		}

		private static bool IsAcceptableUnchanged(string s) {
			if (s == null || s == "") {
				return true;
			}

			if (IsOnlyNametag(s)) {
				return true;
			}

			return false;
		}

		private static bool IsOnlyNametag(string s) {
			if (s.StartsWith("\u0004(")) {
				int braceclose = s.IndexOf(')');
				if (braceclose == -1) {
					return false;
				}

				string postbrace = s.Substring(braceclose + 1);
				if (postbrace != "") {
					return false;
				}

				try {
					string inbrace = s.Substring(2, braceclose - 2);
					SCS.DecodeNumber(inbrace);
					return true;
				} catch (Exception) {
					return false;
				}
			}

			return false;
		}

		private static string MakeSloppy(string s) {
			s = s.Replace(" ", "");
			s = s.Replace("、", "");
			s = s.Replace("\n", "");
			s = s.Replace("　", "");
			s = s.Replace("～", "~");
			s = s.Replace("！", "!");
			s = s.Replace("…", "...");
			s = s.Replace("？", "?");
			return s;
		}

		private static int ReplaceStringsWInternal_Strict(SCS wscs, List<(int index, string entry)> j, List<(int index, string entry)> u, List<bool> juConsumed, List<bool> wOverwritten, SortedSet<int> multidefined_j_idxs, List<(int widx, int jidx)> widx_with_multidefined_j) {
			int replaceCount = 0;
			for (int idx = 0; idx < wscs.Entries.Count; ++idx) {
				if (!wOverwritten[idx]) {
					string jp = wscs.Entries[idx];
					for (int idx2 = 0; idx2 < j.Count; ++idx2) {
						if (!juConsumed[idx2] && u[idx2].entry != null && u[idx2].index >= 0 && (j[idx2].entry == jp)) {
							wOverwritten[idx] = true;
							juConsumed[idx2] = true;
							wscs.Entries[idx] = u[idx2].entry;
							++replaceCount;
							if (multidefined_j_idxs != null && multidefined_j_idxs.Contains(j[idx2].index)) {
								widx_with_multidefined_j.Add((idx, j[idx2].index));
							}
							break;
						}
					}
				}
			}
			return replaceCount;
		}
		private static int ReplaceStringsWInternal_Sloppy(SCS wscs, List<(int index, string entry)> j, List<(int index, string entry)> u, List<bool> juConsumed, List<bool> wOverwritten, List<string> precompd_j_sloppy, List<string> precompd_w_sloppy, SortedSet<int> multidefined_j_idxs, List<(int widx, int jidx)> widx_with_multidefined_j) {
			int replaceCount = 0;
			for (int idx = 0; idx < wscs.Entries.Count; ++idx) {
				if (!wOverwritten[idx]) {
					string jp = precompd_w_sloppy[idx];
					for (int idx2 = 0; idx2 < j.Count; ++idx2) {
						if (!juConsumed[idx2] && u[idx2].entry != null && u[idx2].index >= 0 && (precompd_j_sloppy[idx2] == jp)) {
							wOverwritten[idx] = true;
							juConsumed[idx2] = true;
							wscs.Entries[idx] = u[idx2].entry;
							++replaceCount;
							if (multidefined_j_idxs != null && multidefined_j_idxs.Contains(j[idx2].index)) {
								widx_with_multidefined_j.Add((idx, j[idx2].index));
							}
							break;
						}
					}
				}
			}
			return replaceCount;
		}

		private static SCS ReadChatNames(DuplicatableStream s) {
			List<string> strings = new List<string>();
			s.DiscardBytes(8);
			uint count = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			s.DiscardBytes(12);
			for (uint i = 0; i < count; ++i) {
				long p = s.Position;
				long v = s.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
				strings.Add(s.ReadNulltermStringFromLocationAndReset(p + v, TextUtils.GameTextEncoding.ShiftJIS));
			}
			return new SCS(strings);
		}

		private static List<(string regular, string alt)> BuildCharnameMapping(SCS charnameJ, List<int> charnamemappingsJ) {
			var m = new List<(string regular, string alt)>();
			for (int i = 0; i < charnamemappingsJ.Count; i += 2) {
				int reg = charnamemappingsJ[i];
				int alt = charnamemappingsJ[i + 1];
				m.Add((reg >= 0 ? charnameJ.Entries[reg] : "", alt >= 0 ? charnameJ.Entries[alt] : ""));
			}
			return m;
		}

		private static (DuplicatableStream, List<int>) EvaluateCharNameBin(DuplicatableStream jstream) {
			List<int> jmappingoverride;
			jstream.ReStart();
			long jstreamsize = jstream.Length;
			jstream.DiscardBytes(4);
			uint jstreamoffset = jstream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			jstream.DiscardBytes(4);
			uint jstreammappingstart = jstream.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
			jstream.Position = jstreammappingstart;
			jmappingoverride = new List<int>();
			while (jstream.Position < jstreamoffset) {
				jmappingoverride.Add(((int)jstream.ReadUInt16().FromEndian(EndianUtils.Endianness.BigEndian)) - 1);
			}
			jstream.End();
			jstream = new PartialStream(jstream, jstreamoffset, jstreamsize - jstreamoffset);
			return (jstream, jmappingoverride);
		}

		private static bool IsValid(int? v, List<(int index, string entry)> tssa) {
			return v == null ? false : v.Value < 0 ? false : v.Value >= tssa.Count ? false : true;
		}

		private static string FindTssCompareString((MappedEntry e, int? jtss, int? utss) x, List<(int index, string entry)> jtssa, List<(int index, string entry)> utssa) {
			if (x.jtss == null && x.utss == null) {
				return "shouldn't happen??";
			}
			if (x.jtss == null) {
				return "U only";
			}
			if (x.utss == null) {
				return "J only";
			}

			bool jvalid = IsValid(x.jtss, jtssa);
			bool uvalid = IsValid(x.utss, utssa);
			if (!jvalid && !uvalid) {
				return "both invalid";
			}

			bool selfmatch = jvalid && uvalid && jtssa[x.jtss.Value].entry == utssa[x.utss.Value].entry;
			if (x.e == null) {
				return (selfmatch ? "y" : ".") + ", no entry";
			}

			bool jpmatch = jvalid && x.e.jpos == jtssa[x.jtss.Value].index;
			bool usmatch = uvalid && x.e.upos == utssa[x.utss.Value].index;

			return (selfmatch ? "y" : ".") + (jpmatch ? "j" : ".") + (usmatch ? "u" : ".");
		}

		private static int FindFirstIndex(List<(int index, string entry)> jtssapplied, int jpos) {
			for (int i = 0; i < jtssapplied.Count; ++i) {
				if (jtssapplied[i].index == jpos) {
					return i;
				}
			}
			return -1;
		}

		private static string CsvEscape(string jp, List<(string regular, string alt)> charnames) {
			if (jp == null) {
				return "[null]";
			}
			if (jp == "") {
				return "[empty]";
			}
			string s = jp.Replace("\t", "[t]").Replace("\n", "[n]").Replace("\r", "[r]").Replace("\f", "[f]").ReplaceNames(charnames);
			if (s.StartsWith(" ")) {
				s = "[ ]" + s.Substring(1);
			}
			if (s.EndsWith(" ")) {
				s = s.Substring(0, s.Length - 1) + "[ ]";
			}
			return s;
		}

		private static string ReplaceNames(this string input, List<(string regular, string alt)> charnames) {
			string s = input;
			StringBuilder sb = new StringBuilder();
			while (true) {
				int idx = s.IndexOf('\u0004');
				if (idx == -1) {
					sb.Append(s);
					break;
				}

				string s0 = s.Substring(0, idx);
				string s1 = s.Substring(idx + 1);
				sb.Append(s0);

				if (s1[0] == '(') {
					sb.Append("{");
					int braceclose = s1.IndexOf(')');
					string inbrace = s1.Substring(1, braceclose - 1);
					string postbrace = s1.Substring(braceclose + 1);
					int decodednumber = SCS.DecodeNumber(inbrace) - 1001;
					if (decodednumber >= 0 && decodednumber < charnames.Count) {
						sb.Append(charnames[decodednumber].regular);
					} else {
						sb.Append(decodednumber);
					}
					s = postbrace;
					sb.Append("}");
				} else {
					sb.Append("CHAR");
					s = s1;
				}
			}
			return sb.ToString();
		}

		private static int CountWrongEntriesAtEnd(List<MappedEntry> entries) {
			int cnt = 0;
			for (int i = entries.Count - 1; i >= 0; --i) {
				var e = entries[i];
				if (e.type == MappingType.JpOnly || e.type == MappingType.EnOnly) {
					++cnt;
				} else {
					return cnt;
				}
			}
			return cnt;
		}

		private static List<MappedEntry> MapCompare(List<(string jp, string en)> compare, List<(int index, string entry)> j, List<(int index, string entry)> u, Dictionary<string, List<string>> directCompare) {
			List<MappedEntry> entries = new List<MappedEntry>();

			int jpos = 0;
			int upos = 0;
			int cpos = 0;
			while (true) {
				if (jpos >= j.Count || upos >= u.Count || cpos >= compare.Count) {
					break;
				}

				if (j[jpos].entry == u[upos].entry) {
					entries.Add(new MappedEntry() { jpos = j[jpos].index, upos = u[upos].index, jp = j[jpos].entry, en = u[upos].entry, type = MappingType.SelfMatch });
					++jpos;
					++upos;
					continue;
				}

				if (j[jpos].entry == compare[cpos].jp && u[upos].entry == compare[cpos].en) {
					entries.Add(new MappedEntry() { jpos = j[jpos].index, upos = u[upos].index, jp = j[jpos].entry, en = u[upos].entry, type = MappingType.CompMatch });
					++jpos;
					++upos;
					++cpos;
					continue;
				}

				if (j[jpos].entry == compare[cpos].jp) {
					// en match, see if we can find matching j
					int utmp = upos;
					bool found = false;
					while (true) {
						if (utmp >= u.Count) { break; }
						if (u[utmp].entry == compare[cpos].en) {
							found = true;
							break;
						} else {
							++utmp;
						}
					}
					if (found) {
						for (int w = upos; w < utmp; ++w) {
							entries.Add(new MappedEntry() { upos = u[w].index, jpos = -1, en = u[w].entry, jp = null, type = MappingType.EnOnly });
						}
						upos = utmp;
						continue;
					}
				}

				if (u[upos].entry == compare[cpos].en) {
					// en match, see if we can find matching j
					int jtmp = jpos;
					bool found = false;
					while (true) {
						if (jtmp >= j.Count) { break; }
						if (j[jtmp].entry == compare[cpos].jp) {
							found = true;
							break;
						} else {
							++jtmp;
						}
					}
					if (found) {
						for (int w = jpos; w < jtmp; ++w) {
							entries.Add(new MappedEntry() { jpos = j[w].index, upos = -1, jp = j[w].entry, en = null, type = MappingType.JpOnly });
						}
						jpos = jtmp;
						continue;
					}
				}

				// check if this pair is in remaining compare 
				{
					int ctmp = cpos;
					bool found = false;
					while (true) {
						if (ctmp >= compare.Count) { break; }
						if (j[jpos].entry == compare[ctmp].jp && u[upos].entry == compare[ctmp].en) {
							found = true;
							break;
						} else {
							++ctmp;
						}
					}
					if (found) {
						cpos = ctmp;
						continue;
					}
				}

				MappingType mappingType = MappingType.NotMatched;
				List<string> tmpval;
				if (directCompare.TryGetValue(j[jpos].entry, out tmpval)) {
					if (tmpval.Contains(u[upos].entry)) {
						mappingType = MappingType.DirectMatched;
					}
				}
				if (mappingType == MappingType.NotMatched && DoMatchByVoiceLine(j[jpos].entry, u[upos].entry)) {
					mappingType = MappingType.VoiceLineMatched;
				}
				entries.Add(new MappedEntry() { jpos = j[jpos].index, upos = u[upos].index, jp = j[jpos].entry, en = u[upos].entry, type = mappingType });
				++jpos;
				++upos;
			}

			for (int w = jpos; w < j.Count; ++w) {
				entries.Add(new MappedEntry() { jpos = j[w].index, upos = -1, jp = j[w].entry, en = null, type = MappingType.JpOnly });
			}
			for (int w = upos; w < u.Count; ++w) {
				entries.Add(new MappedEntry() { upos = u[w].index, jpos = -1, en = u[w].entry, jp = null, type = MappingType.EnOnly });
			}

			return entries;
		}

		private static List<string> ExtractVoiceLines(string s) {
			List<string> voiceLines = new List<string>();
			string w = s;
			while (true) {
				int t = w.IndexOf('\t');
				if (t == -1) {
					break;
				}

				w = w.Substring(t + 1);

				int braceOpen = w.IndexOf('(');
				int braceClose = w.IndexOf(')');
				if (braceOpen != -1 && braceClose != -1 && braceOpen < braceClose) {
					string v = w.Substring(braceOpen + 1, braceClose - braceOpen - 1);
					voiceLines.Add(v);
				}
			}
			return voiceLines;
		}

		private static string RemoveVoiceLines(string s) {
			StringBuilder sb = new StringBuilder();
			bool inVoiceLine = false;
			foreach (char c in s) {
				if (!inVoiceLine) {
					if (c == '\t') {
						inVoiceLine = true;
					} else {
						sb.Append(c);
					}
				} else {
					if (c == ')') {
						inVoiceLine = false;
					}
				}
			}
			return sb.ToString();
		}

		private static bool DoMatchByVoiceLine(string jp, string en) {
			var jv = ExtractVoiceLines(jp);
			if (jv.Count > 0) {
				var ev = ExtractVoiceLines(en);
				return jv.SequenceEqual(ev);
			}
			return false;
		}

		private static bool IsSkitFile(string filename) {
			string[] stuff = filename.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
			return stuff[0] == "rootR.cpk" && stuff[1] == "chat" && (stuff.Last().StartsWith("CHT_") || stuff.Last().StartsWith("debug_0"));
		}

		private static SortedSet<int> GetDefinedAddsSet(M m) {
			SortedSet<int> set = new SortedSet<int>();
			foreach (var a in m.Adds) {
				foreach (int b in a.entries) {
					if (b >= 0) {
						set.Add(b);
					}
				}
			}
			return set;
		}

		private static List<(int index, string entry)> InsertAdds(SCS scs, M m, bool isU, bool isSkit) {
			List<(int index, string entry)> e = new List<(int index, string entry)>();

			for (int i = 0; i < scs.Entries.Count; ++i) {
				e.Add((i, scs.Entries[i]));
			}

			if (isSkit && isU) {
				// these have audio clips that the JP ver doesn't use, so we should remove them
				List<(int index, string entry)> n = new List<(int index, string entry)>();
				foreach (var x in e) {
					n.Add((x.index, RemoveVoiceLines(x.entry)));
				}
				e = n;
			}

			foreach (var a in m.Adds.OrderByDescending(x => x.where)) {
				var c = new List<int>(a.entries);
				c.Reverse();
				foreach (int b in c) {
					if (b < 0) {
						for (int i = 0; i < -b; ++i) {
							e.Insert(a.where, (m.ReplaceEmptyStringsInsteadOfSkippingNegativeAdds ? int.MaxValue : -1, ""));
						}
					} else {
						e.Insert(a.where, (b, scs.Entries[b]));
					}
				}
			}

			foreach (var feedsplit in m.FeedSplits) {
				int idx = feedsplit.entry;
				char ch = feedsplit.character;
				List<(int index, string entry)> n = new List<(int index, string entry)>();
				foreach (var x in e) {
					if (x.index == idx) {
						if (feedsplit.keepChar) {
							List<int> where = FindPosSplits(x.entry, ch, feedsplit.maxSplits).ToList();
							foreach (string s in PositionSplit(x.entry, where)) {
								n.Add((x.index, s));
							}
						} else {
							foreach (string s in TextboxSplit(x.entry, ch)) {
								n.Add((x.index, s));
							}
						}
					} else {
						n.Add(x);
					}
				}
				e = n;
			}

			foreach (var possplit in m.PosSplits) {
				int idx = possplit.entry;
				List<int> where = possplit.where;
				List<(int index, string entry)> n = new List<(int index, string entry)>();
				foreach (var x in e) {
					if (x.index == idx) {
						foreach (string s in PositionSplit(x.entry, where)) {
							n.Add((x.index, s));
						}
					} else {
						n.Add(x);
					}
				}
				e = n;
			}

			foreach (var idxs in m.Merges) {
				List<int> targets = new List<int>();
				for (int i = 0; i < e.Count; ++i) {
					var x = e[i];
					if (x.index == idxs.target) {
						targets.Add(i);
					}
				}

				List<string> joinedText = new List<string>();
				foreach (var x in e) {
					foreach (var y in idxs.sources) {
						if (y == x.index) {
							joinedText.Add(x.entry);
						}
					}
				}

				string final = DoJoin(idxs.joiner, joinedText, idxs.newlinesToRemove, idxs.newlinesToAdd);
				foreach (int target in targets) {
					e[target] = (e[target].index, final);
				}
			}

			if (m.Removes.Count > 0) {
				List<(int index, string entry)> n = new List<(int index, string entry)>();
				foreach (var x in e) {
					if (!m.Removes.Contains(x.index)) {
						n.Add(x);
					}
				}
				e = n;
			}

			return e;
		}

		private static IEnumerable<int> FindPosSplits(string entry, char ch, int maxSplits) {
			int cntr = 0;
			for (int i = 1; i < entry.Length; ++i) {
				if (ch == entry[i - 1]) {
					yield return i;
					++cntr;
					if (cntr == maxSplits) {
						break;
					}
				}
			}
		}

		private static IEnumerable<string> PositionSplit(string entry, List<int> where) {
			string s = entry;
			int last = 0;
			foreach (int w in where) {
				string left = s.Substring(0, w - last);
				string right = s.Substring(w - last);
				last = w;
				yield return left;
				s = right;
			}
			yield return s;
		}

		private static string DoJoin(string sep, List<string> vals, List<int> newlinesToRemove, List<int> newlinesToAdd) {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < vals.Count; ++i) {
				bool first = i == 0;
				bool last = i == (vals.Count - 1);
				if (!first) {
					sb.Append(sep);
				}

				bool shouldRemoveCloseBrace = !last && vals[i].EndsWith(")") && vals[i + 1].StartsWith("(");
				bool shouldRemoveOpenBrace = !first && vals[i].StartsWith("(") && vals[i - 1].EndsWith(")");
				if (!shouldRemoveOpenBrace && !shouldRemoveCloseBrace) {
					sb.Append(vals[i]);
				} else {
					int start = shouldRemoveOpenBrace ? 1 : 0;
					int end = shouldRemoveCloseBrace ? (vals[i].Length - 1) : vals[i].Length;
					sb.Append(vals[i].Substring(start, end - start));
				}
			}
			if (newlinesToRemove != null) {
				int currentNewline = 0;
				for (int i = 0; i < sb.Length; ++i) {
					if (sb[i] == '\n') {
						if (newlinesToRemove.Contains(currentNewline)) {
							sb[i] = ' ';
						}
						++currentNewline;
					}
				}
			}
			if (newlinesToAdd != null) {
				foreach (int nl in newlinesToAdd) {
					sb[nl] = '\n';
				}
			}
			return sb.ToString();
		}

		private static string[] TextboxSplit(string entry, char ch) {
			return entry.Split(new char[] { ch });
		}

		private static ReservedMemchunk ReserveMemory(List<MemChunk> memchunks, uint size) {
			MemChunk scratchChunk = memchunks.FirstOrDefault(x => x.FreeBytes >= size && x.IsInternal && (x.Mapper.MapRomToRam(x.Address) % 4) == 0);
			if (scratchChunk != null) {
				uint addressRom = scratchChunk.Address;
				uint addressRam = scratchChunk.Mapper.MapRomToRam(addressRom);
				scratchChunk.File.Position = scratchChunk.Address;
				for (uint cnt = 0; cnt < size; ++cnt) {
					scratchChunk.File.WriteByte(0x00);
				}
				scratchChunk.Address += size;
				scratchChunk.FreeBytes -= size;
				return new ReservedMemchunk() { Size = size, AddressRom = addressRom, AddressRam = addressRam };
			} else {
				throw new Exception("failed to find scratch space, should not happen");
			}
		}
	}

	internal class ReservedMemchunk {
		public uint Size;
		public uint AddressRom;
		public uint AddressRam;
	}
}
