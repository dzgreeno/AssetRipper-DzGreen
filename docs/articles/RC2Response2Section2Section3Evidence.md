# RC2 Response 2 — أدلة Section 2 وSection 3

**الفرع:** `dzgreen-vnext-hardening`  
**النشر الخارجي:** لا يوجد push أو Release.  
**سياسة المصدر:** اختُبرت F1 وF2 من أرشيفات قدمها المستخدم محليًا، بينما F3 موسومة `synthetic-verified` ولا تمثل توافق Unity حديثًا.

## Section 2

| البند | التنفيذ | الدليل | الحالة |
| --- | --- | --- | --- |
| 2.1 GLB fallback | `GlbFallbackTextureCatalog` يتحقق من وجود وحجم وصلاحية image، ويطبع key. `GlbLevelBuilder` يقرأ catalog فقط عندما يحل PPtr إلى object غير `ITexture2D`؛ resolved texture لا يستبدل، وnull يستخدم neutral 1×1. CLI `--glb --fallback-textures` يمرر catalog فعليًا. | اختبار Premium: 23 بعد catalog، ثم 25 بعد metadata/audio. F1 وF2 GLB مع catalog قبلا fallback file بلا rejections. | منفذ؛ إثبات code-level يضمن عدم overwrite للـresolved، وrun-time GLB تم على F1/F2. |
| 2.2 mip/color space | `PremiumTextureTranscoder.FromExposedSchema` يخرج `Exposed` وsRGB/Linear فقط عند تقديم schema metadata؛ null يخرج `NotExposed` و`Unknown`. `TryExportExposedMipChain` يحفظ levels المقدمة فقط ولا يولد level. | synthetic-verified test يغطي exposed وnot-exposed. لا يكشف IImageTexture الحالي fields مضمونة للمips/color-space في العينة، لذلك runtime reports تبقى NotExposed/Unknown. | منفذ ضمن الحدود؛ انتظار schema fixture حقيقي لإثبات branch exposed من importer. |
| 2.3 Audio/Video | `TryNormalizeAudio` يمرر WAV/OGG كما هي، ويستخدم converter القائم للـOGG→WAV؛ يرفض relabel أو codec fabrication. Video passthrough الحالي يتطلب integrity وامتدادًا ومحتوى مقروءًا. | synthetic-verified audio test يثبت WAV/OGG والرفض. لا تحتوي F1/F2 المستخرجة على AudioClip/VideoClip قابل للقراءة للتحويل النهائي. | منفذ، لكن conversion acceptance على Unity clip حقيقي مفتوح. |
| 2.4 compression acceptance | `TextureConverter` الحالي يربط ASTC، ETC/EAC/ETC2، PVRTC، Crunch، BC/DXT بمفككات محددة؛ تصدير `--textures` يحكمه decoder وintegrity فقط. | F2 raw JSON أظهر formats numeric `50` و`51` وتصدير textures أنتج manifests لكل PNG/TGA/EXR. لم تتوفر fixture حقيقية معلنة لكل family، ولا تحوّل synthetic metadata إلى ادعاء decode. | decoder-path evidence فقط؛ acceptance per-family حقيقي مفتوح. |

## Section 3

| fixture | provenance | verified-only + diagnostics | textures PNG/TGA/EXR | fallback GLB | verifier |
| --- | --- | --- | --- | --- | --- |
| F1 | user-supplied-authorized، `character.rar` | نجح `exit 0` على `hero20050.unity3d` | نجحت الأوامر الثلاثة، 23 ملف output لكل تشغيل بما فيها manifest | المحاولة غير المفلترة رفضت تعدد الجذور كما ينبغي؛ إعادة التشغيل بـ`--filter hero20050` نجحت وأنتجت GLB | GLB/inspection comparison محفوظ؛ meshes 8→8، vertices 7377→7361، clips 91→79. الفرق يسجل كملاحظة، لا claim تطابق. |
| F2 | user-supplied-authorized، subset متعدد الملفات من `android.rar` | نجح `exit 0`، 534 asset في GLB run | نجحت الأوامر الثلاثة؛ خرج manifest لكل format | نجح `exit 0` وأنتج `KarlaKick@KarlaKick_rig...glb` بلا catalog rejection | المسار نجح؛ inspection zero بينما GLB zero في root المختار، فلا يستنتج أن bundle كاملة خالية. |
| F3 | synthetic-verified فقط | نجح `exit 0` مع 0 assets وdiagnostics/manifest فارغين | نجحت الأوامر الثلاثة، manifest صفر assets | يرفض GLB بـ`No character or prefab root was found` كما هو متوقع للـfixture الفارغ | غير قابل للتشغيل لغياب GLB حقيقي؛ لا claim Unity 2021.3/2022.3. |

## Verifier policy

`tools/RC2HeadlessVerifier` لا يعيد استيراد Unity أو يفسر proprietary data. يقرأ inspection JSON الصادر من CLI وJSON chunk داخل GLB، ويقارن side-by-side meshes وvertices وbones وbind poses وblend shapes وclips وmaterials وtextures. الفرق يبقى observation لأن تصدير GLB قد يغير primitive boundaries أو يستبعد مسار غير مدعوم.

## ملفات الأدلة

| المسار | المحتوى |
| --- | --- |
| `tests/fixtures/rc2/fixture-profiles.json` | provenance وتعريف F1/F2/F3. |
| `tests/fixtures/rc2/F3-synthetic-verified-README.md` | حدود F3 الواضحة. |
| `/tmp/rc2-glb-fallback-test.log` | Premium tests: 23 ناجحًا. |
| `/tmp/rc2-texture-metadata-test.log` | Premium tests: 24 ناجحًا. |
| `/tmp/rc2-audio-normalization-test.log` | Premium tests: 25 ناجحًا. |
| `/tmp/rc2-glb-cli-build.log` | CLI build: 0 warnings، 0 errors. |
| `/tmp/assetripper-rc2-response2-trials/` | CLI trials وdiagnostics/manifests وGLB وverifier outputs. |
