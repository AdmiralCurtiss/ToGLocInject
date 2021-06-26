using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HyoutaUtils;
using HyoutaTools.Tales.CPK;

namespace ToGLocInject {
	internal class SubcpkData {
		public CpkContainer Cpk;
		public long SubcpkOffset;
		public long ContentTocOffset;
		public List<SubcpkFileInfo> Files;
	}

	internal class SubcpkFileInfo {
		public string name;
		public long fsizepos;
		public long esizepos;
		public long foffspos;
	}

	internal class FileInjector {
		// for debugging without producing output
		private bool DisableInjection = false;

		private CpkContainer Cpk;
		private Stream OutputStream;
		public long CurrentInjectionOffset { get; private set; }

		private Dictionary<string, SubcpkData> RemappedSubcpks;

		public FileInjector(CpkContainer cpk, string outpath, long injectionOffset) {
			if (DisableInjection) {
				return;
			}

			Cpk = cpk;
			if (outpath != null) {
				Directory.CreateDirectory(Path.GetDirectoryName(outpath));
				OutputStream = new FileStream(outpath, FileMode.Create);
			} else {
				OutputStream = new HyoutaUtils.Streams.MemoryStream64();
			}
			CurrentInjectionOffset = injectionOffset;

			using (Stream instream = cpk.DuplicateStream()) {
				instream.Position = 0;
				StreamUtils.CopyStream(instream, OutputStream, instream.Length);
			}
			EnsureAligned();

			RemappedSubcpks = new Dictionary<string, SubcpkData>();
		}

		private void EnsureAligned() {
			if (DisableInjection) {
				return;
			}

			long pos = OutputStream.Length;
			OutputStream.Position = pos;
			long alignedPos = NumberUtils.Align(pos, 32);
			while (pos < alignedPos) {
				OutputStream.WriteByte(0);
				++pos;
			}
			CurrentInjectionOffset = pos;
		}

