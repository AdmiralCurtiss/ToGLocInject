using HyoutaPluginBase;
using HyoutaTools.Tales.Vesperia.FPS4;
using HyoutaTools.Tales.Vesperia.Texture;
using HyoutaUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToGLocInject {
	public class SkitTexCache {
		public class SkitTex {
			public DuplicatableStream Stream;

			public override string ToString() {
				using (FPS4 fps4 = new FPS4(Stream.Duplicate()))
				using (DuplicatableStream txmvStream = fps4.GetChildByIndex(1).AsFile.DataStream.Duplicate())
				using (FPS4 txmv = new FPS4(txmvStream.Duplicate()))
				using (DuplicatableStream txmStream = txmv.GetChildByIndex(0).AsFile.DataStream.Duplicate()) {
					TXM txm = new TXM(txmStream.Duplicate());
					return txm.TXMRegulars[0].Name;
				}
			}
		}

		private Dictionary<uint, SkitTex> Cache;

		public SkitTexCache() {
			Cache = new Dictionary<uint, SkitTex>();
		}

		public void AddTextureIfNotExists(uint key, DuplicatableStream texture) {
			SkitTex containedTexture;
			if (Cache.TryGetValue(key, out containedTexture)) {
				const bool verify = false;
				if (verify) {
					using (DuplicatableStream otex = containedTexture.Stream.Duplicate())
					using (DuplicatableStream ntex = texture.Duplicate()) {
						otex.Position = 0;
						ntex.Position = 0;
						if (!(otex.Length == ntex.Length && StreamUtils.IsIdentical(otex, ntex, otex.Length))) {
							throw new Exception("texture added does not match known texture");
						}
					}
				}
			} else {
				Cache.Add(key, new SkitTex() { Stream = texture.Duplicate() });
			}
		}

		public DuplicatableStream GetTextureStream(uint v) {
			return Cache[v].Stream.Duplicate();
		}
	}
}
