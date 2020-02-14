using System;
using System.IO;
using HyoutaUtils;
using HyoutaTools.Tales.CPK;

namespace ToGLocInject {
	internal class FileInjector {
		// for debugging without producing output
		private bool DisableInjection = false;

		private CpkContainer Cpk;
		private Stream OutputStream;
		public long CurrentInjectionOffset { get; private set; }

		public FileInjector(CpkContainer cpk, string outpath, long injectionOffset) {
			if (DisableInjection) {
				return;
			}

			Cpk = cpk;
			Directory.CreateDirectory(Path.GetDirectoryName(outpath));
			OutputStream = new FileStream(outpath, FileMode.Create);
			CurrentInjectionOffset = injectionOffset;

			using (Stream instream = cpk.DuplicateStream()) {
				instream.Position = 0;
				StreamUtils.CopyStream(instream, OutputStream, instream.Length);
			}
			EnsureAligned();
		}

		private void EnsureAligned() {
			if (DisableInjection) {
				return;
			}

			OutputStream.Position = OutputStream.Length;
			CurrentInjectionOffset = NumberUtils.Align(CurrentInjectionOffset, 32);
			while (OutputStream.Position < CurrentInjectionOffset) {
				OutputStream.WriteByte(0);
			}
		}

		public void InjectFile(Stream generatedFile, string relativePath) {
			if (DisableInjection) {
				return;
			}

			// logic for injection into main cpk
			int idx = Cpk.GetChildIndexFromName(relativePath).Value;
			var fsize = Cpk.QueryChildInfoByIndex(idx, "FileSize");
			var esize = Cpk.QueryChildInfoByIndex(idx, "ExtractSize");
			var foffs = Cpk.QueryChildInfoByIndex(idx, "FileOffset");

			generatedFile.Position = 0;
			uint newFilesize = (uint)generatedFile.Length;
			long newFileoffs = CurrentInjectionOffset;
			OutputStream.Position = newFileoffs;
			StreamUtils.CopyStream(generatedFile, OutputStream, generatedFile.Length);
			CurrentInjectionOffset = OutputStream.Position;
			OutputStream.Position = fsize.data_position;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = esize.data_position;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = foffs.data_position;
			OutputStream.WriteUInt64(((ulong)(newFileoffs - Math.Min(Cpk.content_offset, Cpk.toc_offset))).ToEndian(EndianUtils.Endianness.BigEndian));
			EnsureAligned();
		}

		public void InjectFileSubcpk(Stream generatedFile, string subcpkPath, string relativePath) {
			if (DisableInjection) {
				return;
			}

			// logic for injection into subcpk
			int subcpkidx = Cpk.GetChildIndexFromName(subcpkPath).Value;
			var subcpkoffs = Cpk.QueryChildInfoByIndex(subcpkidx, "FileOffset");
			var subcpk = new CpkContainer(Cpk.GetChildByIndex(subcpkidx).AsFile.DataStream);
			int idx = subcpk.GetChildIndexFromName(relativePath).Value;
			var fsize = subcpk.QueryChildInfoByIndex(idx, "FileSize");
			var esize = subcpk.QueryChildInfoByIndex(idx, "ExtractSize");
			var foffs = subcpk.QueryChildInfoByIndex(idx, "FileOffset");

			long subcpkOffsetInMainCpk = (long)subcpkoffs.value_u64 + Math.Min(Cpk.content_offset, Cpk.toc_offset);

			generatedFile.Position = 0;
			uint newFilesize = (uint)generatedFile.Length;
			long newFileoffs = CurrentInjectionOffset;
			OutputStream.Position = newFileoffs;
			StreamUtils.CopyStream(generatedFile, OutputStream, generatedFile.Length);
			CurrentInjectionOffset = OutputStream.Position;
			OutputStream.Position = fsize.data_position + subcpkOffsetInMainCpk;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = esize.data_position + subcpkOffsetInMainCpk;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = foffs.data_position + subcpkOffsetInMainCpk;
			OutputStream.WriteUInt64(((ulong)(newFileoffs - (subcpkOffsetInMainCpk + Math.Min(subcpk.content_offset, subcpk.toc_offset)))).ToEndian(EndianUtils.Endianness.BigEndian));
			EnsureAligned();
		}

		public void Close() {
			if (DisableInjection) {
				return;
			}

			OutputStream.Close();
		}
	}
}
