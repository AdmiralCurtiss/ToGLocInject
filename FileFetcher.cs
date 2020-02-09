using System;
using System.Collections.Generic;
using System.IO;
using HyoutaPluginBase;
using HyoutaTools.FileContainer;
using HyoutaUtils;
using HyoutaUtils.Streams;

namespace HyoutaTools.Tales.Graces.TranslationPort {
	internal class FileFetcher {
		private Config Config;

		Dictionary<(Version v, string cpk), HyoutaPluginBase.FileContainer.IContainer> cache;

		public FileFetcher(Config config) {
			Config = config;
			cache = new Dictionary<(Version v, string cpk), HyoutaPluginBase.FileContainer.IContainer>();
			cache.Add((Version.U, ""), config.GamefileContainerPS3US);
			cache.Add((Version.J, ""), config.GamefileContainerPS3JP);
			cache.Add((Version.E, ""), config.GamefileContainerPS3EU);
			cache.Add((Version.W, ""), config.GamefileContainerWiiV2);
			if (config.GamefileContainerWiiV0 != null) {
				cache.Add((Version.Wv0, ""), config.GamefileContainerWiiV0);
			}
		}

		private HyoutaPluginBase.FileContainer.INode ReturnAndCache(HyoutaPluginBase.FileContainer.INode node, string path, Version version) {
			if (node != null && path != null) {
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				using (FileStream fs = new FileStream(path, FileMode.Create)) {
					using (DuplicatableStream ds = node.AsFile.DataStream.Duplicate()) {
						StreamUtils.CopyStream(ds, fs, ds.Length);
					}
				}
			}
			return node;
		}

		private HyoutaPluginBase.FileContainer.INode GetFileInternal(string name, Version version) {
			if (name == "boot.elf") {
				switch (version) {
					case Version.U: return Config.EbootBinPS3US;
					case Version.J: return Config.EbootBinPS3JP;
					case Version.E: return Config.EbootBinPS3EU;
					case Version.W: return Config.MainDolWiiV2;
				}
				return null;
			} else {
				string cachepath = null;
				if (Config.CachePath != null) {
					cachepath = Path.Combine(Config.CachePath, version.ToString() + "_" + name.Replace('/', '_'));
					if (File.Exists(cachepath)) {
						return new FileFromStream(new DuplicatableByteArrayStream(new DuplicatableFileStream(cachepath).CopyToByteArrayAndDispose()));
					}
				}

				HyoutaPluginBase.FileContainer.IContainer root = cache[(version, "")];
				var split = name.Split('/');
				HyoutaPluginBase.FileContainer.IContainer cpk;
				if (!cache.TryGetValue((version, split[0]), out cpk)) {
					cpk = new Tales.CPK.CpkContainer(root.GetChildByName(split[0]).AsFile.DataStream);
					cache.Add((version, split[0]), cpk);
				}
				if (name == split[0]) {
					return cpk;
				}

				if (split[1].EndsWith(".cpk")) {
					HyoutaPluginBase.FileContainer.IContainer subcpk;
					string p = split[0] + "/" + split[1];
					if (!cache.TryGetValue((version, p), out subcpk)) {
						var s = cpk.GetChildByName(split[1])?.AsFile?.DataStream;
						if (s == null) {
							return null;
						}
						subcpk = new Tales.CPK.CpkContainer(s);
						cache.Add((version, p), subcpk);
					}
					if (name == p) {
						return subcpk;
					}
					return ReturnAndCache(subcpk.GetChildByName(name.Split(new char[] { '/' }, 3)[2]), cachepath, version);
				} else {
					if (version == Version.U && name == "rootR.cpk/sys/ja/SysString.bin") {
						return root.GetChildByName("Sys").AsContainer.GetChildByName("ja").AsContainer.GetChildByName("SysString.bin");
					}

					if (version != Version.W) {
						var fixup = name.Split(new char[] { '/' }, 3);
						if (fixup[1] == "sys" || fixup[1] == "str") {
							return ReturnAndCache(cpk.GetChildByName("S" + fixup[1].Substring(1) + '/' + fixup[2]), cachepath, version);
						} else {
							return ReturnAndCache(cpk.GetChildByName(name.Split(new char[] { '/' }, 2)[1]), cachepath, version);
						}
					} else {
						return ReturnAndCache(cpk.GetChildByName(name.Split(new char[] { '/' }, 2)[1]), cachepath, version);
					}
				}
			}
		}

		public DuplicatableStream GetFile(string name, Version version) {
			var f = GetFileInternal(name, version)?.AsFile?.DataStream;
			if (f == null) {
				throw new Exception("Failed to find " + version + ": " + name);
			}
			return f;
		}
		public HyoutaPluginBase.FileContainer.IFile TryGetFile(string name, Version version) {
			return GetFileInternal(name, version)?.AsFile;
		}

		public HyoutaPluginBase.FileContainer.IContainer TryGetContainer(string name, Version version) {
			return GetFileInternal(name, version)?.AsContainer;
		}
	}
}
