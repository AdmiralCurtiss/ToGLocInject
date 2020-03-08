using System;
using System.Collections.Generic;
using System.Text;
using HyoutaTools.Tales.Graces.SCS;
using HyoutaUtils;

namespace ToGLocInject {
	internal static class MainDolPostProcess {
		internal static void PostProcessMainDolReplacements(ToGLocInject.FileFetcher _fc, HyoutaTools.Tales.Graces.CharNameBin charnamesU, SCS w, SCS worig, List<(int index, string entry)> j, List<(int index, string entry)> u, Dictionary<char, (int w1, int w2)> charToWidthMap) {
			// point per-language wii files at the single one that actually exists on the disc
			for (int i = 1; i <= 8; ++i) {
				w.Entries[i] = w.Entries[0];
			}

			// remove arte furigana
			for (int i = 7645; i <= 8465; i += 3) {
				w.Entries[i] = null;
			}

			w.Entries[1147] = j[161].entry.Substring(0, 3) + " " + u[161].entry;

			// replace localized wii error messages with english ones
			for (int i = 935; i <= 941; ++i) {
				w.Entries[i] = w.Entries[936];
			}
			for (int i = 942; i <= 948; ++i) {
				w.Entries[i] = w.Entries[943];
			}
			for (int i = 949; i <= 955; ++i) {
				w.Entries[i] = w.Entries[950];
			}
			w.Entries[984] = w.Entries[985];
			// TODO: english messages for this block are not proper english, fix that
			for (int i = 956; i <= 983; i += 4) {
				w.Entries[i + 0] = w.Entries[960];
				w.Entries[i + 1] = w.Entries[961];
				w.Entries[i + 2] = w.Entries[962];
				w.Entries[i + 3] = w.Entries[963];
			}

			w.Entries[1187] = w.Entries[1187].Substring(0, 9) + u[204].entry.Substring(10);

			// party member names
			w.Entries[1370] = charnamesU.GetName(1001).regular;
			w.Entries[1371] = charnamesU.GetName(1002).regular;
			w.Entries[1372] = charnamesU.GetName(1003).regular;
			w.Entries[1373] = charnamesU.GetName(1004).regular;
			w.Entries[1374] = charnamesU.GetName(1005).regular;
			w.Entries[1375] = charnamesU.GetName(1006).regular;
			w.Entries[1376] = charnamesU.GetName(1007).regular;
			w.Entries[1377] = charnamesU.GetName(1022).regular;
			w.Entries[1378] = charnamesU.GetName(1009).alt;
			w.Entries[1379] = charnamesU.GetName(1002).alt;

			w.Entries[1447] = u[152].entry;
			w.Entries[1449] = u[508].entry.Substring(6);
			w.Entries[1450] = u[574].entry.Substring(12);
			w.Entries[1459] = u[224].entry;
			w.Entries[1480] = u[227].entry;

			w.Entries[1639] = u[570].entry.Substring(0, 16) + u[570].entry.Substring(20, 7) + u[570].entry.Substring(31);
			w.Entries[1642] = u[573].entry.Substring(0, 15) + u[573].entry.Substring(19, 7) + u[573].entry.Substring(30);
			w.Entries[1669] = w.Entries[1668];

			// menu strings
			// TODO: is this right?
			w.Entries[1496] = u[392].entry;
			w.Entries[1498] = u[3503].entry.Substring(4, 5);
			w.Entries[1501] = u[3506].entry.Substring(4, 6);
			w.Entries[1502] = u[3507].entry.Substring(4, 7);
			w.Entries[1503] = u[3508].entry.Substring(4, 6);
			w.Entries[1504] = u[3509].entry.Substring(10, 5);
			w.Entries[1505] = u[3510].entry.Substring(4, 7);
			w.Entries[1506] = u[3511].entry.Substring(4, 8);
			w.Entries[1507] = u[476].entry.Substring(0, 4);
			w.Entries[1508] = u[476].entry.Substring(5, 4);
			w.Entries[1509] = u[700].entry;
			// TODO: is this right?
			w.Entries[1510] = u[691].entry;

			w.Entries[1539] = u[3503].entry.Substring(4, 5);
			w.Entries[1541] = u[3508].entry.Substring(4, 6);
			w.Entries[1542] = u[3511].entry.Substring(4, 8);

			w.Entries[1581] = u[512].entry;
			w.Entries[1558] = w.Entries[1558].Substring(19, 1).ToUpperInvariant() + w.Entries[1558].Substring(20, w.Entries[1558].Length - 21);

			w.Entries[1606] = w.Entries[1606] + " ";

			// TODO: are these right?
			w.Entries[1614] = u[442].entry + u[544].entry.Substring(5);
			w.Entries[1616] = u[444].entry + u[544].entry.Substring(5);
			w.Entries[1619] = w.Entries[1619].Substring(0, 6);

			// shop menu prepends item name to these strings if you buy/sell a single item
			// which looks very wrong in english without the linebreak
			w.Entries[1769] = "\n" + w.Entries[1769];
			w.Entries[1770] = "\n" + w.Entries[1770];

			// TODO: is this right?
			w.Entries[1771] = u[475].entry;

			w.Entries[1796] = u[759].entry.Substring(0, 12) + u[744].entry.Substring(13);

			// TODO: are these right?
			w.Entries[1800] = u[309].entry;
			w.Entries[1807] = "";
			w.Entries[1814] = u[762].entry;
			w.Entries[1826] = u[765].entry;
			w.Entries[1839] = u[783].entry;

			// wii-specific stuff from 1740 to 1745, TODO: translate
			// more wii-specific stuff from 1916 to 1970, TODO: translate
			// lots of online related stuff in range 2084-2271, most of which was removed or reworked in PS3, TODO: translate

			w.Entries[2360] = u[1315].entry;
			w.Entries[2400] = u[1355].entry;
			w.Entries[2420] = u[1387].entry;
			w.Entries[2451] = u[1418].entry;
			w.Entries[2559] = u[1526].entry;
			w.Entries[2560] = u[1527].entry;

			// equipment bonus effect name parts
			for (int i = 2329; i < 2605; i += 4) {
				w.Entries[i + 2] = " " + w.Entries[i + 2];
				w.Entries[i + 3] = "-" + w.Entries[i + 3];
			}

			// TODO: is this right?
			w.Entries[2630] = u[1609].entry;
			w.Entries[2737] = u[1716].entry;
			// TODO: is this right? seems to be different between versions
			w.Entries[2766] = u[1745].entry;
			w.Entries[2773] = u[1752].entry;
			w.Entries[2774] = u[1753].entry;

			w.Entries[2801] = u[673].entry;
			// TODO: is this right?
			w.Entries[2974] = u[1997].entry;

			// TODO: these seem to have been changed between versions, not sure if it's actually right just injecting the PS3 text
			w.Entries[3261] = u[2250].entry;
			w.Entries[3262] = u[2251].entry;
			w.Entries[3263] = u[2252].entry;
			w.Entries[3265] = u[2254].entry;

			// these seem like debug warp menu strings
			var mapname = new SCS(_fc.GetFile("rootR.cpk/str/ja/MapName.bin", Version.U));
			w.Entries[1776] = mapname.Entries[61].Substring(0, 6);
			w.Entries[1777] = mapname.Entries[24].Substring(0, 7);
			w.Entries[1778] = mapname.Entries[32].Substring(0, 6);
			w.Entries[1779] = mapname.Entries[76].Substring(0, 7) + mapname.Entries[549].Substring(10, 1) + mapname.Entries[1100].Substring(0, 5);
			w.Entries[1780] = mapname.Entries[80];
			w.Entries[1781] = mapname.Entries[88];

			w.Entries[1851] = "";
			w.Entries[1901] = "";
			w.Entries[1903] = "";
			w.Entries[1904] = "";
			w.Entries[1905] = "";
			w.Entries[1906] = "";

			// TODO: check these if they're right
			w.Entries[2052] = u[1006].entry;
			w.Entries[2053] = u[1007].entry;

			w.Entries[2185] = u[1146].entry.Substring(0, 4) + u[1146].entry.Substring(21);

			w.Entries[3288] = u[2279].entry.ReplaceSubstring(6, 9, worig.Entries[3288], 0, 27);
			w.Entries[3292] = u[2283].entry.ReplaceSubstring(6, 9, worig.Entries[3292], 0, 27);
			w.Entries[3296] = u[2287].entry.ReplaceSubstring(14, 9, worig.Entries[3296], 18, 19).ReplaceSubstring(4, 9, worig.Entries[3296], 0, 17);
			w.Entries[3300] = u[2291].entry.ReplaceSubstring(14, 9, worig.Entries[3300], 18, 19).ReplaceSubstring(4, 9, worig.Entries[3300], 0, 17);
			w.Entries[3304] = u[2295].entry.ReplaceSubstring(4, 9, worig.Entries[3304], 0, 27);
			w.Entries[3308] = u[2299].entry.ReplaceSubstring(6, 9, worig.Entries[3308], 0, 27);
			w.Entries[3312] = u[2303].entry.ReplaceSubstring(19, 9, worig.Entries[3312], 0, 27);
			w.Entries[3316] = u[2307].entry.ReplaceSubstring(28, 9, worig.Entries[3316], 11, 27);
			w.Entries[3320] = u[2311].entry.ReplaceSubstring(5, 9, worig.Entries[3320], 0, 9);
			for (int i = 3287; i < 3315; i += 4) {
				string[] split = w.Entries[i + 2].Split('\n');
				w.Entries[i + 2] = split[0];
				w.Entries[i + 3] = split[1];
			}
			for (int i = 3323; i < 3967; i += 7) {
				if (i == 3666 || w.Entries[i + 2]?.Trim() == "" || w.Entries[i + 3]?.Trim() == "" || w.Entries[i + 4]?.Trim() == "" || w.Entries[i + 5]?.Trim() == "" || w.Entries[i + 6]?.Trim() == "") {
					w.Entries[i + 2] = null;
					w.Entries[i + 3] = null;
					w.Entries[i + 4] = null;
					w.Entries[i + 5] = null;
					w.Entries[i + 6] = null;
				}
			}
			w.Entries[3324] = u[2327].entry;
			w.Entries[3331] = u[2334].entry;
			w.Entries[3373] = u[2376].entry.Insert(158, " ");
			w.Entries[3422] = u[2425].entry;
			w.Entries[3569] = u[2600].entry;
			w.Entries[3610] = u[2669].entry;
			w.Entries[3625] = u[2684].entry;
			w.Entries[3674] = u[2740].entry;
			w.Entries[3772] = u[2838].entry;
			w.Entries[3877] = u[2964].entry;
			w.Entries[3968] = u[3146].entry.Split('\n')[0];
			w.Entries[3969] = u[3146].entry.Split('\n')[1];
			w.Entries[3970] = u[3146].entry.Split('\n')[2];
			w.Entries[3975] = u[3153].entry.Split('\n')[0];
			w.Entries[3976] = u[3153].entry.Split('\n')[1];
			w.Entries[3977] = u[3153].entry.Split('\n')[2];
			w.Entries[3978] = u[3153].entry.Split('\n')[3].ReplaceSubstring(0, 9, worig.Entries[3978], 0, 9);
			w.Entries[3979] = u[3153].entry.Split('\n')[4].ReplaceSubstring(0, 9, worig.Entries[3979], 0, 9);
			w.Entries[3982] = u[3160].entry.Split('\n')[0];
			w.Entries[3983] = u[3160].entry.Split('\n')[1];
			w.Entries[3984] = u[3160].entry.Split('\n')[2];
			w.Entries[3985] = u[3160].entry.Split('\n')[3];
			w.Entries[3986] = u[3160].entry.Split('\n')[4].ReplaceSubstring(0, 18, worig.Entries[3986], 0, 9);
			w.Entries[3989] = u[3167].entry.Split('\n')[0];
			w.Entries[3990] = u[3167].entry.Split('\n')[1];
			w.Entries[3991] = u[3167].entry.Split('\n')[2];
			w.Entries[3996] = u[3174].entry.Split('\n')[0];
			w.Entries[3997] = u[3174].entry.Split('\n')[1];
			w.Entries[3998] = u[3174].entry.Split('\n')[2];
			w.Entries[3999] = u[3174].entry.Split('\n')[3];
			w.Entries[4003] = u[3181].entry.ReplaceSubstring(5, 9, worig.Entries[4003], 0, 9);
			w.Entries[4016] = u[3194].entry;
			w.Entries[4052] = u[3202].entry;
			w.Entries[4129] = u[3286].entry.ReplaceSubstring(187, 9, worig.Entries[4133], 4, 9);
			w.Entries[4157] = u[3314].entry;
			w.Entries[4262] = u[3426].entry;
			for (int i = 4002; i < 4023; i += 7) {
				for (int k = 2; k < 7; ++k) {
					w.Entries[i + k] = null;
				}
			}
			for (int i = 4051; i < 4331; i += 7) {
				for (int k = 2; k < 7; ++k) {
					w.Entries[i + k] = null;
				}
			}

			w.Entries[4395] = u[3575].entry;
			w.Entries[4405] = u[3585].entry;
			w.Entries[4410] = u[3590].entry;
			w.Entries[4412] = u[3592].entry;
			w.Entries[4536] = u[3740].entry;
			w.Entries[4537] = u[3741].entry;
			w.Entries[4538] = u[3742].entry;

			w.Entries[4668] = u[3890].entry;
			w.Entries[4669] = u[3891].entry;
			w.Entries[4684] = u[3906].entry;
			w.Entries[4734] = u[3956].entry;

			w.Entries[4862] = u[4098].entry;
			w.Entries[4958] = u[4194].entry;
			w.Entries[5051] = u[4143].entry;
			w.Entries[5052] = u[4145].entry;
			w.Entries[5407] = u[4700].entry;
			w.Entries[5527] = u[4850].entry;
			w.Entries[5585] = u[4926].entry;
			w.Entries[5597] = u[4938].entry;
			w.Entries[5613] = u[4954].entry;
			w.Entries[5621] = u[4962].entry;
			w.Entries[5633] = u[4974].entry;
			w.Entries[5635] = u[4976].entry;
			w.Entries[5639] = u[4980].entry;
			w.Entries[5641] = u[4982].entry;
			w.Entries[5643] = u[4984].entry;
			w.Entries[5645] = u[4986].entry;
			w.Entries[5647] = u[4988].entry;
			w.Entries[5649] = u[4990].entry;
			w.Entries[5651] = u[4992].entry;
			w.Entries[5653] = u[4994].entry;
			w.Entries[5655] = u[4996].entry;
			w.Entries[5657] = u[4998].entry;
			w.Entries[5659] = u[5000].entry;
			w.Entries[5661] = u[5002].entry;
			w.Entries[5663] = u[5004].entry;
			w.Entries[5673] = u[5014].entry;
			w.Entries[5679] = u[5020].entry;
			w.Entries[5681] = u[5022].entry;
			{
				string post = u[5764].entry.Substring(u[5764].entry.Length - 2, 2) + u[737].entry;
				w.Entries[6369] = u[5764].entry.Substring(0, 21) + u[5764].entry.Substring(33, 6) + post;
				w.Entries[6371] = u[5766].entry.Substring(0, 21) + u[5766].entry.Substring(33, 9) + post;
				w.Entries[6373] = u[5768].entry.Substring(0, 21) + u[5768].entry.Substring(33, 6) + post;
				w.Entries[6375] = u[5770].entry.Substring(0, 21) + u[5770].entry.Substring(33, 4) + post;
				w.Entries[6377] = u[5772].entry.Substring(0, 21) + u[5772].entry.Substring(33, 7) + post;
				w.Entries[6379] = u[5774].entry.Substring(0, 21) + u[5774].entry.Substring(33, 4) + post;
				w.Entries[6381] = u[5776].entry.Substring(0, 21) + u[5776].entry.Substring(33, 8) + post;
				w.Entries[6383] = u[5778].entry.Substring(0, 21) + u[5778].entry.Substring(33, 5) + post;
				w.Entries[6385] = u[5780].entry.Substring(0, 21) + u[5780].entry.Substring(33, 4) + post;
			}
			w.Entries[6395] = u[5864].entry.ReplaceSubstring(60, 9, worig.Entries[6395], 24, 27);
			w.Entries[6415] = u[5884].entry;
			w.Entries[6437] = u[5906].entry;

			// TODO: are these right?
			w.Entries[6533] = u[6018].entry.ReplaceSubstring(26, 2, worig.Entries[6533].ConvertFullwidthToAscii(), 6, 1);
			w.Entries[6575] = u[6060].entry.Remove(38, 16).ReplaceSubstring(39, 1, worig.Entries[6575], 18, 1);
			w.Entries[6579] = u[6064].entry.Remove(38, 16).ReplaceSubstring(39, 1, worig.Entries[6579], 18, 1);
			w.Entries[6583] = u[6068].entry.Remove(38, 16).ReplaceSubstring(39, 1, worig.Entries[6583], 18, 1);
			w.Entries[6599] = u[6084].entry;

			w.Entries[6665] = u[6150].entry;
			w.Entries[6683] = u[6168].entry;
			w.Entries[6997] = u[6560].entry;
			w.Entries[7163] = u[6726].entry;
			w.Entries[7319] = u[6882].entry;
			w.Entries[7552] = u[7115].entry;
			w.Entries[7570] = u[7133].entry;
			w.Entries[7572] = u[7135].entry;
			w.Entries[7715] = u[7278].entry;
			w.Entries[7730] = u[7293].entry.Substring(0, 59);
			{
				string repl = w.Entries[7739].Substring(15, 27);
				string orig = u[7311].entry.Substring(48, 9);
				w.Entries[7739] = u[7311].entry.Replace(orig, repl);
				w.Entries[7742] = u[7314].entry.Replace(orig, repl);
				w.Entries[7745] = u[7317].entry.Replace(orig, repl);
				w.Entries[7853] = u[7434].entry.Replace(orig, repl);
				w.Entries[7856] = u[7437].entry.Replace(orig, repl);
				w.Entries[7859] = u[7440].entry.Replace(orig, repl);
				w.Entries[7937] = u[7527].entry.Replace(orig, repl);
				w.Entries[7940] = u[7530].entry.Replace(orig, repl);
				w.Entries[7943] = u[7533].entry.Replace(orig, repl);
				w.Entries[8039] = u[7638].entry.Replace(orig, repl);
				w.Entries[8042] = u[7641].entry.Replace(orig, repl);
				w.Entries[8045] = u[7644].entry.Replace(orig, repl);
				w.Entries[8126] = u[7734].entry.Replace(orig, repl);
				w.Entries[8129] = u[7737].entry.Replace(orig, repl);
				w.Entries[8132] = u[7740].entry.Replace(orig, repl);
				w.Entries[8213] = u[7830].entry.Replace(orig, repl);
				w.Entries[8216] = u[7833].entry.Replace(orig, repl);
				w.Entries[8219] = u[7836].entry.Replace(orig, repl);
				w.Entries[8255] = u[7905].entry.Replace(orig, repl);
			}
			// TODO: is this right?
			w.Entries[7820] = u[7395].entry;
			// TODO: JP for this this seems completely different?
			w.Entries[7886] = u[7470].entry;
			// TODO: is this right?
			w.Entries[8201] = u[7812].entry;
			w.Entries[8330] = u[7881].entry;
			w.Entries[8471] = u[8106].entry.ReplaceSubstring(21, 6, u[8105].entry.ToLowerInvariant(), 0, 6);
			w.Entries[8483] = u[8118].entry.ReplaceSubstring(35, 6, u[8105].entry.ToLowerInvariant(), 0, 6);
			w.Entries[8485] = u[8120].entry.ReplaceSubstring(33, 6, u[8105].entry.ToLowerInvariant(), 0, 6);
			w.Entries[8541] = u[8186].entry.ReplaceSubstring(10, 2, worig.Entries[8541].ConvertFullwidthToAscii(), 5, 2);
			w.Entries[8612] = u[409].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[8693 + i * 2] = u[8348 + i * 2].entry;
			}
			w.Entries[8751] = u[8406].entry.ReplaceSubstring(50, 3, worig.Entries[8751].ConvertFullwidthToAscii(), 0, 3);
			w.Entries[8753] = u[8408].entry.ReplaceSubstring(50, 4, worig.Entries[8753].ConvertFullwidthToAscii(), 0, 4);
			w.Entries[8761] = u[8416].entry;
			w.Entries[8783] = u[8438].entry;
			w.Entries[8859] = u[8522].entry;
			w.Entries[8891] = u[8554].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[9001 + i * 2] = u[8700 + i * 2].entry;
			}
			w.Entries[9073] = u[8772].entry;
			w.Entries[9203] = u[8910].entry;
			for (int i = 0; i < 7; ++i) {
				w.Entries[9262 + i] = u[8999 + i].entry;
			}
			w.Entries[9277] = u[9014].entry;
			w.Entries[9279] = u[9016].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[9287 + i * 2] = u[9024 + i * 2].entry;
			}
			w.Entries[9473] = u[9218].entry;
			w.Entries[9475] = u[9220].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[9555 + i * 2] = u[9328 + i * 2].entry;
			}
			w.Entries[9739] = u[9520].entry;
			w.Entries[9741] = u[9522].entry;
			w.Entries[9768] = u[9573].entry;
			w.Entries[9769] = u[9574].entry;
			for (int i = 0; i < 13; ++i) {
				w.Entries[9792 + i] = u[9603 + i].entry;
			}
			for (int i = 0; i < 4; ++i) {
				w.Entries[9821 + i * 2] = u[9632 + i * 2].entry;
			}
			w.Entries[9871] = u[9682].entry;
			w.Entries[10009] = u[9828].entry;
			w.Entries[10011] = u[9830].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[10091 + i * 2] = u[9938 + i * 2].entry;
			}
			w.Entries[10147] = u[9994].entry;
			w.Entries[10149] = u[9996].entry;
			for (int i = 0; i < 4; ++i) {
				w.Entries[10335 + i * 2] = u[10230 + i * 2].entry;
			}

			// reformat tutorials
			for (int i = 3323; i < 3967; i += 7) {
				TryReformatTutorial(i, w, charToWidthMap, 480);
			}
			for (int i = 4002; i < 4331; i += 7) {
				TryReformatTutorial(i, w, charToWidthMap, 480);
			}

			// reformat synposis
			for (int i = 4384; i < 4419; ++i) {
				ReformatSynopsis(i, w, charToWidthMap, 500);
			}

			// trim off excess whitespace at end of strings
			w.Entries[1235] = w.Entries[1235].TrimEnd();
			w.Entries[1559] = w.Entries[1559].TrimEnd();
			w.Entries[1582] = w.Entries[1582].TrimEnd();
			w.Entries[2075] = w.Entries[2075].TrimEnd();
			for (int i = 3287; i < 10456; ++i) {
				if (w.Entries[i] != null) {
					w.Entries[i] = w.Entries[i].TrimEnd();
					while (w.Entries[i].Contains(" \n")) {
						w.Entries[i] = w.Entries[i].Replace(" \n", "\n");
					}
				}
			}

			// replace strings with their ID to identify them in-game
			bool id_strings = false;
			if (id_strings) {
				for (int i = 986; i < 10456; ++i) {
					if (w.Entries[i] == null || (w.Entries[i] != null && !w.Entries[i].Contains("%"))) {
						w.Entries[i] = "[" + i.ToString() + "]";
					}
				}
			}
		}

		private static List<string> Reformat(string input, Dictionary<char, (int w1, int w2)> charToWidthMap, int perLineMaxWidth) {
			StringBuilder committedText = new StringBuilder();
			StringBuilder uncommittedText = new StringBuilder();
			int committedLineWidth = 0;
			int uncommittedLineWidth = 0;
			List<string> lines = new List<string>(6);
			int currentControlCode = -1;
			StringBuilder currentControlCodeContents = new StringBuilder();
			string currentColorCodeUncommitted = null;
			string currentColorCodeCommitted = null;
			int spaceWidth = charToWidthMap[' '].w2;
			// note: space at end forces last word to be processed
			foreach (char c in input + " ") {
				if (currentControlCode != -1) {
					currentControlCodeContents.Append(c);
					if (c == ')') {
						uncommittedText.Append(currentControlCodeContents);
						if (currentControlCode == 0xF) {
							// guessed these visually...
							int icon = int.Parse(currentControlCodeContents.ToString().Split(new char[] { ',', ')' })[1]);
							if (icon == 1039 || icon == 1040) {
								uncommittedLineWidth += 49;
							} else {
								uncommittedLineWidth += 30;
							}
						} else if (currentControlCode == 0x3) {
							currentColorCodeUncommitted = currentControlCodeContents.ToString();
							if (int.Parse(currentColorCodeUncommitted.Split(new char[] { '(', ')' })[1]) == 0) {
								currentColorCodeUncommitted = null;
							}
						}
						currentControlCode = -1;
						currentControlCodeContents.Clear();
					}
				} else {
					if (c == '\x0F') {
						currentControlCode = 0xF;
						currentControlCodeContents.Append(c);
					} else if (c == '\x03') {
						currentControlCode = 0x3;
						currentControlCodeContents.Append(c);
					} else if (c == '\x01') {
						currentControlCode = 0x1;
						currentControlCodeContents.Append(c);
					} else if (c == ' ') {
						// check if we can commit the current text
						if (committedLineWidth + uncommittedLineWidth + spaceWidth <= perLineMaxWidth) {
							// can commit, do so
							committedLineWidth += uncommittedLineWidth + spaceWidth;
							committedText.Append(uncommittedText);
							committedText.Append(c);
							currentColorCodeCommitted = currentColorCodeUncommitted;
							uncommittedLineWidth = 0;
							uncommittedText.Clear();
						} else if (committedLineWidth + uncommittedLineWidth <= perLineMaxWidth) {
							// can commit but no space fits afterwards, so this line is done after the commit
							committedLineWidth += uncommittedLineWidth;
							committedText.Append(uncommittedText);
							currentColorCodeCommitted = currentColorCodeUncommitted;
							uncommittedLineWidth = 0;
							uncommittedText.Clear();
							lines.Add(committedText.ToString());
							committedLineWidth = 0;
							committedText.Clear();
							if (currentColorCodeCommitted != null) {
								committedText.Append(currentColorCodeCommitted);
							}
						} else {
							// cannot commit, flush line and commit into new string
							lines.Add(committedText.ToString());
							committedLineWidth = 0;
							committedText.Clear();
							if (currentColorCodeCommitted != null) {
								committedText.Append(currentColorCodeCommitted);
							}
							committedLineWidth += uncommittedLineWidth + spaceWidth;
							committedText.Append(uncommittedText);
							committedText.Append(c);
							currentColorCodeCommitted = currentColorCodeUncommitted;
							uncommittedLineWidth = 0;
							uncommittedText.Clear();
						}
					} else {
						uncommittedText.Append(c);
						uncommittedLineWidth += charToWidthMap[c].w2;
					}
				}
			}
			if (committedLineWidth > 0) {
				lines.Add(committedText.ToString().TrimEnd());
			}

			return lines;
		}

		private static void TryReformatTutorial(int idx, SCS w, Dictionary<char, (int w1, int w2)> charToWidthMap, int perLineMaxWidth) {
			// can only reformat when all text in one entry
			if (w.Entries[idx + 2] != null || w.Entries[idx + 3] != null || w.Entries[idx + 4] != null || w.Entries[idx + 5] != null || w.Entries[idx + 6] != null) {
				return;
			}
			if (w.Entries[idx + 1] == null) {
				return;
			}

			List<string> lines = Reformat(w.Entries[idx + 1].Trim().Replace('\n', ' '), charToWidthMap, perLineMaxWidth);

			if (lines.Count > 6) {
				throw new Exception("tutorial too long");
			}

			for (int i = 0; i < lines.Count; ++i) {
				w.Entries[idx + 1 + i] = lines[i];
			}
		}

		private static void ReformatSynopsis(int idx, SCS w, Dictionary<char, (int w1, int w2)> charToWidthMap, int perLineMaxWidth) {
			w.Entries[idx] = string.Join("\n", Reformat(w.Entries[idx].Trim().Replace('\n', ' '), charToWidthMap, perLineMaxWidth));
		}
	}
}
