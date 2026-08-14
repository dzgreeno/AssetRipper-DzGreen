# AssetRipper Feature Update

هذه النسخة مبنية على شجرة AssetRipper المعدّلة السابقة، وتحافظ على إصلاحات ترويسات Unity وRawWeb واستعادة إصدارات Unity ونسخة Windows بلا Terminal. الإضافات الحالية موجهة إلى الاستخدام المشروع مع ملفات يملك المستخدم حق تحليلها؛ لا تتضمن فك تشفير أو تجاوز DRM أو anti-tamper.

## ما تم تنفيذه

| المجال | النتيجة |
|---|---|
| الواجهة | طبقة Dark Workspace بألوان slate/zinc، cyan، وpurple، مع بطاقات، حدود، حالات focus، وتخطيط متجاوب. التطبيق المنشور Windows هو `Exe` حتى تبقى نافذة Terminal ظاهرة للتشخيص والإيقاف؛ استخدم `--hide-console` فقط عند الحاجة إلى إخفائها. |
| صفحة الأصل | تخطيط ثلاثي المناطق: sidebar للفلاتر والتنقل، viewport مركزي، وinspector يعرض class/path/collection/bundle. التبويبات الأصلية لم تُحذف. |
| Asset Workspace | بعد انتهاء المعالجة تظهر الشاشة الرئيسية مباشرة كمتصفح واسع لكل الأصول، مع بحث فوري، فلاتر category/class/collection، chips سريعة، list/grid، عدّ النتائج، وروابط مباشرة للأصول والمجموعات. |
| Character sets | يجمع root hierarchy مع skinned meshes، Avatar، AnimatorController/RuntimeController، AnimationClips القابلة للحل، materials، textures، ويعرض counts وروابط مباشرة لكل مكوّن وروابط missing. صيغة FBX هي الافتراضية الآن؛ عند Export Primary Content يُنتج ملف شخصية مجمعًا لكل root. |
| المعاينة ثلاثية الأبعاد | أزرار Lighting، Reset camera، وAnimation، مع معالجة آمنة للصفحات التي لا تحتوي على Model tab. |
| GLB | تنزيل GLB مباشر من Model tab. التصدير الأساسي السابق إلى GLB والـpreview الحاليان محفوظان. |
| FBX ASCII | مصدّر مستقل بلا Autodesk native SDK؛ يكتب FBX 7.4 ASCII مع geometry (vertices/indices/normals/tangents/UV0/colors/topologies)، مواد Phong، texture sidecars بصيغة PNG، hierarchy، Skin/Cluster وbind matrices من بيانات Mesh، وTRS curves عند توفر AnimationClip والمسارات القابلة للحل. |
| اختيار الصيغة | `ExportSettings.ModelExportFormat` يجعل FBX الافتراضي لتجربة التجميع المطلوبة، مع إبقاء GLB متاحًا من صفحة Settings للتوافق مع المعاينة والتصدير السابق. |
| Status dock | عرض آخر رسائل Import/Export/Auto-Fix في أسفل كل صفحة عبر `/Status/Recent`، مع ألوان للحالات التحذيرية والأخطاء. |
| معالجة الترويسات | واجهة `FilePreProcessor` ورفع الفحص المحدود إلى 256 بايت لاستعادة `UnityFS` و`UnityWeb` و`UnityRaw` و`RawWeb` و`UnityArchive` عندما تكون ظاهرة نصيًا بعد بادئة غير مشفرة. |
| القراءة الخام | إذا تعذر اكتشاف platform أو mixed structure، يحاول `GameStructure` قراءة الملفات المدخلة خامًا بدل إنشاء مجموعة أصول فارغة. |
| Companion expansion | عند اختيار ملفات Unity منفردة، يبحث التطبيق في المجلد نفسه عن عائلة الملفات ذات الصلة وmanifest/asset-bundle/split/cab companions، مع إبقاء توسعة المجلد الكامل متاحة. لا يتم فك تشفير أو تصنيع dependency. |
| Dependency diagnostics | يعرض السجل الاسم الأصلي والاسم المطبع ومسارات البحث، ويوضح متى يجب اختيار مجلد الحزمة كاملًا أو توفير companion غير معدل. |
| ShaderLab | إضافة `ShaderExportHandler` وتوسيع fallback ليضيف `_MainTex` و`_Color` و`_BumpMap` و`_SpecGlossMap` و`_OcclusionMap` عند غيابها، مع CGPROGRAM محمول. |
| الصوت والفيديو | المصَدّرات الحالية موجودة أصلًا: audio decode/download وvideo raw dump مع integrity checks. لم تتم إضافة إعادة ترميز MP3 أو MP4 لأن ذلك يحتاج codecs وسياسة جودة منفصلة. |

