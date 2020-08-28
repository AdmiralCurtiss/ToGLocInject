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
	internal static partial class FileProcessing {
		public static void GenerateTranslatedFiles(Config config) {
			var delayedInjects = new Dictionary<string, Stream>(); // injects that we may need to modify further before injecting
			bool generateNew = config.PatchedFileOutputPath != null;
			var _fc = new FileFetcher(config);
			var files = Mappings.GetFileMappings(_fc, config.EnglishVoiceProcessingDir != null);

			FileInjectorV0V2 map0inject = null;
			FileInjectorV0V2 map1inject = null;
			FileInjectorV0V2 rootinject = null;
			string v2outpath = generateNew ? Path.Combine(config.PatchedFileOutputPath, "v2patched") : null;
			string v0outpath = generateNew ? (config.GamefileContainerWiiV0 == null ? null : Path.Combine(config.PatchedFileOutputPath, "v0patched")) : null;
			if (generateNew) {
				map0inject = new FileInjectorV0V2(_fc.TryGetContainer("map0R.cpk", Version.W) as CpkContainer, config.GamefileContainerWiiV0 == null ? null : _fc.TryGetContainer("map0R.cpk", Version.Wv0) as CpkContainer, Path.Combine(v2outpath, "files", "map0R.cpk"), v0outpath == null ? null : Path.Combine(v0outpath, "files", "map0R.cpk"), 0x4A694000);
				map1inject = new FileInjectorV0V2(_fc.TryGetContainer("map1R.cpk", Version.W) as CpkContainer, config.GamefileContainerWiiV0 == null ? null : _fc.TryGetContainer("map1R.cpk", Version.Wv0) as CpkContainer, Path.Combine(v2outpath, "files", "map1R.cpk"), v0outpath == null ? null : Path.Combine(v0outpath, "files", "map1R.cpk"), 0x25DFB000);
				rootinject = new FileInjectorV0V2(_fc.TryGetContainer("rootR.cpk", Version.W) as CpkContainer, config.GamefileContainerWiiV0 == null ? null : _fc.TryGetContainer("rootR.cpk", Version.Wv0) as CpkContainer, Path.Combine(v2outpath, "files", "rootR.cpk"), v0outpath == null ? null : Path.Combine(v0outpath, "files", "rootR.cpk"), 0x30084000);
			}

			DuplicatableStream newFontMetrics = null;
			DuplicatableStream newFontTexture = null;
			Dictionary<char, (int w1, int w2)> charToWidthMap = null;
			bool fontTextureInjected = false;
			bool executableProcessed = false;

			if (false && config.PS3CompareCsvOutputPath != null) {
				string[] sofiles = new string[] { };
				foreach (string so in sofiles) {
					var e = ParseTss(_fc.GetFile(so, Version.E), isSkitFile: false);
					var u = ParseTss(_fc.GetFile(so, Version.U), isSkitFile: false);
					var j = ParseTss(_fc.GetFile(so, Version.J), isSkitFile: false);
					var escs = new SCS(_fc.GetFile(Path.Combine(Path.GetDirectoryName(so), "en", Path.GetFileNameWithoutExtension(so) + ".scs").Replace('\\', '/'), Version.E));
					var uscs = new SCS(_fc.GetFile(Path.Combine(Path.GetDirectoryName(so), "ja", Path.GetFileNameWithoutExtension(so) + ".scs").Replace('\\', '/'), Version.U));
					var jscs = new SCS(_fc.GetFile(Path.Combine(Path.GetDirectoryName(so), "ja", Path.GetFileNameWithoutExtension(so) + ".scs").Replace('\\', '/'), Version.J));
					var ea = Apply(e, escs.Entries);
					var ua = Apply(u, uscs.Entries);
					var ja = Apply(j, jscs.Entries);

					Console.WriteLine("Diffs: " + (ua.Count - ja.Count) + " / " + (ea.Count - ja.Count) + "  " + so);
					{
						Directory.CreateDirectory(Path.Combine(config.PS3CompareCsvOutputPath, "so"));
						CreateCsv(Path.Combine(config.PS3CompareCsvOutputPath, "so", so.Replace("/", "_") + ("_multipledout_eur") + ".csv"), ea);
						CreateCsv(Path.Combine(config.PS3CompareCsvOutputPath, "so", so.Replace("/", "_") + ("_multipledout_usa") + ".csv"), ua);
						CreateCsv(Path.Combine(config.PS3CompareCsvOutputPath, "so", so.Replace("/", "_") + ("_multipledout_jpn") + ".csv"), ja);
					}
				}

				return;
			}

			List<(string jp, string en)> compare = null;
			ISet<string> acceptableNonReplacements = new HashSet<string>();
			{
				string compareListFileName = @"map0R.cpk/mapfile_basiR.cpk/map/sce/R/ja/basi_d01.scs";
				MappingData compareListMappingData = new MappingData(c: false, u: Mappings.GenerateDefault());
				var jscs = new SCS(_fc.GetFile(compareListFileName, Version.J));
				var uscs = new SCS(_fc.GetFile(compareListFileName, Version.U));
				var j = InsertAdds(jscs, compareListMappingData.J, false, IsSkitFile(compareListFileName));
				var u = InsertAdds(uscs, compareListMappingData.U, true, IsSkitFile(compareListFileName));
				compare = new List<(string jp, string en)>();
				int jcount = j.Count;
				int ucount = u.Count;
				int jpos = 0;
				int upos = 0;
				for (int i = 0; i < jcount; ++i) {
					if (j[jpos].entry != u[upos].entry) {
						compare.Add((j[jpos].entry, u[upos].entry));
					}
					++jpos;
					++upos;
				}
				compare.Add((null, null));

				var wscs = new SCS(_fc.GetFile(compareListFileName, Version.W));
				acceptableNonReplacements.Add(wscs.Entries[39]);
				acceptableNonReplacements.Add(wscs.Entries[40]);
				acceptableNonReplacements.Add(wscs.Entries[96]);
				for (int cmcmc = 115; cmcmc <= 480; ++cmcmc) {
					acceptableNonReplacements.Add(wscs.Entries[cmcmc]);
				}
			}

			Dictionary<string, List<string>> directCompare = new Dictionary<string, List<string>>();
			if (compare != null) {
				var directCompareFiles = new List<string>();
				directCompareFiles.Add("map0R.cpk/mapfile_basiR.cpk/map/sce/R/ja/basi_d09.scs");
				foreach (string fn in directCompareFiles) {
					var jscs = new SCS(_fc.GetFile(fn, Version.J));
					var uscs = new SCS(_fc.GetFile(fn, Version.U));
					MappingData val;
					if (files.TryGetValue(fn, out val) && val != null) {
						var j = InsertAdds(jscs, val.J, false, IsSkitFile(fn));
						var u = InsertAdds(uscs, val.U, true, IsSkitFile(fn));
						List<MappedEntry> entries = MapCompare(compare, j, u, directCompare);
						foreach (var e in entries) {
							if (e.type == MappingType.NotMatched) {
								List<string> tmpval;
								if (directCompare.TryGetValue(e.jp, out tmpval)) {
									tmpval.Add(e.en);
								} else {
									directCompare.Add(e.jp, new List<string>() { e.en });
								}
							}
						}
					} else {
						Console.WriteLine("WARNING: Failed to find mappings for file " + fn);
					}
				}
			}

			CharNameBin charnamesJ = new CharNameBin(_fc.GetFile(@"rootR.cpk/str/ja/CharName.bin", Version.J));
			CharNameBin charnamesU = new CharNameBin(_fc.GetFile(@"rootR.cpk/str/ja/CharName.bin", Version.U));
			CharNameBin charnamesW = new CharNameBin(_fc.GetFile(@"rootR.cpk/str/ja/CharName.bin", Version.W));
			CharNameBin charnamesWE = charnamesW;
			CharNameMapping charnameMapping = CharNameMapping.BuildPs3ToWiiCharNameIdMapping(charnamesW, charnamesJ);

			List<string> mappingNextInput = new List<string>();

			// prefilter for common strings that we want to replace in the English files before actually matching
			List<(string j, string expectedU, string replacementU)> prefilterStrings = new List<(string j, string expectedU, string replacementU)>();
			(string j, string expectedU, string replacementU) prefilterStringMultidefinedJHack;
			{
				var d17j = new SCS(_fc.GetFile("map0R.cpk/mapfile_basiR.cpk/map/sce/R/ja/basi_d17.scs", Version.J));
				var d17u = new SCS(_fc.GetFile("map0R.cpk/mapfile_basiR.cpk/map/sce/R/ja/basi_d17.scs", Version.U));
				prefilterStringMultidefinedJHack = (d17j.Entries[477], d17u.Entries[474], d17u.Entries[532]);
				prefilterStrings.Add((d17j.Entries[270], d17u.Entries[268], d17u.Entries[391].Substring(0, 5)));
				prefilterStrings.Add(prefilterStringMultidefinedJHack);
			}

			foreach (var kvp in files) {
				string f = kvp.Key;
				bool writeAny = config.PS3CompareCsvOutputPath != null;
				bool writeAll = writeAny && !config.PS3CompareCsvWriteOnlyUnmatched;
				if (writeAny && !kvp.Value.SkipTextMapping && (writeAll || !kvp.Value.Confirmed)) {
					DuplicatableStream jstream = _fc.GetFile(f, Version.J);
					DuplicatableStream ustream = _fc.GetFile(f, Version.U);
					DuplicatableStream wstream = _fc.TryGetFile(f, Version.W)?.DataStream;
					SCS jscs;
					SCS uscs;
					SCS wscs;
					if (f == @"rootR.cpk/SysSub/JA/TOG_SS_ChatName.dat") {
						jscs = ReadChatNames(jstream);
						uscs = ReadChatNames(ustream);
						wscs = ReadChatNames(wstream);
					} else if (f == "boot.elf") {
						jscs = ConvertDoltextToScs(MainDolStringReader.ReadElfStringsJp(jstream.Duplicate()));
						uscs = ConvertDoltextToScs(MainDolStringReader.ReadElfStringsUs(ustream.Duplicate()));
						wscs = ConvertDoltextToScs(MainDolStringReader.ReadDolStrings(wstream));
					} else if (f == @"rootR.cpk/btl/acf/bin000.acf") {
						jscs = ReadBattleNames(jstream, Version.J);
						uscs = ReadBattleNames(ustream, Version.U);
						wscs = ReadBattleNames(wstream, Version.W);
					} else if (f == @"rootR.cpk/str/ja/CharName.bin") {
						jscs = new SCS(BuildCharnameList(new CharNameBin(jstream)));
						uscs = new SCS(BuildCharnameList(new CharNameBin(ustream)));
						wscs = new SCS(BuildCharnameList(new CharNameBin(wstream)));
					} else {
						jscs = new SCS(jstream);
						uscs = new SCS(ustream);
						wscs = wstream != null ? new SCS(wstream) : null;
					}
					bool isSkitFile = IsSkitFile(f);
					var j = InsertAdds(jscs, kvp.Value.J, false, isSkitFile);
					var u = InsertAdds(uscs, kvp.Value.U, true, isSkitFile);
					var w = wscs != null ? InsertAdds(wscs, new M(), false, isSkitFile) : null;

					if (compare != null) {
						List<MappedEntry> entries = MapCompare(compare, j, u, directCompare);

						if (config.PS3CompareCsvOutputPath != null) {
							bool anyUnmatched = entries.Any(x => !x.type.IsMatching());
							int wrong = CountWrongEntriesAtEnd(entries);
							if (true) {
								Stream utssstream = null;
								Stream jtssstream = null;
								if (f != @"rootR.cpk/str/ja/CharName.bin") {
									if (isSkitFile) {
										string chdpath = "chat/chd/" + Path.GetFileNameWithoutExtension(f) + ".chd";
										DuplicatableStream uchdstream = _fc.TryGetFile("rootR.cpk/" + chdpath, Version.U)?.AsFile?.DataStream;
										DuplicatableStream jchdstream = _fc.TryGetFile("rootR.cpk/" + chdpath, Version.J)?.AsFile?.DataStream;
										if (uchdstream != null) {
											var uchd = new FPS4(uchdstream.Duplicate());
											utssstream = uchd.GetChildByIndex(0).AsFile.DataStream.Duplicate();
										}
										if (jchdstream != null) {
											var jchd = new FPS4(jchdstream.Duplicate());
											jtssstream = jchd.GetChildByIndex(0).AsFile.DataStream.Duplicate();
										}
									} else if (f != "boot.elf") {
										string sopath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f)), Path.GetFileNameWithoutExtension(f) + ".so").Replace('\\', '/');
										HyoutaPluginBase.FileContainer.IFile ufile = _fc.TryGetFile(sopath, Version.U);
										HyoutaPluginBase.FileContainer.IFile jfile = _fc.TryGetFile(sopath, Version.J);
										if (ufile != null) {
											utssstream = ufile.DataStream;
										}
										if (jfile != null) {
											jtssstream = jfile.DataStream;
										}
									}
								}
								List<int> utss;
								List<int> jtss;
								if (utssstream != null) {
									utss = ParseTss(utssstream, isSkitFile);
								} else {
									Console.WriteLine("WARNING: Failed to find TSS script for US " + f);
									utss = null;
								}
								if (jtssstream != null) {
									jtss = ParseTss(jtssstream, isSkitFile);
								} else {
									Console.WriteLine("WARNING: Failed to find TSS script for JP " + f);
									jtss = null;
								}
								var utssapplied = Apply(utss, uscs.Entries);
								var jtssapplied = Apply(jtss, jscs.Entries);

								Directory.CreateDirectory(config.PS3CompareCsvOutputPath);
								List<string> comparecsv = new List<string>();
								bool utssbreak = false;
								bool jtssbreak = false;
								{
									int utssoffset = -1;
									int jtssoffset = -1;
									bool printedPrevious = false;
									List<(MappedEntry e, int? jtss, int? utss)?> prints = new List<(MappedEntry e, int? jtss, int? utss)?>();
									List<int> jcycle = new List<int>();
									List<int> ucycle = new List<int>();
									for (int i = 0; i < entries.Count; ++i) {
										bool jincrementskip = false;
										bool uincrementskip = false;
										if (ShouldPrintMappedEntry(entries, i)) {
											printedPrevious = true;
											MappedEntry e = entries[i];
											if (jtssoffset == -1 && e.jpos >= 0) {
												jtssoffset = FindFirstIndex(jtssapplied, e.jpos);
											}
											if (utssoffset == -1 && e.upos >= 0) {
												utssoffset = FindFirstIndex(utssapplied, e.upos);
											}
											if (jtssoffset != -1 && e.jpos >= 0) {
												if (jtssoffset >= jtssapplied.Count) {
													jtssbreak = true;
												} else {
													int jtssoffsetstart = jtssoffset;
													while (jtssapplied[jtssoffset].index != e.jpos) {
														jcycle.Add(jtssoffset);
														++jtssoffset;
														if (jtssoffset >= jtssapplied.Count) {
															jtssbreak = true;
															jtssoffset = jtssoffsetstart;
															jcycle.Clear();
															jcycle.Add(-2);
															jincrementskip = true;
															break;
														}
													}
												}
											}
											if (utssoffset != -1 && e.upos >= 0) {
												if (utssoffset >= utssapplied.Count) {
													utssbreak = true;
												} else {
													int utssoffsetstart = utssoffset;
													while (utssapplied[utssoffset].index != e.upos) {
														ucycle.Add(utssoffset);
														++utssoffset;
														if (utssoffset >= utssapplied.Count) {
															utssbreak = true;
															utssoffset = utssoffsetstart;
															ucycle.Clear();
															ucycle.Add(-2);
															uincrementskip = true;
															break;
														}
													}
												}
											}
											if (jcycle.Count > 0 || ucycle.Count > 0) {
												for (int cycleidx = 0; cycleidx < Math.Max(jcycle.Count, ucycle.Count); ++cycleidx) {
													int? jjjj = null;
													int? uuuu = null;
													if (cycleidx < jcycle.Count) {
														jjjj = jcycle[cycleidx];
													}
													if (cycleidx < ucycle.Count) {
														uuuu = ucycle[cycleidx];
													}
													prints.Add((null, jjjj, uuuu));
												}
												jcycle.Clear();
												ucycle.Clear();
											}
											prints.Add((e, e.jpos >= 0 ? (jincrementskip ? -1 : jtssoffset) : -1, e.upos >= 0 ? (uincrementskip ? -1 : utssoffset) : -1));
											if (!jincrementskip && jtssoffset != -1 && e.jpos >= 0) { ++jtssoffset; }
											if (!uincrementskip && utssoffset != -1 && e.upos >= 0) { ++utssoffset; }
										} else {
											if (printedPrevious) {
												printedPrevious = false;
												prints.Add(null);
											}
											jtssoffset = -1;
											utssoffset = -1;
										}
									}

									foreach (var x in prints) {
										if (x == null) {
											comparecsv.Add("----------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
										} else {
											comparecsv.Add(string.Format(
												"{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
												FindTssCompareString(x.Value, jtssapplied, utssapplied),
												x.Value.e == null ? "." : x.Value.e.type.ToString(),
												(x.Value.jtss == null || x.Value.jtss.Value < 0 || x.Value.jtss.Value >= jtssapplied.Count) ? (x.Value.jtss == null ? "." : x.Value.jtss.Value.ToString()) : jtssapplied[x.Value.jtss.Value].index.ToString(),
												(x.Value.jtss == null || x.Value.jtss.Value < 0 || x.Value.jtss.Value >= jtssapplied.Count) ? "." : CsvEscape(jtssapplied[x.Value.jtss.Value].entry, charnamesU),
												x.Value.e == null ? "." : CsvEscape(x.Value.e.jp, charnamesU),
												x.Value.e == null ? "." : x.Value.e.jpos.ToString(),
												x.Value.e == null ? "." : x.Value.e.upos.ToString(),
												x.Value.e == null ? "." : CsvEscape(x.Value.e.en, charnamesU),
												(x.Value.utss == null || x.Value.utss.Value < 0 || x.Value.utss.Value >= utssapplied.Count) ? "." : CsvEscape(utssapplied[x.Value.utss.Value].entry, charnamesU),
												(x.Value.utss == null || x.Value.utss.Value < 0 || x.Value.utss.Value >= utssapplied.Count) ? (x.Value.utss == null ? "." : x.Value.utss.Value.ToString()) : utssapplied[x.Value.utss.Value].index.ToString()
											));
										}
									}
								}
								comparecsv.Add("");
								comparecsv.Add("");
								comparecsv.Add("");
								comparecsv.Add("");
								foreach (var e in entries) {
									comparecsv.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", e.type, e.jpos, CsvEscape(e.jp, charnamesJ), e.upos, CsvEscape(e.en, charnamesU)));
								}

								if (w != null) {
									comparecsv.Add("");
									comparecsv.Add("");
									comparecsv.Add("");
									comparecsv.Add("");
									foreach (var e in w) {
										comparecsv.Add(string.Format("{0}\t{1}", e.index, CsvEscape(e.entry, charnamesW)));
									}
								}

								string csvp = Path.Combine(config.PS3CompareCsvOutputPath, f.Replace("/", "_") + (w != null ? "" : "_FOnly") + (anyUnmatched ? "" : "_MATCHES") + (wrong != 0 ? "_WRONG" + wrong : "") + (jtssbreak ? "_JBREAK" : "") + (utssbreak ? "_UBREAK" : "") + ".csv");
								File.WriteAllLines(csvp, comparecsv);
							}
						}
					}

					int jcount = j.Count;
					int ucount = u.Count;
					if (jcount != ucount) {
						Console.WriteLine("Diff of " + (j.Count - u.Count) + " in " + f);
					} else {
						Console.WriteLine("Mapping strings from " + f);
					}
				}

				var currentFileW = generateNew ? _fc.TryGetFile(f, Version.W) : null;
				if (generateNew && kvp.Value.Confirmed && currentFileW != null) {
					bool isFontTexture = f == "rootR.cpk/sys/FontTexture2.tex";
					bool isFontMetrics = f == "rootR.cpk/sys/FontBinary2.bin";
					bool isExecutable = f == "boot.elf";
					bool isGenericTexture = f.StartsWith("rootR.cpk/mg/tex") || f.StartsWith("rootR.cpk/mnu/tex") || f == "rootR.cpk/SysSub/JA/TitleTexture.tex";
					bool isSkitText = f.StartsWith("rootR.cpk/chat/scs/JA/");
					bool delayInjection = isSkitText;

					Console.WriteLine("Processing and injecting " + f + "...");
					DuplicatableStream jstream;
					DuplicatableStream ustream;

					if ((isFontTexture || isFontMetrics || isExecutable) && (newFontMetrics == null || newFontTexture == null)) {
						Console.WriteLine("Converting font...");
						(newFontMetrics, newFontTexture, charToWidthMap) = FontProcessing.Run(_fc, config);
					}

					if (isFontTexture || isFontMetrics) {
						jstream = f == "rootR.cpk/sys/FontBinary2.bin" ? newFontMetrics.Duplicate() : newFontTexture.Duplicate();
						ustream = jstream.Duplicate();
					} else if (kvp.Value.ReplaceInWiiV0) {
						jstream = null;
						ustream = null;
					} else {
						jstream = _fc.GetFile(f, Version.J);
						ustream = _fc.GetFile(f, Version.U);
					}

					DuplicatableStream wstream = currentFileW?.DataStream;
					SCS wscs = null;
					List<(int index, string entry)> j;
					List<(int index, string entry)> u;
					List<MainDolString> doltext = null;
					List<MainDolString> elf_u_text = null;
					List<MainDolString> elf_j_text = null;
					SortedSet<int> multidefined_j_idxs = null;
					CharNameBin jcharbin = null;
					CharNameBin ucharbin = null;
					CharNameBin wcharbin = null;
					if (f == @"rootR.cpk/str/ja/CharName.bin") {
						jcharbin = new CharNameBin(jstream.Duplicate());
						ucharbin = new CharNameBin(ustream.Duplicate());
						wcharbin = new CharNameBin(wstream.Duplicate());
						var jchars = BuildCharnameList(jcharbin);
						var uchars = BuildCharnameList(ucharbin);
						var wchars = BuildCharnameList(wcharbin);
						j = new List<(int index, string entry)>();
						u = new List<(int index, string entry)>();
						if (jchars.Count != uchars.Count) {
							throw new Exception();
						}
						for (int aaaa = 0; aaaa < jchars.Count; ++aaaa) {
							j.Add((aaaa, jchars[aaaa]));
							u.Add((aaaa, uchars[aaaa]));
						}
						wscs = new SCS(wchars);
					} else if (f == @"rootR.cpk/SysSub/JA/TOG_SS_ChatName.dat") {
						j = InsertAdds(ReadChatNames(jstream), kvp.Value.J, false, false);
						u = InsertAdds(ReadChatNames(ustream), kvp.Value.U, true, false);
						wscs = ReadChatNames(wstream);
					} else if (f == "boot.elf") {
						elf_j_text = MainDolStringReader.ReadElfStringsJp(jstream.Duplicate());
						elf_u_text = MainDolStringReader.ReadElfStringsUs(ustream.Duplicate());
						j = InsertAdds(ConvertDoltextToScs(elf_j_text), kvp.Value.J, false, false);
						u = InsertAdds(ConvertDoltextToScs(elf_u_text), kvp.Value.U, true, false);
						doltext = MainDolStringReader.ReadDolStrings(wstream);
						wscs = ConvertDoltextToScs(doltext);
					} else if (f == @"rootR.cpk/btl/acf/bin000.acf") {
						j = InsertAdds(ReadBattleNames(jstream, Version.J), kvp.Value.J, false, false);
						u = InsertAdds(ReadBattleNames(ustream, Version.U), kvp.Value.U, true, false);
						wscs = ReadBattleNames(wstream, Version.W);
					} else if (kvp.Value.SkipTextMapping) {
						j = new List<(int index, string entry)>();
						u = new List<(int index, string entry)>();
						wscs = new SCS(new List<string>());
					} else {
						SCS jscs = null;
						SCS uscs = null;
						jscs = new SCS(jstream);
						uscs = new SCS(ustream);
						wscs = new SCS(wstream);
						j = InsertAdds(jscs, kvp.Value.J, false, IsSkitFile(f));
						u = InsertAdds(uscs, kvp.Value.U, true, IsSkitFile(f));
						ApplyPrefilter(j, u, prefilterStrings);
						multidefined_j_idxs = FilterMultidefinedJs(GetDefinedAddsSet(kvp.Value.J), j, u, prefilterStringMultidefinedJHack);
					}

					List<(int index, string entry)> j_orig_name_ids = j;
					List<(int index, string entry)> u_orig_name_ids = u;
					j = charnameMapping.MapAllPs3ToWii(j);
					u = charnameMapping.MapAllPs3ToWii(u);

					if (j.Count == u.Count) {
						if (kvp.Value.MultiplyOutSkit) {
							string chdpath = "chat/chd/" + Path.GetFileNameWithoutExtension(f) + ".chd";
							string chdpathroot = "rootR.cpk/" + chdpath;
							DuplicatableStream chdstream = _fc.GetFile(chdpathroot, Version.W);
							var chd = new FPS4(chdstream.Duplicate());
							var tssstream = chd.GetChildByIndex(0).AsFile.DataStream.Duplicate();
							var multipliedResult = SkitProcessing.MultiplyOutSkitTss(tssstream, wscs);
							wscs = multipliedResult.wscsnew;
							long fps4InjectLoc = chd.Files[0].Location.Value;
							Stream chdmodstream = chdstream.CopyToMemory();
							chdmodstream.Position = fps4InjectLoc;
							StreamUtils.CopyStream(multipliedResult.newTssStream, chdmodstream, multipliedResult.newTssStream.Length);
							chdmodstream.Position = 0;
							delayedInjects.Add(chdpathroot, chdmodstream);
						}

						SCS wscsorig = new SCS(new List<string>(wscs.Entries));
						(int unmappedCount, List<int> indicesUnmapped, List<bool> juConsumed, List<(int widx, int jidx)> widx_with_multidefined_j) unmappedStrings;
						if (!kvp.Value.SkipTextMapping) {
							if (f == "boot.elf") {
								// process this one in chunks for better matching, to reduce context-incorrect jp wii <- us ps3 matches
								unmappedStrings = ReplaceStringsWMainDol(acceptableNonReplacements, wscs, j, u, kvp.Value.W, true);
							} else {
								var p = ReplaceStringsW_Part1(acceptableNonReplacements, wscs, wscsorig, j, u, kvp.Value.W, true, multidefined_j_idxs);
								wscs = p.wscs;
								int newStringCount = 0;
								if (f.StartsWith("map") && f.EndsWith(".scs") && p.widx_with_multidefined_j.Count > 0) {
									var multiplyOutResult = ScenarioProcessing.MultiplyOutScenarioFile(p.widx_with_multidefined_j, _fc, f, wscs, j, u);
									if (multiplyOutResult.newScenarioFileStream != null) {
										newStringCount = multiplyOutResult.wscsnew.Entries.Count - wscs.Entries.Count;
										wscs = multiplyOutResult.wscsnew;
										Console.WriteLine("Appended " + newStringCount + " strings in " + f);
										ReplaceStringsWMultipliedOut(wscs, p.j, p.u, p.widx_with_multidefined_j, multiplyOutResult.new_multidefined_widxs, charnamesW);
										InjectFile(map0inject, map1inject, rootinject, multiplyOutResult.newScenarioFilePath, multiplyOutResult.newScenarioFileStream);

										for (int nsc = 0; nsc < newStringCount; ++nsc) {
											p.wOverwritten.Add(true);
										}
									}
								}
								unmappedStrings = ReplaceStringsW_Part2(p.acceptableNonReplacements, wscs, p.wscsorig, p.j, p.u, p.prep, p.replacementCountGlobal + newStringCount, p.juConsumedGlobal, p.wOverwritten, p.widx_with_multidefined_j);
							}
						} else {
							unmappedStrings = (0, null, null, null);
						}

						Stream scsstr;
						if (isGenericTexture) {
							scsstr = TextureProcessing.ProcessTexture(_fc, f, jstream, ustream);
						} else if (f.EndsWith(".ani")) {
							scsstr = TextureProcessing.ProcessAreaNameTexture(_fc, f, jstream, ustream);
						} else if (f.EndsWith(".nub")) {
							scsstr = VoiceInject.InjectEnglishVoicesToWiiNub(config, _fc, f, wstream, jstream, ustream);
						} else if (f == @"rootR.cpk/snd/init/StrConfig.stp") {
							scsstr = new DuplicatableFileStream(Path.Combine(config.EnglishVoiceProcessingDir, "StrConfig.stp")).CopyToMemoryAndDispose();
						} else if (kvp.Value.VoiceInject != null) {
							if (delayedInjects.ContainsKey(f)) {
								wstream = new DuplicatableByteArrayStream(delayedInjects[f].CopyToByteArrayAndDispose());
								delayedInjects.Remove(f);
							}
							scsstr = VoiceInject.InjectEnglishContainedVoice(config, _fc, f, wstream, jstream, ustream, kvp.Value.VoiceInject);
							if (kvp.Value.VoiceInject.IsSkit) {
								string skitScsPath = f.Replace("/chd/", "/scs/JA/").Replace(".chd", ".scs");
								if (delayedInjects.ContainsKey(skitScsPath)) {
									// we copied the skit script from the EN version, so we need to also use the english skit text so the text IDs match
									var uskitscs = _fc.TryGetFile(skitScsPath, Version.U);
									delayedInjects.Remove(skitScsPath);
									delayedInjects.Add(skitScsPath, uskitscs.AsFile.DataStream.Duplicate());
								}
							}
						} else if (f == @"rootR.cpk/str/ja/CharName.bin") {
							// rebuild char mapping from new wscs
							List<string> deduplicatedNames = new List<string>();
							List<int> indicesToNames = new List<int>();

							for (int aaa = 0; aaa < wscs.Entries.Count; ++aaa) {
								string str = wscs.Entries[aaa];
								if (str == null || str == "") {
									indicesToNames.Add(0);
								} else {
									int ifxof = deduplicatedNames.IndexOf(str);
									if (ifxof == -1) {
										ifxof = deduplicatedNames.Count;
										deduplicatedNames.Add(str);
									}
									indicesToNames.Add(ifxof + 1);
								}
							}

							Dictionary<int, (ushort reg, ushort alt)> idToScsMappings = new Dictionary<int, (ushort reg, ushort alt)>();
							foreach (CharNameBinSection section in wcharbin.Sections) {
								for (int numbercountcounter = 0; numbercountcounter < section.NumberCount; ++numbercountcounter) {
									int n = section.NumberStart + numbercountcounter;
									idToScsMappings.Add(n, ((ushort)indicesToNames[n * 2], (ushort)indicesToNames[n * 2 + 1]));
								}
							}

							charnamesWE = new CharNameBin(wcharbin.Sections, idToScsMappings, new SCS(deduplicatedNames));
							MemoryStream ms = charnamesWE.GenerateFile();
							ms.Position = 0;
							scsstr = ms;
						} else if (kvp.Value.ReplaceInWiiV0) {
							wstream.ReStart();
							scsstr = wstream.CopyToMemory();
							scsstr.Position = 0;
						} else if (kvp.Value.SkipTextMapping) {
							scsstr = ustream.CopyToMemory();
							scsstr.Position = 0;
						} else if (f == @"rootR.cpk/SysSub/JA/TOG_SS_ChatName.dat") {
							wstream.ReStart();
							MemoryStream ms = new MemoryStream();
							for (int bbb = 0; bbb < 0x18; ++bbb) {
								ms.WriteByte(wstream.ReadUInt8());
							}
							long stringpos = ms.Position + 4 * wscs.Entries.Count;
							foreach (string chatname in wscs.Entries) {
								long cpos = ms.Position;
								long spos = stringpos;
								ms.Position = stringpos;
								ms.WriteShiftJisNullterm(chatname);
								stringpos = ms.Position;
								ms.Position = cpos;
								ms.WriteUInt32(((uint)(spos - cpos)).ToEndian(EndianUtils.Endianness.BigEndian));
							}
							ms.Position = 0;
							scsstr = ms;
						} else if (f == "boot.elf") {
							ToGLocInject.MainDolPostProcess.PostProcessMainDolReplacements(_fc, charnamesU, wscs, wscsorig, j, u, charToWidthMap);

							MemoryStream fontStream = newFontTexture.CopyToMemory();

							long wstreamlength = wstream.Length;
							MemoryStream ms = new MemoryStream();
							wstream.Position = 0;
							StreamUtils.CopyStream(wstream, ms, wstreamlength);
							wstream.Position = 0;
							var dol = new HyoutaTools.GameCube.Dol(wstream);

							// figure out where we're allowed to put text
							// first 0xFF out a memory region of the same size as the rom
							MemoryStream storageFinder = new MemoryStream((int)wstreamlength);
							for (long llll = 0; llll < wstreamlength; ++llll) {
								storageFinder.WriteByte(0xFF);
							}
							// then write zeros to space we assume to be safe for use
							List<string> statusMessagesReusedTextPositions = new List<string>();
							{
								// sweep across executable and figure out if our pointers are reused elsewhere, then don't mark those as freespace
								// first delete all the pointers we're gonna replace
								var tmp = wstream.CopyToMemory();
								foreach (var d in doltext) {
									tmp.Position = d.RomPointerPosition;
									tmp.WriteUInt32(0);
								}

								// then read entire file in 4 byte chunks into set
								var set = new Dictionary<uint, List<uint>>();
								tmp.Position = 0;
								long tmplength = tmp.Length;
								while (tmp.Position < tmplength) {
									uint loc = (uint)tmp.Position;
									uint ptr = tmp.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
									if (set.ContainsKey(ptr)) {
										set[ptr].Add(loc);
									} else {
										var ttttt = new List<uint>();
										ttttt.Add(loc);
										set.Add(ptr, ttttt);
									}
								}

								// okay, set now contains all pointers that could possibly be used elsewhere
								// sweep across both storage finder and actual output binary and erase memchunks
								foreach (var d in doltext) {
									if (d.RomTextPosition != 0) {
										if (!set.ContainsKey(dol.MapRomToRam(d.RomTextPosition))) {
											storageFinder.Position = d.RomTextPosition;
											ms.Position = d.RomTextPosition;
											for (uint cnt = 0; cnt < d.StringByteCount.Align(4); ++cnt) {
												storageFinder.WriteByte(0);
												ms.WriteByte(0);
											}
										} else {
											statusMessagesReusedTextPositions.Add("text at 0x" + d.RomTextPosition.ToString("X8") + " is reused");
											statusMessagesReusedTextPositions.Add("text is " + ReduceToSingleLine(d.Text));
											statusMessagesReusedTextPositions.Add("ram pointer would be " + dol.MapRomToRam(d.RomTextPosition).ToString("X8"));
											foreach (uint v in set[dol.MapRomToRam(d.RomTextPosition)]) {
												statusMessagesReusedTextPositions.Add("can be found in rom at 0x" + v.ToString("X8"));
											}
										}
									}
								}
							}
							if (config.DebugTextOutputPath != null) {
								Directory.CreateDirectory(config.DebugTextOutputPath);
								File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "non_cleared_text_areas.txt"), statusMessagesReusedTextPositions);
							}
							// put found memory chunks into a more usable format; address + bytes left
							List<MemChunk> memchunks = new List<MemChunk>();
							{
								storageFinder.Position = 0;
								uint startaddress = 0;
								bool in_chunk = false;
								while (storageFinder.Position < storageFinder.Length) {
									if (!in_chunk) {
										if (storageFinder.ReadByte() == 0) {
											startaddress = (uint)(storageFinder.Position - 1);
											in_chunk = true;
										}
									} else {
										if (storageFinder.ReadByte() != 0) {
											uint len = (uint)(storageFinder.Position - 1) - startaddress;
											in_chunk = false;
											MemChunk mc = new MemChunk();
											mc.Address = startaddress;
											mc.FreeBytes = len;
											mc.File = ms;
											mc.Mapper = dol;
											mc.IsInternal = true;
											memchunks.Add(mc);
										}
									}
								}

								bool allowInjectIntoFontTexture = true;
								if (allowInjectIntoFontTexture) {
									memchunks.AddRange(ToGLocInject.FontSpaceFinder.FindFreeMemoryInFontTexture(fontStream));
								}
							}

							uint maxTextLength = 0;
							foreach (string tmp in wscs.Entries) {
								if (tmp != null) {
									maxTextLength = Math.Max(maxTextLength, (uint)TextUtils.StringToBytesShiftJis(tmp).Length);
								}
							}
							ReservedMemchunk reservedSpaceTextRenderBuffer = ReserveMemory(memchunks, (maxTextLength * 8u + maxTextLength).Align(4));
							ReservedMemchunk reservedSpaceFontTexPointerFix = ReserveMemory(memchunks, CodePatches.CodeSizeForFontTexPointerFix());
							ReservedMemchunk reservedSpaceBattleChallengeDescription = ReserveMemory(memchunks, 0x60);

							// actually write strings to executable
							List<string> failedToFinds = new List<string>();
							long requiredExtraBytes = 0;
							Dictionary<SJisString, uint> alreadyWrittenStrings = new Dictionary<SJisString, uint>();
							foreach ((int dolidx, bool forceInternal) in GenerateDoltextInjectOrder(doltext)) {
								var d = doltext[dolidx];
								string t = wscs.Entries[dolidx];
								if (t == null) {
									// if text is null write a nullptr to the rom
									ms.Position = d.RomPointerPosition;
									ms.WriteUInt32(0);
								} else {
									byte[] inject = TextUtils.StringToBytesShiftJis(t);
									SJisString sjis = new SJisString(inject);
									uint address;
									if (alreadyWrittenStrings.TryGetValue(sjis, out address)) {
										ms.Position = d.RomPointerPosition;
										ms.WriteUInt32(address);
									} else {
										uint bytecount = ((uint)inject.Length) + 1;
										MemChunk chunk = memchunks.FirstOrDefault(x => x.FreeBytes >= bytecount && (!forceInternal || x.IsInternal));
										if (chunk != null) {
											address = chunk.Mapper.MapRomToRam(chunk.Address).ToEndian(EndianUtils.Endianness.BigEndian);
											chunk.File.Position = chunk.Address;
											for (uint cnt = 0; cnt < bytecount; ++cnt) {
												byte b = (byte)(cnt < inject.Length ? inject[cnt] : 0);
												chunk.File.WriteByte(b);
											}
											chunk.Address += bytecount;
											chunk.FreeBytes -= bytecount;

											ms.Position = d.RomPointerPosition;
											ms.WriteUInt32(address);
											alreadyWrittenStrings.Add(sjis, address);
										} else {
											Console.WriteLine("ERROR: Failed to find free space for string " + t);
											failedToFinds.Add("ERROR: Failed to find free space for string " + t);
											requiredExtraBytes += bytecount;
											ms.Position = d.RomPointerPosition;
											ms.WriteUInt32(dol.MapRomToRam(0x4D2828u).ToEndian(EndianUtils.Endianness.BigEndian)); // point at a default string instead
										}
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
								foreach (MemChunk mc in memchunks) {
									unusedByteCount += mc.FreeBytes;
								}
								failedToFinds.Add("Have " + unusedByteCount + " bytes of unused space.");
								File.WriteAllLines(Path.Combine(config.DebugTextOutputPath, "maindol_no_space_left.txt"), failedToFinds);
							}

							// finally apply a few code patches

							// allow this to boot on non-JPN consoles by always claiming Japanese system language in the SCGetLanguage wii system library function
							{
								ms.Position = dol.MapRamToRom(0x803595dcu);
								ms.WriteUInt32(0x38600000u.ToEndian(EndianUtils.Endianness.BigEndian)); // li r3, 0
							}

							// remove special cases in font metrics calculation function to let spaces read its metrics from the font metrics file
							{
								ms.Position = dol.MapRamToRom(0x80068534u);
								ms.WriteUInt32(0x3BE00001u.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x800685e0u);
								ms.WriteUInt32(0x60000000u.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x800685f8u);
								ms.WriteUInt32(0x60000000u.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x80068898u);
								ms.WriteUInt32(0x4800006Cu.ToEndian(EndianUtils.Endianness.BigEndian));
							}

							// at the start of battles the game may sprintf the challenge description to a ~32 byte buffer at 0x80632050 to display later
							// this overflows with english strings, so repoint that buffer elsewhere
							// TODO: check if we also have such issues with the challenge command, buffer for that seems to be at 0x8073C638, similar size
							{
								uint newBufferAddr = reservedSpaceBattleChallengeDescription.AddressRam;
								ushort high = (ushort)(newBufferAddr >> 16);
								ushort low = (ushort)(newBufferAddr & 0xFFFF);
								ushort highwrite = (ushort)(low >= 0x8000 ? high + 1 : high);
								ms.Position = dol.MapRamToRom(0x800bdd46u);
								ms.WriteUInt16(highwrite.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x800bdd4eu);
								ms.WriteUInt16(low.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x800bdd5au);
								ms.WriteUInt16(low.ToEndian(EndianUtils.Endianness.BigEndian));
							}

							// default text render buffer is too small for some of our long english strings, extend
							{
								uint addr1 = reservedSpaceTextRenderBuffer.AddressRam;
								uint addr2 = reservedSpaceTextRenderBuffer.AddressRam + maxTextLength * 8u;
								ushort high1 = (ushort)(addr1 >> 16);
								ushort low1 = (ushort)(addr1 & 0xFFFF);
								ushort high1write = (ushort)(low1 >= 0x8000 ? high1 + 1 : high1);
								ushort high2 = (ushort)(addr2 >> 16);
								ushort low2 = (ushort)(addr2 & 0xFFFF);
								ushort high2write = (ushort)(low2 >= 0x8000 ? high2 + 1 : high2);
								ms.Position = dol.MapRamToRom(0x80066972u);
								ms.WriteUInt16(high1write.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x8006697au);
								ms.WriteUInt16(low1.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x80066976u);
								ms.WriteUInt16(high2write.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x80066982u);
								ms.WriteUInt16(low2.ToEndian(EndianUtils.Endianness.BigEndian));
								ms.Position = dol.MapRamToRom(0x8006697eu);
								ms.WriteUInt16(((ushort)maxTextLength).ToEndian(EndianUtils.Endianness.BigEndian));
							}

							CodePatches.ApplyFontTexPointerFix(ms, dol, reservedSpaceFontTexPointerFix);

							fontStream.Position = 0;
							newFontTexture = new DuplicatableByteArrayStream(fontStream.CopyToByteArrayAndDispose());

							executableProcessed = true;

							ms.Position = 0;
							scsstr = ms;
						} else if (f == @"rootR.cpk/btl/acf/bin000.acf") {
							wstream.ReStart();
							MemoryStream ms = wstream.CopyToMemory();

							ms.Position = 0x1C;
							long start = ms.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
							long size = ms.ReadUInt32().FromEndian(EndianUtils.Endianness.BigEndian);
							ms.Position = start;
							foreach (string enemyname in wscs.Entries) {
								long tmp = ms.Position;
								byte[] inject = TextUtils.StringToBytesShiftJis(enemyname);
								for (uint cnt = 0; cnt < 0x17; ++cnt) {
									byte b = (byte)(cnt < inject.Length ? inject[cnt] : 0);
									ms.WriteByte(b);
								}
								ms.WriteByte(0);
								ms.Position = tmp + 0x88;
							}

							ms.Position = 0;
							scsstr = ms;
						} else {
							scsstr = wscs.WriteToScs();
						}

						bool writeAnyReplaceCsvs = config.WiiCompareCsvOutputPath != null;
						bool writeAllReplaceCsvs = writeAnyReplaceCsvs && !config.WiiCompareCsvWriteOnlyUnmatched;
						if ((writeAnyReplaceCsvs && !kvp.Value.SkipTextMapping && (writeAllReplaceCsvs || !kvp.Value.Confirmed || unmappedStrings.unmappedCount > 0))) {
							List<string> csv = new List<string>();
							for (int hhhh2 = 0; hhhh2 < 2; ++hhhh2) {
								for (int hhh = 0; hhh < wscsorig.Entries.Count; ++hhh) {
									int entry = hhh;
									bool unmapped = f != "boot.elf" && unmappedStrings.indicesUnmapped.Contains(entry);
									if (unmapped || hhhh2 == 1) {
										string orig = wscsorig.Entries[entry];
										string repl = wscs.Entries[entry];
										csv.Add(string.Format("{0}\t{1}\t{2}\t{3}", unmapped ? "!!!!!!" : "", entry, CsvEscape(orig, charnamesW), CsvEscape(repl, charnamesWE)));
									}
								}
								csv.Add("");
								csv.Add("");
								csv.Add("");
							}
							csv.Add("\t\tUnconsumed:\t");
							for (int hhhh2 = 0; hhhh2 < unmappedStrings.juConsumed.Count; ++hhhh2) {
								if (!unmappedStrings.juConsumed[hhhh2]) {
									csv.Add(string.Format("{0}\t{1}\t{2}\t{3}", "", u[hhhh2].index, CsvEscape(j_orig_name_ids[hhhh2].entry, charnamesJ), CsvEscape(u_orig_name_ids[hhhh2].entry, charnamesU)));
								}
							}
							Directory.CreateDirectory(config.WiiCompareCsvOutputPath);
							string csvp = Path.Combine(config.WiiCompareCsvOutputPath, f.Replace("/", "_") + "_unmapped" + unmappedStrings.unmappedCount + ".csv");
							File.WriteAllLines(csvp, csv);
						}

						if (config.WiiRawCsvOutputPath != null && !kvp.Value.SkipTextMapping) {
							List<string> csv = new List<string>();
							for (int hhh = 0; hhh < wscsorig.Entries.Count; ++hhh) {
								int entry = hhh;
								string orig = wscsorig.Entries[entry];
								string repl = wscs.Entries[entry];
								csv.Add(string.Format("{0}\t{1}\t{2}", entry, CsvEscape(orig, charnamesW), CsvEscape(repl, charnamesWE)));
							}
							Directory.CreateDirectory(config.WiiRawCsvOutputPath);
							string csvp = Path.Combine(config.WiiRawCsvOutputPath, f.Replace("/", "_") + ".csv");
							File.WriteAllLines(csvp, csv);
						}

						bool writeResultsToFileForWiiInject = true;
						if (writeResultsToFileForWiiInject) {
							if (v0outpath != null && f == "rootR.cpk/module/mainRR.sel") {
								WritePatchedFile(Path.Combine(v0outpath, "files", "module", "mainRR.sel"), scsstr);
							}

							if (isFontTexture) {
								if (executableProcessed) {
									InjectFile(map0inject, map1inject, rootinject, f, scsstr);
									fontTextureInjected = true;
								}
							} else if (isExecutable) {
								WritePatchedFile(Path.Combine(v2outpath, "sys", "main.dol"), scsstr);
								if (v0outpath != null) {
									WritePatchedFile(Path.Combine(v0outpath, "sys", "main.dol"), scsstr);
								}
								if (config.RiivolutionOutputPath != null) {
									WritePatchedFile(Path.Combine(config.RiivolutionOutputPath, "main.dol"), scsstr);
								}
							} else {
								if (delayInjection) {
									delayedInjects.Add(f, scsstr);
								} else {
									InjectFile(map0inject, map1inject, rootinject, f, scsstr);
								}
							}
						}
					} else {
						Console.WriteLine("ERROR: Mismatching line count in supposedly confirmed file.");
					}
				}
			}

			if (generateNew) {
				if (newFontTexture != null && !fontTextureInjected) {
					var scsstr = newFontTexture.Duplicate();
					scsstr.Position = 0;
					InjectFile(map0inject, map1inject, rootinject, "rootR.cpk/sys/FontTexture2.tex", scsstr);
				}

				foreach (var kvp in delayedInjects) {
					InjectFile(map0inject, map1inject, rootinject, kvp.Key, kvp.Value);
				}
				delayedInjects.Clear();

				if (config.RiivolutionOutputPath != null) {
					DateTime generationTime = DateTime.Now;
					for (int gameversion = 0; gameversion < 4; gameversion += 2) {
						Directory.CreateDirectory(config.RiivolutionOutputPath);
						StringBuilder xml = new StringBuilder();
						xml.AppendLine("<wiidisc version=\"1\">");
						xml.AppendFormat("\t<id game=\"STGJ\" version=\"{0}\" />", gameversion).AppendLine();
						xml.AppendLine("\t<options>");
						xml.AppendLine("\t\t<section name=\"English Patch\">");
						xml.AppendFormat("\t\t\t<option name=\"English Patch {0:yyyy-MM-dd HH-mm-ss}\" default=\"1\">", generationTime).AppendLine();
						xml.AppendLine("\t\t\t\t<choice name=\"Enabled\">");
						xml.AppendLine("\t\t\t\t\t<patch id=\"eng\" />");
						xml.AppendLine("\t\t\t\t</choice>");
						xml.AppendLine("\t\t\t</option>");
						xml.AppendLine("\t\t</section>");
						xml.AppendLine("\t</options>");
						xml.AppendLine("\t<patch id=\"eng\" root=\"/graces_english\">");
						map0inject.GenerateRiivolutionData(xml, config.RiivolutionOutputPath, "map0R.cpk", gameversion == 2);
						map1inject.GenerateRiivolutionData(xml, config.RiivolutionOutputPath, "map1R.cpk", gameversion == 2);
						rootinject.GenerateRiivolutionData(xml, config.RiivolutionOutputPath, "rootR.cpk", gameversion == 2);
						xml.AppendLine("\t\t<file disc=\"main.dol\" external=\"main.dol\" />");
						xml.AppendLine("\t</patch>");
						xml.AppendLine("</wiidisc>");
						File.WriteAllText(Path.Combine(config.RiivolutionOutputPath, string.Format("STGJv{0}.xml", gameversion)), xml.ToString());
					}
				}

				map0inject.Close();
				map1inject.Close();
				rootinject.Close();
			}

			return;
		}

		private static void ApplyPrefilter(List<(int index, string entry)> j, List<(int index, string entry)> u, List<(string j, string expectedU, string replacementU)> prefilterStrings) {
			foreach (var prefilter in prefilterStrings) {
				for (int i = 0; i < j.Count; ++i) {
					if (j[i].entry == prefilter.j && u[i].entry == prefilter.expectedU) {
						u[i] = (u[i].index, prefilter.replacementU);
					}
				}
			}
		}

		private static void WritePatchedFile(string outfilename, Stream scsstr) {
			Directory.CreateDirectory(Path.GetDirectoryName(outfilename));
			using (var fs = new FileStream(outfilename, FileMode.Create)) {
				long p = scsstr.Position;
				scsstr.Position = 0;
				StreamUtils.CopyStream(scsstr, fs, scsstr.Length);
				scsstr.Position = p;
			}
		}
	}
}