		public void InjectFile(Stream generatedFile, string relativePath, CompressionStyle compressionStyle) {
			if (DisableInjection) {
				return;
			}

			// logic for injection into main cpk
			int idx = Cpk.GetChildIndexFromName(relativePath).Value;
			var fsize = Cpk.QueryChildInfoByIndex(idx, "FileSize");
			var esize = Cpk.QueryChildInfoByIndex(idx, "ExtractSize");
			var foffs = Cpk.QueryChildInfoByIndex(idx, "FileOffset");

			// maybe compress
			OutputStream.Position = fsize.data_position;
			uint oldfsize = OutputStream.ReadUInt32(EndianUtils.Endianness.BigEndian);
			OutputStream.Position = esize.data_position;
			uint oldesize = OutputStream.ReadUInt32(EndianUtils.Endianness.BigEndian);
			Stream maybeCompressedFile = MaybeCompress(generatedFile, oldfsize, oldesize, compressionStyle, relativePath);

			// inject data
			maybeCompressedFile.Position = 0;
			uint newFilesize = (uint)maybeCompressedFile.Length;
			uint newExtractsize = (uint)generatedFile.Length;
			long newFileoffs = CurrentInjectionOffset;
			OutputStream.Position = newFileoffs;
			StreamUtils.CopyStream(maybeCompressedFile, OutputStream, maybeCompressedFile.Length);

			// update header
			CurrentInjectionOffset = OutputStream.Position;
			OutputStream.Position = fsize.data_position;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = esize.data_position;
			OutputStream.WriteUInt32(newExtractsize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = foffs.data_position;
			OutputStream.WriteUInt64(((ulong)(newFileoffs - Math.Min(Cpk.content_offset, Cpk.toc_offset))).ToEndian(EndianUtils.Endianness.BigEndian));
			EnsureAligned();
		}

		private SubcpkData GetOrCreateSubcpkData(string subcpkPath) {
			SubcpkData data;
			if (RemappedSubcpks.TryGetValue(subcpkPath, out data)) {
				return data;
			}

			int subcpkidx = Cpk.GetChildIndexFromName(subcpkPath).Value;
			var subcpkoffs = Cpk.QueryChildInfoByIndex(subcpkidx, "FileOffset");
			var subcpkstream = Cpk.GetChildByIndex(subcpkidx).AsFile.DataStream;
			var subcpk = new CpkContainer(subcpkstream.Duplicate());
			long subcpkOffsetInMainCpk = (long)subcpkoffs.value_u64 + Math.Min(Cpk.content_offset, Cpk.toc_offset);

			// header goes from start of file to content_offset in all of these
			long headerInjectedOffset = CurrentInjectionOffset;
			long headerSize = subcpk.content_offset;
			OutputStream.Position = headerInjectedOffset;
			subcpkstream.Position = 0;
			StreamUtils.CopyStream(subcpkstream, OutputStream, headerSize);
			EnsureAligned();

			// and ToC is from toc_offset until the end of file
			long tocInjectedOffset = CurrentInjectionOffset;
			long tocSize = subcpkstream.Length - subcpk.toc_offset;
			OutputStream.Position = tocInjectedOffset;
			subcpkstream.Position = subcpk.toc_offset;
			StreamUtils.CopyStream(subcpkstream, OutputStream, tocSize);
			EnsureAligned();

			// fetch relevant metadata for all files and repoint the offsets
			List<SubcpkFileInfo> files = new List<SubcpkFileInfo>((int)subcpk.toc_entries);
			long shift = subcpkOffsetInMainCpk - headerInjectedOffset;
			for (int i = 0; i < subcpk.toc_entries; ++i) {
				var name = subcpk.GetChildNameFromIndex(i);
				var fsize = subcpk.QueryChildInfoByIndex(i, "FileSize");
				var esize = subcpk.QueryChildInfoByIndex(i, "ExtractSize");
				var foffs = subcpk.QueryChildInfoByIndex(i, "FileOffset");
				long fsizepos = tocInjectedOffset + (fsize.data_position - subcpk.toc_offset);
				long esizepos = tocInjectedOffset + (esize.data_position - subcpk.toc_offset);
				long foffspos = tocInjectedOffset + (foffs.data_position - subcpk.toc_offset);
				OutputStream.Position = foffspos;
				long newFileoffs = (long)foffs.value_u64 + shift;
				OutputStream.WriteUInt64(((ulong)newFileoffs).ToEndian(EndianUtils.Endianness.BigEndian));
				files.Add(new SubcpkFileInfo() { name = name, fsizepos = fsizepos, esizepos = esizepos, foffspos = foffspos });
			}

			// repoint offsets to be correct
			List<string> offsetnames = new List<string>() { "ContentOffset", "TocOffset", "EtocOffset", "ItocOffset", "GtocOffset" };
			foreach (string offsetname in offsetnames) {
				var result = subcpk.QueryInfo(offsetname);
				long value = (long)result.value_u64;
				long position = result.data_position;
				if (offsetname == "ContentOffset") {
					// hack to force ContentOffset and TocOffset to be the same
					value = subcpk.toc_offset;
				}
				if (value != 0) {
					long newOffset = (value - subcpk.toc_offset) + (tocInjectedOffset - headerInjectedOffset);
					OutputStream.Position = headerInjectedOffset + position;
					OutputStream.WriteUInt64(((ulong)newOffset).ToEndian(EndianUtils.Endianness.BigEndian));
				}
			}

			// repoint file offset in main cpk to copied headers
			long newOffsetOfSubcpkInMaincpk = headerInjectedOffset - Math.Min(Cpk.content_offset, Cpk.toc_offset);
			OutputStream.Position = subcpkoffs.data_position;
			OutputStream.WriteUInt64(((ulong)newOffsetOfSubcpkInMaincpk).ToEndian(EndianUtils.Endianness.BigEndian));

			// remember and return
			OutputStream.Position = CurrentInjectionOffset;
			data = new SubcpkData() { Cpk = subcpk, SubcpkOffset = headerInjectedOffset, ContentTocOffset = tocInjectedOffset - headerInjectedOffset, Files = files };
			RemappedSubcpks.Add(subcpkPath, data);
			return data;
		}

		public void InjectFileSubcpk(Stream generatedFile, string subcpkPath, string relativePath, CompressionStyle compressionStyle) {
			if (DisableInjection) {
				return;
			}

			// logic for injection into subcpk

			SubcpkData data = GetOrCreateSubcpkData(subcpkPath);
			var filedata = data.Files.Where(x => x.name == relativePath).First();

			// maybe compress
			OutputStream.Position = filedata.fsizepos;
			uint oldfsize = OutputStream.ReadUInt32(EndianUtils.Endianness.BigEndian);
			OutputStream.Position = filedata.esizepos;
			uint oldesize = OutputStream.ReadUInt32(EndianUtils.Endianness.BigEndian);
			Stream maybeCompressedFile = MaybeCompress(generatedFile, oldfsize, oldesize, compressionStyle, subcpkPath + "/" + relativePath);

			// copy file into output stream
			maybeCompressedFile.Position = 0;
			uint newFilesize = (uint)maybeCompressedFile.Length;
			uint newExtractsize = (uint)generatedFile.Length;
			long newFileoffs = CurrentInjectionOffset;
			OutputStream.Position = newFileoffs;
			StreamUtils.CopyStream(maybeCompressedFile, OutputStream, maybeCompressedFile.Length);
			EnsureAligned();

			// fix sizes and offset
			OutputStream.Position = filedata.fsizepos;
			OutputStream.WriteUInt32(newFilesize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = filedata.esizepos;
			OutputStream.WriteUInt32(newExtractsize.ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = filedata.foffspos;
			OutputStream.WriteUInt64(((ulong)(newFileoffs - (data.SubcpkOffset + data.ContentTocOffset))).ToEndian(EndianUtils.Endianness.BigEndian));
			OutputStream.Position = CurrentInjectionOffset;
		}

		private static Stream MaybeCompress(Stream generatedFile, uint oldfsize, uint oldesize, CompressionStyle compressionStyle, string filename) {
			if (!ShouldCompress(generatedFile, oldfsize, oldesize, compressionStyle, filename)) {
				Console.WriteLine("Injecting {0} uncompressed...", filename);
				return generatedFile;
			}

			try {
				Console.WriteLine("Injecting {0} compressed...", filename);
				MemoryStream ms = new MemoryStream();
				utf_tab_sharp.CpkCompress.compress(generatedFile, 0, generatedFile.Length, ms);
				if (generatedFile.Length <= ms.Length) {
					Console.WriteLine("Compression didn't reduce size, using uncompressed instead...", filename);
					return generatedFile;
				}
				return ms;
			} catch (Exception ex) {
				Console.WriteLine("Error while compressing, injecting uncompressed instead: " + ex.ToString());
				return generatedFile;
			}
		}

		private static bool ShouldCompress(Stream generatedFile, uint oldfsize, uint oldesize, CompressionStyle compressionStyle, string filename) {
			if (generatedFile.Length <= 0x100) {
				return false;
			}

			switch (compressionStyle) {
				case CompressionStyle.NeverCompress:
					return false;
				case CompressionStyle.AlwaysCompress:
					return true;
				case CompressionStyle.CompressIfOriginallyCompressed:
					return oldfsize != oldesize;
				case CompressionStyle.CompressIfOriginallyCompressedPlus:
					return oldfsize != oldesize
					    || (filename.StartsWith("movie/str/ja/TOG_") && filename.EndsWith(".bin"))
					    || (filename.StartsWith("chat/scs/JA/CHT_") && filename.EndsWith(".scs"));
				default:
					return false;
			}
		}

		public void GenerateRiivolutionData(StringBuilder xml, string outputPath, string fileOnDisc, bool isV2) {
			if (DisableInjection) {
				return;
			}

			string basename = Path.GetFileNameWithoutExtension(fileOnDisc);
			string dataname = string.Format("{0}_v{1}_data", basename, isV2 ? 2 : 0);
			long pos = Cpk.toc_offset;
			long len = OutputStream.Length - pos;
			OutputStream.Position = pos;
			using (FileStream fs = new FileStream(Path.Combine(outputPath, dataname), FileMode.Create)) {
				StreamUtils.CopyStream(OutputStream, fs, len);
			}
			xml.AppendFormat("\t\t<file disc=\"/{0}\" external=\"{1}\" resize=\"true\" create=\"false\" offset=\"0x{2:X8}\" length=\"0x{3:X8}\" />", fileOnDisc, dataname, pos, len);
			xml.AppendLine();
		}

		public void Close() {
			if (DisableInjection) {
				return;
			}

			OutputStream.Close();
		}

		public Stream RelinquishOutputStream() {
			var outstream = OutputStream;
			OutputStream = null;
			DisableInjection = true;
			return outstream;
		}
	}
}