## ما لم يُدّعَ أنه مكتمل

المشروع الحالي يملك GLB وFBX ASCII للسيناريوهات التي تدعمها بيانات AssetRipper، لكنه لا يضمن أن كل Mesh أو Skeleton أو AnimationClip أو texture mapping في كل إصدار Unity سيظهر كاملًا. FBX لا يدّعي تحويل Humanoid Avatar إلى تعريف Human كامل، ولا يصدّر blend-shape animation أو custom material curves غير القابلة للحل؛ هذه الحالات تُحفظ أو تُتجاهل بأمان بدل اختلاق بيانات. كما أن توافق Blender/Unity يعتمد على صحة البيانات المصدرية وعلى دعم مستورد FBX للإصدار المكتوب، ولذلك يجب فحص الملفات الناتجة في التطبيق المستهدف.

دعم Unity 6000.3/6000.5 وURP و`SerializeReference` وWebGL يبقى تدريجيًا. تقارير SerializedFile version 23، padding الخاص بالمنصة، وURP layout موثقة في مشكلات AssetRipper الرسمية [1] [2] [3]؛ لذلك لا يصح تحويلها إلى تخطٍّ عام للبايتات أو محاولة تجاوز حماية ملف مشفّر.

## التحقق

تم تنفيذ الاختبارات التالية في بيئة البناء:

| الفحص | النتيجة |
|---|---|
| `dotnet build Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -p:PublishAot=false --no-restore` | Build succeeded، 0 warnings، 0 errors في البناء النهائي. |
| `node --check StaticContent/js/site.js` | ناجح. |
| `node --check StaticContent/js/mesh_preview.js` | ناجح. |
| `node --check StaticContent/js/commands_page.js` | ناجح. |
| `git diff --check` | ناجح. |
| ValidationHarness | ناجح؛ استعادة UnityFS بعد بادئة 200 بايت، فحص حد 256 بايت، وRawWeb. |
| FBX harness | ناجح؛ توليد FBX 7.4 minimal، والتحقق من header والأقسام وتوازن الأقواس ومرجعيات Connections IDs. |
| UI build | `AssetRipper.GUI.Web` نجح بعد Asset Workspace وCharacter sets، 0 warnings، 0 errors. |
| WinExe verification | executable المنشور تحقق كـ`PE32+ executable (GUI) x86-64`، وليس Console subsystem. |

## التشغيل وإعادة البناء

على Windows PowerShell مع .NET SDK 10:

عند اختيار `Fbx` من Settings، يكون الملف الرئيسي بامتداد `.fbx` وتُكتب الصور المستخرجة في مجلد `Textures` بجانب الملف مع مراجع نسبية داخل FBX. لا تحذف هذا المجلد إذا كانت الخامات تعتمد على الصور المرافقة.

```powershell
dotnet restore Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj
dotnet build Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj `
  --configuration Release `
  -p:PublishAot=false

dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishAot=false `
  --output publish\win-x64
```

شغّل `AssetRipper.GUI.Free.exe` من مجلد النشر كاملًا. لا تنقل الملف التنفيذي منفردًا؛ يحتاج إلى DLLs وملفات JSON والموارد المرافقة. التشغيل العادي يفتح Terminal لمراقبة Import/Processing/Export وإيقاف العملية عند الحاجة. استخدم `--hide-console` لإخفائها في التشغيل الصامت، بينما يبقى `--keep-console` مقبولًا للتوافق مع أوامر الإصدارات السابقة. أُضيفت أيقونة Windows مخصصة على شكل قط كرتوني ثلاثي الأبعاد إلى executable. عند اختيار ملفات منفردة من مجلد شخصية، يوسّع التطبيق تلقائيًا الملفات الشقيقة المعروفة، بينما يظل اختيار المجلد الكامل هو الخيار الأكثر موثوقية عند وجود cab/resource companions.

## References

[1]: https://github.com/AssetRipper/AssetRipper/issues/2304 "SerializedFile version 23 and Unity 6000.5"

[2]: https://github.com/AssetRipper/AssetRipper/issues/2237 "Unity 6000.3 WebGL padding"

[3]: https://github.com/AssetRipper/AssetRipper/issues/2207 "Unity 6000.3 URP and SerializeReference"
