using System;
using System.Collections.Generic;

namespace ToGLocInject {
	public static class Util {
		public static byte GetColorComponent(this System.Drawing.Color color, int component) {
			switch (component) {
				case 0: return color.R;
				case 1: return color.G;
				case 2: return color.B;
				case 3: return color.A;
			}
			throw new Exception("Wrong component index.");
		}
	}

	internal enum Version {
		J, U, E, W, Wv0
	}

	internal class E {
		public int where;
		public List<int> entries;

		public E(int beforeWhichEntry, int entry) {
			this.where = beforeWhichEntry;
			this.entries = new List<int> { entry };
		}
		public E(int beforeWhichEntry, List<int> entries) {
			this.where = beforeWhichEntry;
			this.entries = entries;
		}
	}

	internal class M {
		public List<E> Adds = new List<E>();
		public List<(int entry, char character, bool keepChar, int maxSplits)> FeedSplits = new List<(int entry, char character, bool keepChar, int maxSplits)>();
		public List<(int entry, List<int> where)> PosSplits = new List<(int entry, List<int> where)>();
		public List<(int target, List<int> sources, string joiner, List<int> newlinesToRemove, List<int> newlinesToAdd)> Merges = new List<(int target, List<int> sources, string joiner, List<int> newlinesToRemove, List<int> newlinesToAdd)>();
		public List<int> Removes = new List<int>();
		public bool ReplaceEmptyStringsInsteadOfSkippingNegativeAdds = false;

		public M() { }
		public M(M m) {
			Adds = new List<E>(m.Adds);
		}
		public M(List<E> adds) {
			Adds = adds;
		}
		public M FeedSplit(int e, char ch = '\f', bool keepChar = false, int maxSplits = int.MaxValue) {
			FeedSplits.Add((e, ch, keepChar, maxSplits));
			return this;
		}
		public M PosSplit(int e, int pos) {
			PosSplits.Add((e, new List<int>() { pos }));
			return this;
		}
		public M PosSplit(int e, int p1, int p2) {
			PosSplits.Add((e, new List<int>() { p1, p2 }));
			return this;
		}
		public M A(E add) {
			Adds.Add(add);
			return this;
		}
		public M A(int beforeWhichEntry, int entry) {
			Adds.Add(new E(beforeWhichEntry, entry));
			return this;
		}
		public M A(int beforeWhichEntry, int entry1, int entry2) {
			Adds.Add(new E(beforeWhichEntry, new List<int>() { entry1, entry2 }));
			return this;
		}
		public M A(int beforeWhichEntry, int entry1, int entry2, int entry3) {
			Adds.Add(new E(beforeWhichEntry, new List<int>() { entry1, entry2, entry3 }));
			return this;
		}
		public M A(int beforeWhichEntry, int entry1, int entry2, int entry3, int entry4) {
			Adds.Add(new E(beforeWhichEntry, new List<int>() { entry1, entry2, entry3, entry4 }));
			return this;
		}
		public M A(int beforeWhichEntry, int entry1, int entry2, int entry3, int entry4, int entry5) {
			Adds.Add(new E(beforeWhichEntry, new List<int>() { entry1, entry2, entry3, entry4, entry5 }));
			return this;
		}
		public M A(int beforeWhichEntry, List<int> entries) {
			Adds.Add(new E(beforeWhichEntry, entries));
			return this;
		}
		public M Merge(int target, int toJoinInto, string joiner, List<int> newlinesToRemove = null, List<int> newlinesToAdd = null) {
			Merges.Add((target, new List<int>() { target, toJoinInto }, joiner, newlinesToRemove, newlinesToAdd));
			Removes.Add(toJoinInto);
			return this;
		}
		public M Merge(int target, int toJoinInto1, int toJoinInto2, string joiner, List<int> newlinesToRemove = null, List<int> newlinesToAdd = null) {
			Merges.Add((target, new List<int>() { target, toJoinInto1, toJoinInto2 }, joiner, newlinesToRemove, newlinesToAdd));
			Removes.Add(toJoinInto1);
			Removes.Add(toJoinInto2);
			return this;
		}
		public M Dup() {
			return new M(this);
		}
		public M EnableEmptyStrings() {
			ReplaceEmptyStringsInsteadOfSkippingNegativeAdds = true;
			return this;
		}
	}

