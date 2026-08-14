# AssetRipper Custom — Final Workspace Build

هذه الحزمة مبنية فوق نسخة AssetRipper المفتوحة المصدر المعدّلة الحالية، وليست مشروعًا جديدًا من الصفر.

## التشغيل

شغّل `AssetRipper.GUI.Free.exe` على Windows. ستبقى نافذة Terminal ظاهرة افتراضيًا للتشخيص والإيقاف، ويمكن تشغيل `--hide-console` عند الحاجة إلى إخفائها.

## سير العمل المقترح

1. اختر **Open File** أو **Open Folder** واستورد ملفات Unity المسموح لك بتحليلها.
2. بعد انتهاء المعالجة استخدم **Asset Workspace** في الصفحة الرئيسية؛ يدعم البحث، فلاتر النوع والتصنيف والمجموعة، العرض الشبكي والقائمة، والتنقل السريع.
3. انقر على أي أصل لفتح **Inspector** الجانبي ومراجعة class وpath وcollection وbundle.
4. استخدم **Character sets** لمراجعة التجميع المحلول عبر الملفات: hierarchy، meshes، Avatar، controllers، animation clips، materials، textures، مع روابط مباشرة لكل مكوّن ورسائل dependencies غير المحلولة.
5. في Settings اترك **FBX** مختارًا للتصدير المجمّع، ثم استخدم **Export Primary Content**. ينتج المصدّر ملف FBX ASCII 7.4 لكل شخصية/جذر مع geometry وUVs وnormals وmaterials وtextures وskin clusters وanimation curves عندما تتوفر البيانات.
6. استخدم GLB من Settings عند الحاجة إلى التوافق مع سير العمل السابق أو المعاينة المباشرة.

## ملاحظات التوافق

تتضمن النسخة معالجة ترويسات Unity المشوّهة ضمن نافذة محدودة، fallback لإصدارات Unity غير الصالحة، دعم SerializedFile الحديثة، وتوسعة companions مثل cab وmanifest عند اختيار ملفات منفردة. لا تقوم النسخة بفك تشفير أو تجاوز DRM أو تصنيع dependencies مفقودة.

## سجل تحقق البناء

- `dotnet build ... AssetRipper.GUI.Free.csproj -c Release`: نجح، 0 تحذيرات، 0 أخطاء.
- `dotnet publish ... -r win-x64 --self-contained true`: نجح.
- `/js/asset_browser.js`: HTTP 200 في smoke test بعد إعادة البناء.
- `/js/collection_view.js`: HTTP 200 في smoke test بعد إعادة البناء.
- موارد JavaScript موجودة داخل `AssetRipper.GUI.Web.dll` المنشور.
- `AssetRipper.GUI.Free.exe`: Windows PE32+ console executable، مع أيقونة القط المخصصة.

## تحسينات Workspace الحالية

عند تحميل البيانات، تظهر **Asset Workspace** في الصفحة الرئيسية مباشرة. يعرض الجدول اسم الأصل وclass وcategory وcollection وملخص المكونات، ويحدد أول أصل تلقائيًا كي يظهر Inspector الجانبي دون نقرة إضافية. يعرض Character sets قوائم روابط للـmeshes والـAvatar والـcontrollers والـanimation clips والـmaterials والـtextures، إضافة إلى عدادات `Skinned` و`Weighted`.

إذا ظهر تحذير بأن mesh لا يملك recoverable vertex weights، فهذا يعني أن الملف يحتوي غالبًا على animation أو bones لكن weights ليست موجودة في Mesh القابل للقراءة. بعض الألعاب تخزن هذه البيانات داخل MonoBehaviour مخصص مثل GPU skinning؛ لا يمكن للمصدّر العام اختراع هذه الأوزان بأمان، ولذلك تظهر الحالة بصراحة بدل إنتاج FBX مضلل.

