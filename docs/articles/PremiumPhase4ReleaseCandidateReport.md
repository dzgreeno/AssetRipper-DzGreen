# تقرير Release Candidate المحلي — Phase 4

**المنتج:** AssetRipper DzGreen Premium  
**المالك:** dzgreeno  
**الحالة:** Release Candidate محلي للتحقق؛ لا رفع ولا إصدار GitHub ضمن هذا التقرير.

## نطاق النسخة

تركز Phase 4 على تحسين قابلية رؤية نتائج الاستيراد وإخراج textures التي يستطيع المفكك الموجود إثباتها. لا تفك هذه النسخة أي تشفير، ولا تحلل shader bytecode خاصًا، ولا تستبدل مواد Unity في المصدر. كل قرار يخرج من حالة importer ومن تشخيص Premium القابل للتكرار.

| المكون | التنفيذ الحالي | السلوك الآمن |
| --- | --- | --- |
| Texture transcoding | `PremiumTextureTranscoder` وCLI `--textures` | يعتمد فقط على `TextureConverter` القائم؛ تصدير PNG أو TGA أو EXR لا يتم إلا بعد integrity check ونجاح decoder. |
| ضغط textures | ASTC، ETC/EAC/ETC2، PVRTC، Crunch، BC وDXT تدعم حيث يقبلها `TextureConverter` القائم. | stream غير المقبول يسجل `Unsupported` ولا يحول إلى صورة تخمينية. |
| Color space | يعرض التقرير `Unknown` عندما لا يكشف نموذج asset metadata موثوقًا عن sRGB/Linear. | لا يجري تحويل gamma أو linearization بلا دليل schema. |
| Mipmaps | لا يعلن transcoder حفظ أو توليد mip chain مستقل في هذه النسخة. | لا ينشئ mipmap مصطنعًا؛ تسجل الحالة `NotExposed`. |
| Shader injection | `PremiumShaderPropertyInjector` ينتج خطة URP Lit قابلة للمراجعة. | يطابق فقط `_MainTex`/`_BaseMap` و`_BumpMap`/`_NormalMap` وmetallic/occlusion/emission المعروفة. |
| Fallback textures | الخطة تفرق بين `ResolvedSource` و`NeutralFallbackRequired` و`UserFallbackAvailable`. | Null يستخدم neutral fallback، أما Unresolved فلا يستخدم صورة المستخدم إلا حين يطابق فهرسًا صريحًا؛ لا تعديل تلقائي لملف Unity. |
| Diagnostic dashboard | المسار `/PremiumDiagnostics` في واجهة Premium. | لوحة قراءة فقط تعرض TypeTree partial/unavailable، material bindings، reference cycles، وخطة verified-only مع بحث محلي. |

## الاستخدام

```text
AssetRipper.CLI --input game_Data --output export --textures --texture-format png \
  --export-diagnostics html

AssetRipper.CLI --input game_Data --output export --batch --raw \
  --export-verified-only --fallback-textures replacement_textures \
  --export-diagnostics json
```

يكتب `--textures` manifest باسم `textures/assetripper-texture-transcode-manifest.json`. يحصي manifest النجاح والفشل ولا يدعي أن ملفًا لم يفكك قد تم تحويله. يمكن فتح لوحة التشخيص بعد تشغيل نسخة Premium وتحميل بيانات Unity نصية ومصرح بها من الرابط `/PremiumDiagnostics`.

## أدلة التحقق

| التحقق | النتيجة |
| --- | --- |
| اختبارات Premium | 22 ناجحة، 0 فشل؛ وتشمل Phase 4 mapping القياسي للحالات resolved/null/not-mapped. |
| الانحدار الكامل | 9 مشاريع، **532 اختبارًا ناجحًا، 0 فشل**. |
| واجهة Premium | بنيت بنجاح دون أخطاء أو تحذيرات بعد إضافة لوحة diagnostics. |
| CLI | بني بنجاح؛ ظهرت خيارات `--textures` و`--texture-format`. اختبر مسار TGA على مجلد خالٍ وأنشأ manifest صفر أصول دون صنع ملف image. |
| لوحة diagnostics | تحققت محليًا في headless mode؛ يعرض المسار `/PremiumDiagnostics` empty state عند عدم تحميل بيانات. |

## الحدود قبل أي إصدار عام

لا يثبت هذا التقرير أن كل صيغة Unity ممكنة صالحة؛ يثبت فقط أن الديكودرات القائمة ترفض أو تقبل بوضوح، وأن مسار export يحترم تلك النتيجة. لم تتوافر في هذه الجلسة عينة Unity مصرح بها تجمع ASTC وETC2 وPVRTC وCrunch مع Texture2D صالح لكل صيغة، لذلك يلزم اختبار قبول منفصل لكل صيغة فعلية قبل إعلان توافق إنتاجي لها.

لا يتم بعد ربط user fallback texture catalog بمصدّر GLB لتطبيق replacement فعليًا. يبقى ذلك عملًا لاحقًا مستقلًا، ويجب أن يقتصر على material bindings التي تكون `Unresolved` صراحة، مع إبقاء neutral fallback للـNull. كما لا يوجد توليد mipmap في هذه النسخة، لأن توليده بلا تعريف importer/quality settings قابل للقراءة سيغير المحتوى بدلاً من حفظه.

تستمر القيود الأمنية: تعمل النسخة على Unity inputs النصية غير المشفرة والمصرح بها فقط. لا تدعم مفاتيح runtime أو memory dumps أو تخطي الحماية أو فك proprietary shader bytecode.
