using HyoutaPluginBase;
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
			new NubInfo() { Name = "VOCHT", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 419 },
			new NubInfo() { Name = "VOBTL", WiiType = "dsp", WiiSampleRate = 24000, EngType = "vag", EngFileCount = 1917 },
			new NubInfo() { Name = "VOSCE01", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 1002 },
			new NubInfo() { Name = "VOSCE02", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 658 },
			new NubInfo() { Name = "VOSCE03", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 760 },
			new NubInfo() { Name = "VOSCE04", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 719 },
			new NubInfo() { Name = "VOSCE05", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 968 },
			new NubInfo() { Name = "VOSCE06", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 499 },
			new NubInfo() { Name = "VOSCE07", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 453 },
			new NubInfo() { Name = "VOSCE08", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 1031 },
			new NubInfo() { Name = "VOSCE16", WiiType = "bnsf", WiiSampleRate = 32000, EngType = "at3", EngFileCount = 1579 },
		};

		public static void Setup(Config config, string targetpath) {
			Directory.CreateDirectory(targetpath);
			var _fc = new FileFetcher(config);
			var rootR = _fc.TryGetContainer("rootR.cpk", Version.U);
			foreach (var nub in Nubs) {
				var nubstream = rootR.GetChildByName("snd/strpck/" + nub.Name + ".nub").AsFile.DataStream;
				HyoutaTools.Tales.Vesperia.NUB.NUB.ExtractNub(nubstream, Path.Combine(targetpath, nub.Name), HyoutaUtils.EndianUtils.Endianness.BigEndian);
			}

			StringBuilder sb = new StringBuilder();

			foreach (var nub in Nubs) {
				for (int i = 0; i < nub.EngFileCount; ++i) {
					sb.AppendFormat("test {0}\\{1:D8}.{2} -o {0}\\{1:D8}.wav", nub.Name, i, nub.EngType).AppendLine();

					if (nub.WiiType == "bnsf") {
						sb.AppendFormat("ToGLocInject --prepare-raw-bnsf-from-wav {0}\\{1:D8} {2}", nub.Name, i, nub.WiiSampleRate).AppendLine();
						sb.AppendFormat("encode 0 {0}\\{1:D8}.raw {0}\\{1:D8}.rawbnsf 48000 14000", nub.Name, i).AppendLine();
					}
				}
			}

			File.WriteAllText(Path.Combine(targetpath, "convert_voices.bat"), sb.ToString());

			return;
		}

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
					if (nub.WiiType == "bnsf") {
						using (var bnsfstream = new DuplicatableFileStream(Path.Combine(nubdir, i.ToString("D8") + ".rawbnsf")))
						using (var samplecountstream = new DuplicatableFileStream(Path.Combine(nubdir, i.ToString("D8") + ".samplecount"))) {
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
					}
				}

				long filesSize = outstream.Position - header.StartOfFiles;
				outstream.Position = 0x14;
				outstream.WriteUInt32((uint)filesSize, e);

				outstream.Position = 0;
				return outstream;
			}
		}
	}

	public class NubInfo {
		public string Name;
		public string WiiType;
		public int WiiSampleRate;
		public string EngType;
		public int EngFileCount;
	}
}
