using HyoutaPluginBase;
using HyoutaTools.Tales.Graces;
using HyoutaTools.Tales.Vesperia.NUB;
using HyoutaTools.Tales.Vesperia.SpkdUnpack;
using HyoutaUtils;
using HyoutaUtils.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ToGLocInject {
	public class VoiceInject {
		private static NubInfo[] Nubs = new NubInfo[] {
			new NubInfo() { Name = "VOCHT", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOBTLETC", WiiType = "dsp", WiiSampleRate = 24000, EngType = "vag" },
			new NubInfo() { Name = "VOBTL", WiiType = "dsp", WiiSampleRate = 24000, EngType = "vag" },
			new NubInfo() { Name = "VOSCE01", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE02", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE03", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE04", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE05", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE06", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE07", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE08", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
			new NubInfo() { Name = "VOSCE16", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3" },
		};

		public static ContainedVoiceInfo[] ContainedVoices = new ContainedVoiceInfo[] {
			new ContainedVoiceInfo() { BaseName = "btl/acf/vav{0:D3}.acf", StartNumber = 1, EndNumber = 97, SE3Index = 0, WiiType = "dsp", WiiSampleRate = 24000, EngType = "vag" },
			new ContainedVoiceInfo() { BaseName = "btl/acf/skt{0:D3}.acf", StartNumber = 1, EndNumber = 45, SE3Index = 0, WiiType = "dsp", WiiSampleRate = 24000, EngType = "vag" },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_MS{0:D3}.chd", StartNumber = 1, EndNumber = 78, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_MS{0:D3}.chd", StartNumber = 80, EndNumber = 183, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_MS{0:D3}.chd", StartNumber = 185, EndNumber = 242, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_SB{0:D3}.chd", StartNumber = 1, EndNumber = 36, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_SB{0:D3}.chd", StartNumber = 38, EndNumber = 58, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
			new ContainedVoiceInfo() { BaseName = "chat/chd/CHT_SB{0:D3}.chd", StartNumber = 60, EndNumber = 72, SE3Index = 1, WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", IsSkit = true },
		};

		private static void GenerateConversion(StringBuilder sb, string folder, string file, string ext, string targetType, int targetSampleRate) {
			sb.AppendFormat("test {0}\\{1}.{2} -o {0}\\{1}.wav", folder, file, ext).AppendLine();

			if (targetType == "bnsf") {
				sb.AppendFormat("ToGLocInject --prepare-raw-bnsf-from-wav {0}\\{1} {2}", folder, file, targetSampleRate).AppendLine();
				sb.AppendFormat("encode 0 {0}\\{1}.raw {0}\\{1}.rawbnsf 48000 14000", folder, file).AppendLine();
				sb.AppendFormat("del {0}\\{1}.wav", folder, file).AppendLine();
				sb.AppendFormat("del {0}\\{1}.raw", folder, file).AppendLine();
			} else if (targetType == "dsp") {
				sb.AppendFormat("gc-dspadpcm-encode {0}\\{1}.wav {0}\\{1}.dsp", folder, file).AppendLine();
				sb.AppendFormat("del {0}\\{1}.wav", folder, file).AppendLine();
			}
		}

		public static void Setup(Config config, string targetpath) {
			Directory.CreateDirectory(targetpath);
			var _fc = new FileFetcher(config);
			var rootW = _fc.TryGetContainer("rootR.cpk", Version.W);
			var rootU = _fc.TryGetContainer("rootR.cpk", Version.U);

			StringBuilder sb = new StringBuilder();

			var defstreamW = rootW.GetChildByName("snd/init/StrConfig.stp").AsFile.DataStream;
			var defstreamU = rootU.GetChildByName("snd/init/StrConfig.stp").AsFile.DataStream;
			var defW = new SPKD(defstreamW);
			var defU = new SPKD(defstreamU);
			List<SPKD.SpkdPackFileData> defWPack = defW.GetPackData();

			SHBP hashVobtletcU = null;
			iPck lipVobtletcU = null;
			NUB nubVobtletcU = null;
			foreach (NubInfo nubInfo in Nubs) {
				var hashStreamW = defW.GetChildByName(nubInfo.Name + ".1")?.AsFile.DataStream;
				var hashStreamU = defU.GetChildByName(nubInfo.Name + ".1")?.AsFile.DataStream;
				var lipStreamU = defU.GetChildByName(nubInfo.Name + ".2")?.AsFile.DataStream;
				DuplicatableStream lipStreamUDec = null;
				if (lipStreamU != null) {
					MemoryStream ms = new MemoryStream();
					compto.complib.DecodeStream(lipStreamU, ms, 0, 0, true);
					lipStreamUDec = ms.CopyToByteArrayStreamAndDispose();
					lipStreamUDec.Position = 0;
				}
				var nubStreamU = rootU.GetChildByName("snd/strpck/" + nubInfo.Name + ".nub").AsFile.DataStream;

				var hashW = hashStreamW != null ? new SHBP(hashStreamW) : null;
				var hashU = hashStreamU != null ? new SHBP(hashStreamU) : null;
				var lipU = lipStreamUDec != null ? new iPck(lipStreamUDec) : null;
				var nubU = new NUB(nubStreamU, EndianUtils.Endianness.BigEndian);

				if (nubInfo.Name == "VOBTLETC") {
					// the Wii VOBTL was split into two files VOBTL and VOBTLETC in PS3, so we need to do some stuff to put them back together
					hashVobtletcU = hashU;
					lipVobtletcU = lipU;
					nubVobtletcU = nubU;
					continue;
				}

				Console.WriteLine("Extracting files for " + nubInfo.Name + "...");

				List<NubFileRef> filesToExtract = new List<NubFileRef>();
				if (hashW != null && hashU != null) {
					Dictionary<uint, NubFileRef> hashToNubfileMap = new Dictionary<uint, NubFileRef>();
					for (int i = 0; i < hashU.Hashes.Count; ++i) {
						uint hash = hashU.Hashes[i];
						NubFileRef nfr = new NubFileRef() { Nub = nubU, Index = i, Lipsync = lipU.Data[i] };
						if (hashToNubfileMap.ContainsKey(hash)) {
							throw new Exception("duplicate hash key");
						}
						hashToNubfileMap.Add(hash, nfr);
					}

					if (nubInfo.Name == "VOBTL") {
						for (int i = 0; i < hashVobtletcU.Hashes.Count; ++i) {
							uint hash = hashVobtletcU.Hashes[i];
							NubFileRef nfr = new NubFileRef() { Nub = nubVobtletcU, Index = i, Lipsync = lipVobtletcU.Data[i] };
							if (hashToNubfileMap.ContainsKey(hash)) {
								throw new Exception("duplicate hash key");
							}
							hashToNubfileMap.Add(hash, nfr);
						}
					}

					for (int i = 0; i < hashW.Hashes.Count; ++i) {
						uint hash = hashW.Hashes[i];
						filesToExtract.Add(hashToNubfileMap[hash]);
					}
				} else {
					for (int i = 0; i < nubU.EntryCount; ++i) {
						filesToExtract.Add(new NubFileRef() { Nub = nubU, Index = i });
					}
				}

				List<byte[]> lipData = lipU != null ? new List<byte[]>() : null;

				string dir = Path.Combine(targetpath, nubInfo.Name);
				Directory.CreateDirectory(dir);
				for (int i = 0; i < filesToExtract.Count; ++i) {
					var nfr = filesToExtract[i];

					if (lipData != null) {
						lipData.Add(nfr.Lipsync);
					}

					using (var audiofilestream = nfr.Nub.GetChildByIndex(nfr.Index).AsFile.DataStream.Duplicate()) {
						string path = Path.Combine(dir, string.Format("{0:D8}.{1}", i, nubInfo.EngType));
						using (var fs = new FileStream(path, FileMode.Create)) {
							audiofilestream.Position = 0;
							StreamUtils.CopyStream(audiofilestream, fs);
						}
					}
					GenerateConversion(sb, nubInfo.Name, i.ToString("D8"), nubInfo.EngType, nubInfo.WiiType, nubInfo.WiiSampleRate);
				}

				if (lipData != null) {
					using (var newlipstream = new iPck(lipData).WriteToStream(EndianUtils.Endianness.BigEndian)) {
						newlipstream.Position = 0;
						using (MemoryStream ms = new MemoryStream()) {
							compto.complib.EncodeStream(newlipstream, ms, 0, 1, true);
							defWPack.First(x => x.Name == nubInfo.Name).File2 = ms.CopyToByteArrayStreamAndDispose();
						}
					}
				}
			}

			using (var newspkd = SPKD.Pack(defWPack))
			using (var fs = new FileStream(Path.Combine(targetpath, "StrConfig.stp"), FileMode.Create)) {
				newspkd.Position = 0;
				StreamUtils.CopyStream(newspkd, fs);
			}

			string otherdir = Path.Combine(targetpath, "other");
			Directory.CreateDirectory(otherdir);
			foreach (var cvi in ContainedVoices) {
				for (int i = cvi.StartNumber; i <= cvi.EndNumber; ++i) {
					string path = string.Format(cvi.BaseName, i);
					Console.WriteLine("Extracting " + path + "...");
					string fname = Path.GetFileNameWithoutExtension(path);
					var fps4stream = rootU.GetChildByName(path).AsFile.DataStream;
					var fps4 = new HyoutaTools.Tales.Vesperia.FPS4.FPS4(fps4stream);
					var se3stream = fps4.GetChildByIndex(cvi.SE3Index).AsFile.DataStream;
					var ms = new MemoryStream();
					new HyoutaTools.Tales.Vesperia.SE3.SE3(se3stream.Duplicate(), EndianUtils.Endianness.BigEndian, TextUtils.GameTextEncoding.ASCII).ExtractToNub(ms);
					var nub = new NUB(ms.CopyToByteArrayStreamAndDispose(), EndianUtils.Endianness.BigEndian);
					using (var audiofilestream = nub.GetChildByIndex(0).AsFile.DataStream.Duplicate()) {
						string otherpath = Path.Combine(otherdir, string.Format("{0}.{1}", fname, cvi.EngType));
						using (var fs = new FileStream(otherpath, FileMode.Create)) {
							audiofilestream.Position = 0;
							StreamUtils.CopyStream(audiofilestream, fs);
						}
					}
					GenerateConversion(sb, "other", fname, cvi.EngType, cvi.WiiType, cvi.WiiSampleRate);
				}
			}

			File.WriteAllText(Path.Combine(targetpath, "convert_voices.bat"), sb.ToString());

			return;
		}

		private class NubFileRef {
			public NUB Nub;
			public int Index;
			public byte[] Lipsync;

			public override string ToString() {
				return Index + "/" + Nub.EntryCount + " (lipsync: " + (Lipsync == null ? "no" : "yes") + ")";
			}
		};

		public static void PrepareRawBnsfFromWav(string path, int targetSampleRate) {
			try {
				using (var fs = new DuplicatableFileStream(path + ".wav")) {
					fs.Position = 0x16;
					ushort channels = fs.ReadUInt16(EndianUtils.Endianness.LittleEndian);
					uint samplerate = fs.ReadUInt32(EndianUtils.Endianness.LittleEndian);
					fs.Position = 0x2c;
					long samplecount = (fs.Length - 0x2c) / 2;
					long samplesPerChannel = samplecount / channels;
					short[,] samples = new short[channels, samplesPerChannel];

					for (long i = 0; i < samplesPerChannel; ++i) {
						for (long j = 0; j < channels; ++j) {
							samples[j, i] = fs.ReadInt16(EndianUtils.Endianness.LittleEndian);
						}
					}

					short[,] outsamples;
					long outSampleCountPerChannel;
					if (samplerate == targetSampleRate) {
						outSampleCountPerChannel = samplesPerChannel;
						outsamples = samples;
					} else if (targetSampleRate * 3 == samplerate * 2) {
						outSampleCountPerChannel = (samplesPerChannel.Align(3) * 2) / 3;
						outsamples = new short[channels, outSampleCountPerChannel];
						for (long ch = 0; ch < channels; ++ch) {
							long sourcePos = 0;
							for (long s = 0; s < outSampleCountPerChannel; s += 2) {
								int sample0 = sourcePos < samplesPerChannel ? samples[ch, sourcePos] : 0;
								++sourcePos;
								int sample1 = sourcePos < samplesPerChannel ? samples[ch, sourcePos] : sample0;
								++sourcePos;
								int sample2 = sourcePos < samplesPerChannel ? samples[ch, sourcePos] : sample1;
								++sourcePos;

								outsamples[ch, s] = (short)LerpTwoThirds(sample0, sample1);
								outsamples[ch, s + 1] = (short)LerpTwoThirds(sample2, sample1);
							}
						}
					} else {
						throw new Exception("unsupported sample rate conversion");
					}

					using (var nfs = new FileStream(path + ".raw", FileMode.Create)) {
						for (long ch = 0; ch < channels; ++ch) {
							for (long s = 0; s < outSampleCountPerChannel; ++s) {
								nfs.WriteInt16(outsamples[ch, s], EndianUtils.Endianness.LittleEndian);
							}
						}
					}
					using (var nfs = new FileStream(path + ".samplecount", FileMode.Create)) {
						nfs.WriteUInt64((ulong)outSampleCountPerChannel, EndianUtils.Endianness.LittleEndian);
					}

					return;
				}
			} catch (Exception ex) {
				System.IO.File.WriteAllText(path + ".error", ex.ToString());
			}
		}

		private static int LerpTwoThirds(int s0, int s1) {
			const int offset = 0x8000;
			int s0d = s0 + offset;
			int s1d = s1 + offset;
			int interpolated = ((2 * s0d) + s1d) / 3;
			return interpolated - offset;
		}

		internal static Stream InjectEnglishVoicesToWiiNub(Config config, FileFetcher _fc, string name, DuplicatableStream wstream, DuplicatableStream jstream, DuplicatableStream ustream) {
			NubInfo nub = Nubs.Where(x => x.Name == Path.GetFileNameWithoutExtension(name)).FirstOrDefault();
			string nubdir = Path.Combine(config.EnglishVoiceProcessingDir, nub.Name);
			return RebuildNubStream(wstream, nubdir, nub.WiiType, x => x.ToString("D8"));
		}

		delegate string GetFilenameDelegate(long i);

		private static Stream RebuildNubStream(DuplicatableStream wstream, string nubdir, string nubtype, GetFilenameDelegate getFilenameDelegate) {
			MemoryStream outstream = new MemoryStream();
			EndianUtils.Endianness e = EndianUtils.Endianness.BigEndian;
			using (var stream = wstream.Duplicate()) {
				stream.Position = 0;
				var header = new HyoutaTools.Tales.Vesperia.NUB.NubHeader(stream, e);
				stream.Position = 0;
				StreamUtils.CopyStream(stream, outstream, header.StartOfFiles);
				stream.Position = header.StartOfEntries;
				uint[] entries = stream.ReadUInt32Array(header.EntryCount, e);
				for (long i = 0; i < entries.LongLength; ++i) {
					uint entryLoc = entries[i];
					if (nubtype == "bnsf") {
						using (var bnsfstream = new DuplicatableFileStream(Path.Combine(nubdir, getFilenameDelegate(i) + ".rawbnsf")))
						using (var samplecountstream = new DuplicatableFileStream(Path.Combine(nubdir, getFilenameDelegate(i) + ".samplecount"))) {
							// write file to outstream
							long filestart = outstream.Position;
							StreamUtils.CopyStream(bnsfstream, outstream, bnsfstream.Length);
							outstream.WriteAlign(0x10);
							long fileend = outstream.Position;
							long filelen = fileend - filestart;

							// update headers
							outstream.Position = entryLoc + 0xbc + 0x4;
							outstream.WriteUInt32((uint)(bnsfstream.Length + 0x28), e);
							outstream.Position = entryLoc + 0xbc + 0x1c;
							outstream.WriteUInt32((uint)(samplecountstream.ReadUInt64(EndianUtils.Endianness.LittleEndian)), e);
							outstream.Position = entryLoc + 0xbc + 0x2c;
							outstream.WriteUInt32((uint)(bnsfstream.Length), e);

							outstream.Position = entryLoc + 0x14;
							outstream.WriteUInt32((uint)filelen, e);
							outstream.WriteUInt32((uint)(filestart - header.StartOfFiles), e);

							outstream.Position = fileend;
						}
					} else if (nubtype == "dsp") {
						using (var fs = new DuplicatableFileStream(Path.Combine(nubdir, getFilenameDelegate(i) + ".dsp"))) {
							byte[] dspheader = fs.ReadUInt8Array(0x60);

							// write file to outstream
							long filestart = outstream.Position;
							StreamUtils.CopyStream(fs, outstream, fs.Length - 0x60);
							outstream.WriteAlign(0x10);
							long fileend = outstream.Position;
							long filelen = fileend - filestart;

							// update headers
							outstream.Position = entryLoc + 0xbc;
							outstream.Write(dspheader);
							outstream.Position = entryLoc + 0x14;
							outstream.WriteUInt32((uint)filelen, e);
							outstream.WriteUInt32((uint)(filestart - header.StartOfFiles), e);

							outstream.Position = fileend;
						}
					}
				}

				long filesSize = outstream.Position - header.StartOfFiles;
				outstream.Position = 0x14;
				outstream.WriteUInt32((uint)filesSize, e);

				outstream.Position = 0;
				return outstream;
			}
		}

		internal static Stream InjectEnglishContainedVoice(Config config, FileFetcher _fc, string name, DuplicatableStream wstream, DuplicatableStream jstream, DuplicatableStream ustream, ContainedVoiceInfo cvi) {
			var fps4 = new HyoutaTools.Tales.Vesperia.FPS4.FPS4(wstream.Duplicate());
			var se3stream = fps4.GetChildByIndex(cvi.SE3Index).AsFile.DataStream;
			var nubms = new MemoryStream();
			var se3ms = new MemoryStream();
			var se3 = new HyoutaTools.Tales.Vesperia.SE3.SE3(se3stream.Duplicate(), EndianUtils.Endianness.BigEndian, TextUtils.GameTextEncoding.ASCII);
			se3.ExtractSE3Header(se3ms);
			se3.ExtractToNub(nubms);
			var nubstream = new DuplicatableByteArrayStream(nubms.CopyToByteArrayAndDispose());
			var newnubstream = RebuildNubStream(nubstream, Path.Combine(config.EnglishVoiceProcessingDir, "other"), cvi.WiiType, x => Path.GetFileNameWithoutExtension(name));
			var newse3stream = new MemoryStream();
			se3ms.Position = 0;
			newnubstream.Position = 0;
			StreamUtils.CopyStream(se3ms, newse3stream);
			StreamUtils.CopyStream(newnubstream, newse3stream);

			newse3stream.Position = 0;
			MemoryStream newfps4stream = new MemoryStream();
			using (var ufps4 = new HyoutaTools.Tales.Vesperia.FPS4.FPS4(ustream.Duplicate())) {
				List<HyoutaTools.Tales.Vesperia.FPS4.PackFileInfo> packFileInfos = new List<HyoutaTools.Tales.Vesperia.FPS4.PackFileInfo>(fps4.Files.Count);
				if (ufps4.Files.Count != fps4.Files.Count) {
					Console.WriteLine("WARNING: file count mismatch in " + name);
				}
				for (int i = 0; i < fps4.Files.Count - 1; ++i) {
					var pf = new HyoutaTools.Tales.Vesperia.FPS4.PackFileInfo();
					pf.Name = fps4.Files[i].FileName;
					if (i == cvi.SE3Index) {
						pf.DataStream = new DuplicatableByteArrayStream(newse3stream.CopyToByteArrayAndDispose());
					} else if (cvi.IsSkit && (i == 0 || i == 2 || i == 3)) {
						if (i == 3) {
							var aaa = ufps4.GetChildByIndex(i).AsFile.DataStream.Duplicate().CopyToByteArrayAndDispose();
							var bbb = fps4.GetChildByIndex(i).AsFile.DataStream.Duplicate().CopyToByteArrayAndDispose();
							if (!aaa.SequenceEqual(bbb)) {
								Console.WriteLine("WARNING: mismatch in texture ID data in " + name);
							}
						}

						// copy over the actual skit script/timing from the EN version so the voice timing and lipsync matches with the skit
						pf.DataStream = ufps4.GetChildByIndex(i).AsFile.DataStream.Duplicate();
					} else {
						pf.DataStream = fps4.GetChildByIndex(i).AsFile.DataStream;
					}
					pf.Length = pf.DataStream.Length;
					packFileInfos.Add(pf);
				}
				HyoutaTools.Tales.Vesperia.FPS4.FPS4.Pack(packFileInfos, newfps4stream, fps4.ContentBitmask, EndianUtils.Endianness.BigEndian, fps4.Unknown2, wstream.Duplicate(), fps4.ArchiveName, fps4.FirstFileStart, 0x20);
			}

			//using (var fs = new FileStream(Path.Combine(@"c:\__graces\______fps4repacktest\", name.Replace("/", "_") + "_old.fps4"), FileMode.Create)) {
			//	using (var wcpy = wstream.Duplicate()) {
			//		wcpy.Position = 0;
			//		StreamUtils.CopyStream(wcpy, fs);
			//	}
			//}
			//using (var fs = new FileStream(Path.Combine(@"c:\__graces\______fps4repacktest\", name.Replace("/", "_") + "_new.fps4"), FileMode.Create)) {
			//	newfps4stream.Position = 0;
			//	StreamUtils.CopyStream(newfps4stream, fs);
			//}

			newfps4stream.Position = 0;
			return newfps4stream;
		}
	}

	public class NubInfo {
		public string Name;
		public string WiiType;
		public int WiiSampleRate;
		public string EngType;
		public int EngFileCount;
	}

	public class ContainedVoiceInfo {
		public string BaseName;
		public int StartNumber;
		public int EndNumber;
		public int SE3Index;
		public string WiiType;
		public int WiiSampleRate;
		public string EngType;
		public bool IsSkit = false;
	}
}
