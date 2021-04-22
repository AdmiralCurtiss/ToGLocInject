using HyoutaTools.Tales.Vesperia.FPS4;
using HyoutaUtils;
using HyoutaUtils.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToGLocInject {
	public static class Undub {
		// TODO:
		// - there's a handful of .ani/.chr files that are different between the versions for unclear reasons, figure out if those need to be swapped?
		// - fix the skit timing
		// - fix the timing in the fully animated cutscenes (RTS.nub)
		// - figure out whether it's more correct to copy the skt/vav files or inject just the audio subfile
		//   (note that this only matters for the five files from skt067 to skt071, all others are identical except for the audio already)

		public enum UndubVersion {
			JpVoicesToUs,
			JpVoicesToEu,
		}

		private static void CreateDirectory(string outdir) {
			if (!Directory.Exists(outdir)) {
				Console.WriteLine(string.Format("creating empty directory {0}", outdir));
				Directory.CreateDirectory(outdir);
			}
		}

		private static void CopyFileOrDirectory(string outdir, FileSystemInfo file) {
			Console.WriteLine(string.Format("copying {0} to {1}", file.FullName, outdir));
			if (file is DirectoryInfo) {
				string subdir = Path.Combine(outdir, file.Name);
				CreateDirectory(subdir);
				foreach (var fsi in new DirectoryInfo(file.FullName).EnumerateFileSystemInfos()) {
					CopyFileOrDirectory(subdir, fsi);
				}
			} else {
				File.Copy(file.FullName, Path.Combine(outdir, file.Name));
			}
		}

		private static void WriteStream(string outfile, Stream stream) {
			Console.WriteLine(string.Format("writing {0}", outfile));
			using (var fs = new FileStream(outfile, FileMode.Create)) {
				stream.Position = 0;
				StreamUtils.CopyStream(stream, fs);
			}
		}

		private static void WriteCpk(string outfile, HyoutaTools.Tales.CPK.CpkBuilder cpk) {
			Console.WriteLine(string.Format("writing {0}", outfile));
			using (var fs = new FileStream(outfile, FileMode.Create)) {
				cpk.Build(fs);

				// ToGf is buggy and fails to boot if bit 31 of the file length is set
				// (probably something somewhere is interpreted incorrectly as a signed 32 bit integer)
				// so work around that
				long length = fs.Length;
				if ((length % 0x100000000L) >= 0x80000000L) {
					fs.WriteAlign(0x80000000L);
				}
			}
		}

		public static void GenerateUndub(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			CreateDirectory(outdir);

			foreach (var fsi in new DirectoryInfo(datadir).EnumerateFileSystemInfos()) {
				if (fsi.Name == "PS3_GAME") {
					GenerateUndubPS3_GAME(Path.Combine(datadir, fsi.Name), Path.Combine(voicedir, fsi.Name), Path.Combine(outdir, fsi.Name), undubVersion);
				} else {
					CopyFileOrDirectory(outdir, fsi);
				}
			}
		}

		private static void GenerateUndubPS3_GAME(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			CreateDirectory(outdir);

			foreach (var fsi in new DirectoryInfo(datadir).EnumerateFileSystemInfos()) {
				if (fsi.Name == "USRDIR") {
					GenerateUndubUSRDIR(Path.Combine(datadir, fsi.Name), Path.Combine(voicedir, fsi.Name), Path.Combine(outdir, fsi.Name), undubVersion);
				} else {
					CopyFileOrDirectory(outdir, fsi);
				}
			}
		}

		private static void GenerateUndubUSRDIR(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			CreateDirectory(outdir);

			foreach (var fsi in new DirectoryInfo(datadir).EnumerateFileSystemInfos()) {
				if (fsi.Name == "movie") {
					// video data is identical across versions, only audio is different, so just copy JP files here
					CopyFileOrDirectory(outdir, new DirectoryInfo(Path.Combine(voicedir, fsi.Name)));
				} else if (fsi.Name == "map0R.cpk") {
					GenerateUndubMap0(datadir, voicedir, outdir, undubVersion);
				} else if (fsi.Name == "map1R.cpk") {
					GenerateUndubMap1(datadir, voicedir, outdir, undubVersion);
				} else if (fsi.Name == "rootR.cpk") {
					GenerateUndubRoot(datadir, voicedir, outdir, undubVersion);
				} else {
					CopyFileOrDirectory(outdir, fsi);
				}
			}
		}

		class SubcpkInjectData {
			public string Name;
			public string[] Subpaths;
		}

		private static void GenerateUndubMap01(string datadir, string voicedir, string outdir, string mapcpkname, SubcpkInjectData[] injects) {
			Console.WriteLine(string.Format("processing {0}", mapcpkname));
			var datastream = new DuplicatableFileStream(Path.Combine(datadir, mapcpkname));
			var voicestream = new DuplicatableFileStream(Path.Combine(voicedir, mapcpkname));
			var datacpk = new HyoutaTools.Tales.CPK.CpkContainer(datastream.Duplicate());
			var voicecpk = new HyoutaTools.Tales.CPK.CpkContainer(voicestream);
			string outfile = Path.Combine(outdir, mapcpkname);
			var builder = new HyoutaTools.Tales.CPK.CpkBuilder(datastream.Duplicate());

			foreach (var subdata in injects) {
				var subbuilder = new HyoutaTools.Tales.CPK.CpkBuilder(datacpk.GetChildByName(subdata.Name).AsFile.DataStream);
				var subvoicecpk = new HyoutaTools.Tales.CPK.CpkContainer(voicecpk.GetChildByName(subdata.Name).AsFile.DataStream);
				foreach (string subpath in subdata.Subpaths) {
					Console.WriteLine(string.Format("injecting {0}/{1}", subdata.Name, subpath));
					var subvoicestream = subvoicecpk.GetChildByName(subpath).AsFile.DataStream.Duplicate();
					var subfile = subbuilder.Files.Where(x => (x.Directory + "/" + x.Name) == subpath).First();
					subfile.FileStream = subvoicestream.CopyToByteArrayStreamAndDispose();
					subfile.DecompressedSize = (uint)subfile.FileStream.Length;
				}
				MemoryStream subbuild = new MemoryStream();
				subbuilder.Build(subbuild);
				var file = builder.Files.Where(x => x.Name == subdata.Name).First();
				file.FileStream = subbuild.CopyToByteArrayStreamAndDispose();
				file.DecompressedSize = (uint)file.FileStream.Length;
			}

			CreateDirectory(outdir);
			WriteCpk(outfile, builder);
		}

		private static void GenerateUndubMap0(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			GenerateUndubMap01(datadir, voicedir, outdir, "map0R.cpk", new SubcpkInjectData[] {
				new SubcpkInjectData() { Name = "mapfile_bridR.cpk", Subpaths = new string[] { "snd/se3/DYC_E314_060A.se3" } },
				new SubcpkInjectData() { Name = "mapfile_wf01R.cpk", Subpaths = new string[] { "snd/se3/DYC_E210_150.se3", "snd/se3/DYC_WF01.se3" } },
				new SubcpkInjectData() { Name = "mapfile_wf04R.cpk", Subpaths = new string[] { "snd/se3/DYC_E208_020.se3" } },
			});
		}

		private static void GenerateUndubMap1(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			GenerateUndubMap01(datadir, voicedir, outdir, "map1R.cpk", new SubcpkInjectData[] {
				new SubcpkInjectData() { Name = "mapfile_anmaR.cpk", Subpaths = new string[] { "snd/se3/DYC_S455_001.se3" } },
				new SubcpkInjectData() { Name = "mapfile_beraR.cpk", Subpaths = new string[] { "snd/se3/DYC_S408_003.se3" } },
				new SubcpkInjectData() { Name = "mapfile_lanR.cpk", Subpaths = new string[] { "snd/se3/DYC_E208_010.se3", "snd/se3/DYC_E210_061.se3" } },
				new SubcpkInjectData() { Name = "mapfile_otheR.cpk", Subpaths = new string[] { "snd/se3/DYC_E944_012.se3" } },
				new SubcpkInjectData() { Name = "mapfile_sablR.cpk", Subpaths = new string[] { "snd/se3/DYC_E419_030.se3" } },
				new SubcpkInjectData() { Name = "mapfile_winR.cpk", Subpaths = new string[] { "snd/se3/DYC_E104_010.se3" } },
			});
		}

		private static void GenerateUndubRoot(string datadir, string voicedir, string outdir, UndubVersion undubVersion) {
			Console.WriteLine(string.Format("processing {0}", "rootR.cpk"));
			var datastream = new DuplicatableFileStream(Path.Combine(datadir, "rootR.cpk"));
			var voicestream = new DuplicatableFileStream(Path.Combine(voicedir, "rootR.cpk"));
			var datacpk = new HyoutaTools.Tales.CPK.CpkContainer(datastream.Duplicate());
			var voicecpk = new HyoutaTools.Tales.CPK.CpkContainer(voicestream);
			string outfile = Path.Combine(outdir, "rootR.cpk");
			var builder = new HyoutaTools.Tales.CPK.CpkBuilder(datastream.Duplicate());

			// audio containers: we can direct-copy these, no need to un/repack
			foreach (string name in new string[] { "RTS.nub", "VOBTL.nub", "VOBTLETC.nub", "VOCHT.nub", "VOSCE01.nub", "VOSCE02.nub", "VOSCE03.nub", "VOSCE04.nub", "VOSCE05.nub", "VOSCE06.nub", "VOSCE07.nub", "VOSCE08.nub", "VOSCE09.nub", "VOSCE15.nub", "VOSCE16.nub" }) {
				string subdir = "snd/strpck";
				string subpath = subdir + "/" + name;
				Console.WriteLine(string.Format("injecting {0}", subpath));
				var subfile = builder.Files.Where(x => x.Directory == subdir && x.Name == name).First();
				subfile.FileStream = voicecpk.GetChildByName(subpath).AsFile.DataStream.Duplicate().CopyToByteArrayStreamAndDispose();
				subfile.DecompressedSize = (uint)subfile.FileStream.Length;
				subfile.WriteOrderPriority = 1;
			}

			// skits: for all of these unpack them (FPS4 containers), copy over file with index 1, repack
			// some of them are mistimed now because of altered timing for english skits, this could be refined...
			for (long i = 0; i < datacpk.toc_entries; ++i) {
				var entry = datacpk.GetEntryByIndex(i);
				if (entry != null && entry.dir_name == "chat/chd" && entry.file_name.EndsWith(".chd") && entry.file_name != "debug_02.chd") {
					// for EU undub also exclude CHT_PR*.chd because those files are not on the EU disc (they look unused on US too...)
					if (!(undubVersion == UndubVersion.JpVoicesToEu && entry.file_name.StartsWith("CHT_PR"))) {
						string subpath = entry.dir_name + "/" + entry.file_name;
						Console.WriteLine(string.Format("injecting {0}", subpath));
						var skitstreamen = datacpk.GetChildByIndex(i).AsFile.DataStream;
						var skitstreamjp = voicecpk.GetChildByName(subpath).AsFile.DataStream;
						var fps4en = new FPS4(skitstreamen);
						var fps4jp = new FPS4(skitstreamjp);

						List<PackFileInfo> packFileInfos = new List<PackFileInfo>(fps4en.Files.Count - 1);
						for (int j = 0; j < fps4en.Files.Count - 1; ++j) {
							var pf = new PackFileInfo();
							pf.Name = fps4en.Files[j].FileName;
							if (j == 1) {
								pf.DataStream = fps4jp.GetChildByIndex(j).AsFile.DataStream.Duplicate();
							} else {
								pf.DataStream = fps4en.GetChildByIndex(j).AsFile.DataStream.Duplicate();
							}
							pf.Length = pf.DataStream.Length;
							packFileInfos.Add(pf);
						}
						packFileInfos = FPS4.DetectDuplicates(packFileInfos);
						MemoryStream newfps4stream = new MemoryStream();
						FPS4.Pack(packFileInfos, newfps4stream, fps4en.ContentBitmask, EndianUtils.Endianness.BigEndian, fps4en.Unknown2, null, fps4en.ArchiveName, fps4en.FirstFileStart, 0x20);
						newfps4stream.Position = 0;

						var subfile = builder.Files.Where(x => x.Directory == entry.dir_name && x.Name == entry.file_name).First();
						subfile.FileStream = newfps4stream.CopyToByteArrayStreamAndDispose();
						subfile.DecompressedSize = (uint)subfile.FileStream.Length;
						subfile.WriteOrderPriority = 10;
					}
				}
			}

			// post-battle skits/quotes
			for (long i = 0; i < datacpk.toc_entries; ++i) {
				var entry = datacpk.GetEntryByIndex(i);
				if (entry != null && entry.dir_name == "btl/acf" && (
					   (entry.file_name.StartsWith("skt") && entry.file_name.EndsWith(".acf") && entry.file_name != "skt000.acf")
					|| (entry.file_name.StartsWith("vav") && entry.file_name.EndsWith(".acf") && entry.file_name != "vav000.acf")
				)) {
					string subpath = entry.dir_name + "/" + entry.file_name;
					Console.WriteLine(string.Format("injecting {0}", subpath));
					var subfile = builder.Files.Where(x => x.Directory == entry.dir_name && x.Name == entry.file_name).First();
					subfile.FileStream = voicecpk.GetChildByName(subpath).AsFile.DataStream.Duplicate().CopyToByteArrayStreamAndDispose();
					subfile.DecompressedSize = (uint)subfile.FileStream.Length;
				}
			}

			CreateDirectory(outdir);
			WriteCpk(outfile, builder);
		}
	}
}
