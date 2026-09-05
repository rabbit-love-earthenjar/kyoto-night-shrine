import { FileBlob, PresentationFile } from "file:///C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const source = "F:/夜神社カフエ/Applications/Kairosoft/夜神社カフェ_カイロソフト応募企画書_JP_v4_3.pptx";
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
const slides = presentation.slides;
const first = slides.getItem(0);

function methods(value) {
  const out = [];
  let proto = value;
  while (proto && proto !== Object.prototype) {
    for (const name of Object.getOwnPropertyNames(proto)) {
      if (!out.includes(name)) out.push(name);
    }
    proto = Object.getPrototypeOf(proto);
  }
  return out.sort();
}

console.log(JSON.stringify({
  slideCount: slides.items?.length,
  slideCollection: methods(slides),
  slide: methods(first),
  firstOwnKeys: Object.keys(first),
  slideItemsType: typeof slides.items,
}, null, 2));
