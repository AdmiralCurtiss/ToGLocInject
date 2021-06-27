using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HyoutaPluginBase;
using HyoutaTools.Generic;
using HyoutaTools.Tales.CPK;
using HyoutaTools.Tales.Graces;
using HyoutaTools.Tales.Graces.SCS;
using HyoutaTools.Tales.Vesperia.FPS4;
using HyoutaUtils;
using HyoutaUtils.Streams;

namespace ToGLocInject {
	internal class DolTextEntry {
		public int DolIdx;
		public bool ForceInternal;
		public MainDolString DolString;
		public string Text;
		public SJisString sjis = null;
		public int ListIndex = -1;
		public uint bytecount { get { return ((uint)sjis.Data.Length) + 1; } }

		public List<DolTextEntry> Duplicates = null;

		public DolTextEntry IsSuffixOf = null;
	}

	internal static partial class FileProcessing {
		private static void InjectAllStrings(
			Config config, SCS wscs, List<MainDolString> doltext, MemoryStream ms, HyoutaTools.GameCube.Dol dol, MemchunkStorage memchunks,
			List<(int index, string entry)> j, List<(int index, string entry)> u
		) {
			List<string> failedToFinds = new List<string>();
			long requiredExtraBytes = 0;

			List<DolTextEntry> dolTextEntries = new List<DolTextEntry>();
			foreach ((int dolidx, bool forceInternal) in GenerateDoltextInjectOrder(doltext)) {
				dolTextEntries.Add(new DolTextEntry() { DolIdx = dolidx, ForceInternal = forceInternal });
			}

			List<DolTextEntry> dolTextEntriesWithText = new List<DolTextEntry>();
			Dictionary<SJisString, int> alreadyEncounteredStrings = new Dictionary<SJisString, int>();
			for (int i = 0; i < dolTextEntries.Count; ++i) {
				DolTextEntry e = dolTextEntries[i];
				e.DolString = doltext[e.DolIdx];
				e.Text = wscs.Entries[e.DolIdx];
				if (e.Text == null) {
					// if text is null write a nullptr to the rom
					ms.Position = e.DolString.RomPointerPosition;
					ms.WriteUInt32(0);
				} else {
					byte[] inject = TextUtils.StringToBytesShiftJis(e.Text);
					e.sjis = new SJisString(inject);

					int idxx;
					if (alreadyEncounteredStrings.TryGetValue(e.sjis, out idxx)) {
						var de = dolTextEntriesWithText[idxx];
						if (de.Duplicates == null) {
							de.Duplicates = new List<DolTextEntry>();
						}
						de.Duplicates.Add(e);
					} else {
						e.ListIndex = dolTextEntriesWithText.Count;
						dolTextEntriesWithText.Add(e);
						alreadyEncounteredStrings.Add(e.sjis, e.ListIndex);
					}
				}
			}

			{
				List<DolTextEntry> ordered = dolTextEntriesWithText.OrderByDescending(x => x.bytecount).ToList();
				for (int i = 0; i < ordered.Count; ++i) {
					DolTextEntry eshort = ordered[i];
					byte[] shortdata = eshort.sjis.Data;
					for (int k = 0; k < i; ++k) {
						DolTextEntry elong = ordered[k];
						if (eshort.ForceInternal != elong.ForceInternal) {
							continue;
						}

						byte[] longdata = elong.sjis.Data;
						if (longdata.Length == 0 || shortdata.Length == 0) {
							continue;
						}
						if (longdata.Length > shortdata.Length && IsSuffixOf(longdata, shortdata)) {
							if (eshort.IsSuffixOf != null) {
								Console.WriteLine("this shouldn't happen, i'm confused");
							}
							eshort.IsSuffixOf = elong;
							break;
						}
					}
				}
			}

			List<DolTextEntry> dolTextEntriesWithTextSuffixCleaned = new List<DolTextEntry>();
			for (int i = 0; i < dolTextEntriesWithText.Count; ++i) {
				DolTextEntry e = dolTextEntriesWithText[i];
				if (e.IsSuffixOf != null) {
					DolTextEntry curr = e;
					DolTextEntry next = e.IsSuffixOf;
					while (next != null) {
						curr = next;
						next = curr.IsSuffixOf;
					}

					if (curr.Duplicates == null) {
						curr.Duplicates = new List<DolTextEntry>();
					}
					if (e.Duplicates != null) {
						curr.Duplicates.AddRange(e.Duplicates);
					}
					curr.Duplicates.Add(e);
				} else {
					dolTextEntriesWithTextSuffixCleaned.Add(e);
				}
			}

			var injected = new List<(uint location, uint writtenAddress)>();
			for (int i = 0; i < dolTextEntriesWithTextSuffixCleaned.Count; ++i) {
				DolTextEntry e = dolTextEntriesWithTextSuffixCleaned[i];

				uint address;
				MemChunk chunk = memchunks.FindBlock(e.bytecount, e.ForceInternal);
				if (chunk != null) {
					address = chunk.Mapper.MapRomToRam(chunk.Address);
					chunk.File.Position = chunk.Address;
					chunk.File.Write(e.sjis.Data);
					chunk.File.WriteByte(0);
					memchunks.TakeBytes(chunk, e.bytecount);
				} else {
					Console.WriteLine("ERROR: Failed to find free space for string " + e.Text);
					failedToFinds.Add("ERROR: Failed to find free space for string " + e.Text);
					requiredExtraBytes += e.bytecount;
					address = dol.MapRomToRam(0x4D2828u); // point at a default string instead
				}

				InjectStringPointerAt(ms, e, address, injected);

				if (e.Duplicates != null) {
					foreach (DolTextEntry de in e.Duplicates) {
						InjectStringPointerAt(ms, de, address + (e.bytecount - de.bytecount), injected);
					}
				}
			}

			if (config.DebugTextOutputPath != null) {
				Directory.CreateDirectory(config.DebugTextOutputPath);
				List<string> tmp = new List<string>();
				for (int dolidx = 0; dolidx < doltext.Count; ++dolidx) {
					var d = doltext[dolidx];
					string t = wscs.Entries[dolidx];
					StringBuilder sbj = new StringBuilder();
					StringBuilder sbe = new StringBuilder();
					sbj.Append("[" + dolidx.ToString().PadLeft(5) + "/0x" + d.RomPointerPosition.ToString("X8") + "] ");
					sbe.Append("[" + dolidx.ToString().PadLeft(5) + "/0x" + d.RomPointerPosition.ToString("X8") + "] ");
					if (d.Text != null) {
						sbj.Append(ReduceToSingleLine(d.Text));
					}
					if (t != null) {
						sbe.Append(ReduceToSingleLine(t));
					}
					tmp.Add(sbj.ToString());
					tmp.Add(sbe.ToString());
					tmp.Add("");
				}
				File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "maindol_mappings.txt"), tmp);
			}
			if (config.DebugTextOutputPath != null) {
				Directory.CreateDirectory(config.DebugTextOutputPath);
				List<string> tmp = new List<string>();
				for (int juidx = 0; juidx < u.Count; ++juidx) {
					string sj = j[juidx].entry;
					string su = u[juidx].entry;
					StringBuilder sbj = new StringBuilder();
					StringBuilder sbe = new StringBuilder();
					sbj.Append("[" + juidx.ToString().PadLeft(5) + "] ");
					sbe.Append("[" + juidx.ToString().PadLeft(5) + "] ");
					if (sj != null) {
						sbj.Append(ReduceToSingleLine(sj));
					}
					if (su != null) {
						sbe.Append(ReduceToSingleLine(su));
					}
					tmp.Add(sbj.ToString());
					tmp.Add(sbe.ToString());
					tmp.Add("");
				}
				File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "bootelf_ju.txt"), tmp);
			}
			if (config.DebugTextOutputPath != null) {
				Directory.CreateDirectory(config.DebugTextOutputPath);
				failedToFinds.Add("Would need " + requiredExtraBytes + " extra bytes.");
				long unusedByteCount = 0;
				foreach (MemChunk mc in memchunks.GetChunks()) {
					unusedByteCount += mc.FreeBytes;
				}
				failedToFinds.Add("Have " + unusedByteCount + " bytes of unused space.");
				failedToFinds.Add("");
				failedToFinds.Add("Memchunk list: ");
				foreach (MemChunk mc in memchunks.GetChunks()) {
					failedToFinds.Add(string.Format("0x{0:x8}, {1} bytes left, {2}", mc.Address, mc.FreeBytes, mc.IsInternal ? "internal" : "external"));
				}
				File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "maindol_no_space_left.txt"), failedToFinds);
			}
			if (config.DebugTextOutputPath != null) {
				Directory.CreateDirectory(config.DebugTextOutputPath);
				List<string> tmp = new List<string>();
				foreach (var a in injected) {
					tmp.Add(string.Format("wrote at 0x{0:x8} the address 0x{1:x8}", a.location, a.writtenAddress));
				}
				File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "written_data.txt"), tmp);
			}
		}

		private static void InjectStringPointerAt(MemoryStream ms, DolTextEntry e, uint address, List<(uint location, uint writtenAddress)> injected) {
			injected.Add((e.DolString.RomPointerPosition, address));

			ms.Position = e.DolString.RomPointerPosition;
			ms.WriteUInt32(address, EndianUtils.Endianness.BigEndian);
		}

		private static bool IsSuffixOf(byte[] longdata, byte[] shortdata) {
			int diff = longdata.Length - shortdata.Length;
			for (int i = shortdata.Length - 1; i >= 0; --i) {
				if (shortdata[i] != longdata[diff + i]) {
					return false;
				}
			}
			return true;
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

	}
}
