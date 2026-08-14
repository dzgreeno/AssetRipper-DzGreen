# تقرير بحث وتحسين AssetRipper

**المشروع المستهدف:** [AssetRipper/AssetRipper](https://github.com/AssetRipper/AssetRipper)
**الغرض:** تحسين قابلية التشغيل والتوافق مع ملفات Unity التي يملك المستخدم حق تحليلها، مع تقليل فقدان الملفات وفشل التصدير.
**حدود الاستخدام:** هذه التعديلات لا تتجاوز التشفير أو DRM أو anti-tamper ولا تستخرج أسرارًا أو مفاتيح من ألعاب لا يملك المستخدم حق الوصول إليها.

## الملخص التنفيذي

أظهر المسح أن عبارة «لا يعمل مع أحدث Unity» لا تصف عطلًا واحدًا. توجد فجوات مستقلة في تنسيق SerializedFile، وترويسات TypeTree الخارجية، وخصائص `SerializeReference`، والتخطيطات المولدة الخاصة بالمنصة، ومصدّرات GLB، ومكتبة فك ASTC، إضافة إلى مشكلات تشغيل الواجهة على Windows وmacOS. لذلك فإن إضافة heuristics عامة أو محاولة «كسر» حماية اللعبة قد تزيد تلف الأصول بدل حل المشكلة.

نفّذت في هذه النسخة أربع مجموعات عملية. أولًا، صار تطبيق Windows يخفي نافذة الطرفية افتراضيًا ويظل قادرًا على الاحتفاظ بها عند الحاجة عبر `--keep-console`. ثانيًا، صارت أزرار الحوارات native محمية من النقر المتكرر، وتتعامل مع الإلغاء والأخطاء دون مسح المسار المحدد. ثالثًا، أضيفت حواجز تمنع التصدير إلى جذر القرص أو مجلدات Desktop/Documents/Downloads أو داخل مسار اللعبة المحمّل، مع إبقاء تأكيد حذف مجلد التصدير غير الفارغ. رابعًا، أضيفت القراءة الأولية لبنية SerializedFile format 23 التي تستخدمها Unity 6000.5.0a5 وما بعدها، بما في ذلك ترويسة `tthm`، وhash/size الخاصين بـ TypeTree الخارجي، ومعالجة `0.0.0a0` كإصدار غير صالح بدل تمريره إلى التصدير.

## نتائج المسح

| المجال | دليل علني | الحالة | القرار |
|---|---|---|---|
| حوار فتح الملفات والمجلدات | مشكلة ظهور الحوار خلف النافذة في Windows x64، مع إصلاح مدمج في NativeDialogs 1.1.2 [1] [2] | قابل للتحسين في الحزمة | إخفاء الطرفية افتراضيًا، إبقاء native dialogs، والتحقق من الاعتماد المدمج |
| تشغيل macOS | الوثيقة الرسمية المجتمعية تتطلب Terminal و`chmod` وتذكر أنها قد تكون قديمة [3] | تغليف ناقص | يحتاج `.app` موقّعًا أو launcher خاصًا بالمنصة في مرحلة لاحقة |
| حذف محتوى مجلد التصدير | الواجهة تحذّر من أن المجلد غير فارغ، لكن اختيار مجلد اللعبة نفسه خطر تشغيلي | قابل للتحسين محليًا | إضافة حماية من الجذر والمسار المستورد، مع استمرار التأكيد قبل الحذف |
| Unity 6000.5 / SerializedFile v23 | Timberborn على `6000.5.2f1` لا يخرج إلا StreamingAssets، والفرع الرسمي التجريبي يضيف دعمًا أوليًا [4] | دعم أولي وليس كاملًا | نقل بنية version 23 الأساسية دون الادعاء بدعم TypeTree خارجي كامل |
| Unity 6000.3 WebGL | اختلاف 940/944 بايت وفشل `UnityConnectSettings` [5] | يحتاج عينات وقراءة خاصة بالمنصة | عدم إضافة تخطٍّ عام للبايتات؛ إبقاء الفشل على مستوى الأصل وتوثيقه |
| Unity 6000.3 URP | `SerializeReference` وتخطيط `UniversalAdditionalLightData` يمنعان إخراج URP كاملًا [6] | فجوة schema حقيقية | تحتاج دعم SerializeReference وtype trees صحيحة، وليست مشكلة ترويسة |
| GLB كبير جدًا | ملفات 300MB–1GB+ قد تفشل بسبب حجز منطقة متصلة، حتى مع ذاكرة حرة كبيرة [7] | المعالج الحالي يعزل الفشل | أولوية لاحقة لإضافة glTF غير ثنائي أو تقسيم المشهد |
| ASTC TextureArray | عطل قديم في ASTC decoder أُغلق بعد تحديث المكتبة [8] | upstream أصلحه | يجب تحديث dependency واختبار TextureArray قبل كل إصدار |
| Mono/IL2Cpp assemblies | الوثائق تذكر أن Mono يحتاج assemblies، وIL2Cpp يحتاج Cpp2IL، وأن Il2CppInterop ليس البديل المناسب [9] | توثيق وتشخيص | إضافة رسائل تشخيصية بدل محاولة تجاوز الحماية |
| CLI/API | نقاش رسمي يطلب CLI أو مكتبة قابلة لإعادة الاستخدام [10] | تحسين معماري | إنشاء wrapper موثّق لاحقًا مع تحقق آمن من المسارات |

## الملفات المعدلة

| الملف | التعديل |
|---|---|
| `Source/AssetRipper.GUI.Free/Program.cs` | إخفاء نافذة Console في Windows عند التشغيل العادي، مع `--keep-console` للتشخيص. |
| `Source/AssetRipper.GUI.Web/Pages/CommandsPage.cs` | تعطيل أزرار native dialogs أثناء الانتظار وإظهار حالة حوار مؤقتة. |
| `Source/AssetRipper.GUI.Web/StaticContent/js/commands_page.js` | إزالة تأخير الحوار غير الضروري، منع الطلبات المتوازية، الحفاظ على المسار عند الإلغاء، والتحقق من HTTP errors. |
| `Source/AssetRipper.GUI.Web/GameFileLoader.cs` | رفض مسارات التصدير الخطرة وتذكّر مسارات الإدخال لمنع التصدير داخل مجلد اللعبة المحمّل. |
| `Source/AssetRipper.IO.Files/SerializedFiles/FormatVersion.cs` | إضافة `ExtractedTypeTreeSupport = 23`. |
| `Source/AssetRipper.IO.Files/SerializedFiles/Parser/SerializedTypeBase.cs` | قراءة `Hash128` وحجم TypeTree الخارجي في format 23، مع دعم TypeTree فارغ عند كون البيانات خارجية. |
| `Source/AssetRipper.IO.Files/SerializedFiles/Parser/TypeTrees/TypeTree.cs` | قراءة ترويسة `tthm`، مطابقة رقم الإصدار، وإتاحة تنظيف TypeTree. |
| `Source/AssetRipper.IO.Files/SerializedFiles/Parser/SerializedFileMetadata.cs` | اعتبار الإصدار الصفري أو غير القابل للتحليل غير صالح واستخدام baseline LTS مع رسالة Auto-Fix. |
| `Source/AssetRipper.Import/Platforms/PlatformGameStructure.cs` | عدم قبول `0.0.0a0` عند اكتشاف إصدار Unity من bundle header. |

## التحقق

تم بناء مشروع `AssetRipper.GUI.Free` بنجاح باستخدام .NET SDK 10 في وضع Release مع `0 Error`. كما اجتاز `node --check` ملف JavaScript الجديد، واجتاز `git diff --check` فحص المسافات. اختبار مشروع IO يستعيد حالة `Build succeeded`؛ لا توجد في هذا المشروع اختبارات تشغيل كاملة لملفات ألعاب تجارية، ولذلك لا أقدّم ادعاءً بأن كل لعبة Unity أو كل نسخة URP ستُفكك بنجاح.

يجب اختبار الحزمة على Windows بملف يملكه المستخدم، وبخاصة: تشغيل EXE بالنقر المزدوج للتأكد من عدم ظهور Terminal، فتح File/Folder dialogs، اختيار مجلد تصدير غير فارغ، محاولة اختيار مجلد اللعبة نفسه للتأكد من رفضه، ثم تجربة ملف Unity 6000.5 مع حفظ `AssetRipper.log`.

## ما لم يُنفذ بعد

لا تزال هناك أعمال منفصلة لا يصح دمجها في «إصلاح عام». دعم `SerializeReference` يتطلب قراءة metadata وtype trees صحيحة، ودعم Unity 6000.3 WebGL يتطلب عينات ملفات حقيقية لتحديد padding والتخطيط الخاص بالمنصة، ودعم URP الكامل يتطلب نماذج schema متوافقة. كما أن تحويل GLB الكبير إلى glTF أو تقسيمه يحتاج تغييرًا في سياسة التصدير وواجهة الإعدادات. وأخيرًا، تشغيل macOS بلا Terminal يتطلب حزمة `.app` وتوقيعًا مناسبًا، وليس مجرد تغيير C#.

## سياسة الأمان والحقوق

يمكن تحسين قراءة الصيغ المفتوحة، التعامل مع الملفات التالفة، دعم نسخ Unity المعلنة، واستعادة الأصول التي يملك المستخدم حق تحليلها. لا يمكنني تنفيذ أو تضمين آليات لتجاوز DRM أو التشفير أو anti-tamper أو حماية الوصول أو استخراج مفاتيح الألعاب. هذه القيود لا تمنع تحسين AssetRipper كأداة تحليل وتوافق؛ بل تمنع تحويله إلى أداة لاختراق ألعاب أو خدمات الغير.

## الأولويات المقترحة للنسخة التالية

| الأولوية | العمل | معيار النجاح |
|---:|---|---|
| 1 | دعم SerializeReference مع type-tree fixtures عامة | تصدير URP Settings دون إسقاط القوائم المرجعية أو تحويلها إلى بيانات فارغة |
| 2 | استكمال external TypeTree bundle في format 23 | استخراج Timberborn 6000.5.2f1 مع إصدار Unity صحيح بدل `0.0.0a0` |
| 3 | GLB fallback إلى glTF أو تقسيم المشاهد | استمرار التصدير بعد فشل نموذج ضخم مع ملف قابل للاستخدام |
| 4 | Native launcher للمنصات | Windows بلا Console، macOS `.app`، Linux `.desktop` مع logs قابلة للوصول |
| 5 | CLI موثّق | تشغيل import/export من سطر الأوامر دون الاعتماد على POST غير موثّق |

## References

[1]: https://github.com/AssetRipper/AssetRipper/issues/2165 "AssetRipper issue #2165: dialogs in background"

[2]: https://github.com/AssetRipper/AssetRipper.NativeDialogs/pull/16 "AssetRipper.NativeDialogs PR #16"

[3]: https://assetripper.github.io/AssetRipper/articles/RunningOnMac.html "Running AssetRipper on macOS"

[4]: https://github.com/AssetRipper/AssetRipper/issues/2304 "AssetRipper issue #2304: SerializedFile version 23"

[5]: https://github.com/AssetRipper/AssetRipper/issues/2237 "AssetRipper issue #2237: Unity 6000.3 WebGL padding"

[6]: https://github.com/AssetRipper/AssetRipper/issues/2207 "AssetRipper issue #2207: URP export"

[7]: https://github.com/AssetRipper/AssetRipper/issues/1863 "AssetRipper issue #1863: very large file extraction"

[8]: https://github.com/AssetRipper/AssetRipper/issues/2251 "AssetRipper issue #2251: ASTC TextureArray"

[9]: https://assetripper.github.io/AssetRipper/articles/CommonIssues.html "AssetRipper Common Issues"

[10]: https://github.com/AssetRipper/AssetRipper/discussions/1483 "AssetRipper discussion #1483: CLI"