تمت إضافة `<link rel="icon">` و`shortcut icon` و`apple-touch-icon` إلى رأس كل صفحات الويب، وأصبح `StaticContent/favicon.ico` نسخة من أيقونة القط المستخدمة في التطبيق.

## تفاصيل FBX المحدثة

التصدير المجمّع يحافظ على hierarchy واحد للشخصية بدل إنشاء loose mesh nodes مكررة عندما تكون renderers موجودة داخل hierarchy. يدعم الآن جميع قنوات UV المتوفرة حتى UV7، texture offset/scale، local rotations، TRS curve tangents، skin clusters، bind matrices، sidecar PNG textures، والـanimation clips الموجودة في جذر الحزمة مع الملفات الشقيقة.

## Professional Master Workspace

The newest build places a master Workspace between the Character sets and the asset browser. It contains a hierarchy rail, a central Babylon preview, Lighting/Reset camera/Animation controls, Download GLB, Export FBX, and an actions panel with Information/tabs, Yaml, Json, and Mesh GLB links. Selecting a Mesh row updates the central preview. Selecting `Preview assembled` on a Character set loads the full resolved GameObject hierarchy through `/Assets/Character.glb`.

The hierarchy rail is intentionally link-based: each root, GameObject, and component opens the existing full asset page, where Information, Model, Yaml, Json, Dependencies, and Development remain available. The right actions panel keeps the raw-data links visible so the user does not need to leave the main workspace just to inspect a bind pose or serialized field.

For the user's eight-file sample, the live collection showed the expected chain of `hero20008` root, `m_20008`, `hero20008Avatar`, `hero20008Head`, two textures, SkinnedMeshRenderer, Animator, and Bip001 hierarchy. The new Workspace exposes that chain directly instead of only showing counts.

## CLI and MCP tools

تتضمن الحزمة مجلد `tools` بالإضافة إلى واجهة Windows الرئيسية. استخدم `tools\CLI\AssetRipper.CLI.exe` لمعالجة الملفات من الطرفية، و`tools\MCP\AssetRipper.MCP.exe` كخادم MCP عبر `stdio`. أمثلة CLI كاملة موثقة في `ASSET_RIPPER_CLI_MCP_GUIDE.md`.

مثال تصدير FBX مجمّع مع الأنيميشن:

```powershell
.\tools\CLI\AssetRipper.CLI.exe --input "C:\Game\Game_Data" --output "C:\Exports\Hero" --fbx --filter hero --include-anim
```

مثال batch للـraw JSON وFBX:

```powershell
.\tools\CLI\AssetRipper.CLI.exe --input "C:\Game\Game_Data" --output "C:\Exports\Batch" --batch-process --raw --fbx
```

خادم MCP لا يطبع أي تشخيص إلى stdout؛ stdout مخصص لرسائل JSON-RPC، بينما تذهب التشخيصات إلى stderr. أضف `tools\MCP\AssetRipper.MCP.exe` إلى إعدادات عميل MCP كما هو موضح في دليل CLI/MCP. يجب أن يطلب عميل MCP تأكيد المستخدم قبل استدعاء أدوات التصدير.


## Asset Workspace layout controls

تحتوي النسخة الجديدة على زر `Hide asset list` أعلى قائمة الأصول. عند إخفائها تتحول الصفحة إلى مساحة عمل مركزة على المعاينة، ويمكن إعادتها بواسطة `Show asset list` دون فقدان الأصل المحدد أو الفلاتر. كما توجد أزرار `Hierarchy` و`Asset actions` لإخفاء أو إظهار اللوحة اليسرى واليمنى داخل Workbench، وزر `Focus preview` لتوسيع المعاينة المركزية.

تُحفظ حالات هذه الأزرار محليًا في المتصفح عبر `localStorage`، لذلك تبقى تجربة التصفح كما اختارها المستخدم بعد إعادة تحميل الصفحة. ويمكن تغييرها في أي وقت دون إعادة معالجة الملفات.