	internal class W {
		public const int KEEP_ORIGINAL_MARK_MAPPED = -2;
		public const int KEEP_ORIGINAL_MARK_UNMAPPED = -1;
		public List<(int w, int u)> PrecalculatedReplacements = new List<(int w, int u)>(); // specific wii IDs and their matching US IDs
		public List<(int w, string u)> PrecalculatedReplacementsDirect = new List<(int w, string u)>();
		public delegate string PostProcessString(string original, string replacement);
		public List<(int w, PostProcessString func)> PostProcessing = new List<(int w, PostProcessString pps)>();

		public W R(int widx, int replaceWithUIdx) { // replace w entry with specific string from US PS3 file
			if (replaceWithUIdx < 0) {
				throw new Exception("invalid replace index");
			}
			ConfirmUnset(widx);
			PrecalculatedReplacements.Add((widx, replaceWithUIdx));
			return this;
		}

		public W Sys(int widx) { // mark as system string, if it automaps to a thing but really should be left alone, or just to reduce noise in the csvs
			ConfirmUnset(widx);
			PrecalculatedReplacements.Add((widx, KEEP_ORIGINAL_MARK_MAPPED));
			return this;
		}

		public W Un(int widx) { // mark as explicitly untranslated string, if it automaps to a thing it shouldn't
			ConfirmUnset(widx);
			PrecalculatedReplacements.Add((widx, KEEP_ORIGINAL_MARK_UNMAPPED));
			return this;
		}

		public W P(int widx, PostProcessString func) { // register function for postprocessing a string after everything else has been done
			PostProcessing.Add((widx, func));
			return this;
		}

		public W RP(int widx, int uidx, PostProcessString func) { // combines replacement with postprocessing in one convenient call!
			return R(widx, uidx).P(widx, func);
		}

		public W R(int widx, string replacement) { // replace w entry with custom string
			ConfirmUnset(widx);
			PrecalculatedReplacementsDirect.Add((widx, replacement));
			return this;
		}

		private void ConfirmUnset(int widx) {
			foreach (var a in PrecalculatedReplacements) {
				if (widx == a.w) {
					throw new Exception("multiple definitions for explicit string replacements");
				}
			}
			foreach (var a in PrecalculatedReplacementsDirect) {
				if (widx == a.w) {
					throw new Exception("multiple definitions for explicit string replacements");
				}
			}
		}
	}

	internal class MappingData {
		public bool Confirmed;
		public M J;
		public M U;
		public W W;
		public bool MultiplyOutSkit;
		public bool SkipTextMapping;
		public bool ReplaceInWiiV0;
		public MappingData(bool c = false, M j = null, M u = null, W w = null, bool multiplyOutSkit = false, bool skipTextMapping = false, bool replaceInWiiV0 = false) {
			Confirmed = c;
			J = j ?? new M();
			U = u ?? new M();
			W = w ?? new W();
			MultiplyOutSkit = multiplyOutSkit;
			SkipTextMapping = skipTextMapping;
			ReplaceInWiiV0 = replaceInWiiV0;
		}
	}

	internal enum MappingType { SelfMatch, CompMatch, JpOnly, EnOnly, NotMatched, DirectMatched, VoiceLineMatched }

	internal class MappedEntry {
		public int jpos;
		public string jp;
		public int upos;
		public string en;
		public MappingType type;

		public override string ToString() {
			return "[" + type + "] => " + jp + " / " + en;
		}
	}

	internal class MemChunk {
		public uint Address;
		public uint FreeBytes;
		public System.IO.MemoryStream File;
		public HyoutaPluginBase.IRomMapper Mapper;
		public bool IsInternal;

		public override string ToString() {
			return "0x" + FreeBytes.ToString("X") + " free bytes at 0x" + Address.ToString("X");
		}
	}

	internal class SJisString {
		public byte[] Data;
		public SJisString(byte[] data) {
			Data = data;
		}

		public override bool Equals(object obj) {
			SJisString other = obj as SJisString;
			if (other == null) {
				return false;
			}
			if (Data.Length != other.Data.Length) {
				return false;
			}
			for (int i = 0; i < Data.Length; ++i) {
				if (Data[i] != other.Data[i]) {
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode() {
			int v = Data.Length;
			for (int i = 0; i < Data.Length; ++i) {
				v += Data[i];
			}
			return v;
		}
	}

	internal class Ps3ElfMapper : HyoutaPluginBase.IRomMapper {
		public bool TryMapRamToRom(ulong ramAddress, out ulong value) {
			value = ramAddress - 0x10000;
			return value < 0x881600;
		}

		public bool TryMapRomToRam(ulong romAddress, out ulong value) {
			value = romAddress + 0x10000;
			return true;
		}
	}
}
