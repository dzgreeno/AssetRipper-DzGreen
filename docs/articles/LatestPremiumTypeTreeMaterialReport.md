# تقرير التنفيذ الأخير: Phase 2 لمسارات Mesh وAnimation وMaterial في AssetRipper DzGreen Premium

**النسخة:** Premium Preview v1.3.15-dzgreen.15 — قيد بناء الحزمة المحلية  
**الأساس المحلي:** `d12ac69fa276c1fe32260dec72161fc1f26092e9` مع تغييرات Phase 2 غير المرفوعة  
**المالك:** dzgreeno  
**حالة النشر:** محلي فقط؛ لم يتم دفع مصدر أو حزمة إلى GitHub ولم يُنشأ Release.

## الملخص التنفيذي

يتناول هذا التقرير آخر تنفيذين متصلين في نسخة **AssetRipper DzGreen Premium**. ركز العمل على جعل نتائج الاستيراد القابل للقراءة أكثر قابلية للفحص قبل التصدير: قياس درجة توفر TypeTree لكل مجموعة أصول، تحليل شبكة مراجع PPtr بين الأصول، وجرد روابط `Material` إلى `Texture2D` مع تحويلات Scale وOffset. تظهر هذه النتائج داخل تقرير JSON في المسار `/Assets/PremiumDiagnostics` بعد تحميل إدخال Unity مصرح به.

> لا ينشئ التنفيذ حقولًا مفقودة، ولا يعيد بناء بيانات غير مقروءة بالتخمين، ولا يفك تشفير ملفات أو يتجاوز DRM أو anti-tamper أو تفريغات ذاكرة. كل النتائج مشتقة من البيانات التي حمّلها المستورد العادي بالفعل.

| المجال | ما أضيف | الفائدة العملية |
| --- | --- | --- |
| TypeTree | تصنيف موثق لتوفر مخطط الأنواع لكل Serialized Asset Collection | يحدد الملفات التي تصلح لمتابعة فحص Prefab وMesh وAnimation، والملفات التي يجب الإبلاغ عن نقص معلوماتها بدل تصدير بيانات غير موثوقة. |
| PPtr | شبكة مراجع محددة الحجم مع مكونات مترابطة بقوة | تكشف المراجع المفقودة والدورات بين الأصول والملفات، ما يساعد على تفسير Prefab أو Material لا يظهر بصورة مكتملة. |
| Material وTexture | جرد read-only لخصائص Texture المسلسلة | يعرض اسم خاصية المادة والـTexture المرتبط بها وحالة الحل وScale وOffset، لتشخيص المواد البيضاء أو روابط Texture الناقصة. |
| الحسابات الهندسية | أدوات Half وSNORM وSmallest-Three Quaternion مع معالجات Stream مقيدة بالمخطط | تتحول القنوات المدعومة إلى قيم GLB/Animation دقيقة، بينما يسجل المخطط غير المدعوم ولا يفسر بالتخمين. |

## 1. تغطية TypeTree

تمت إضافة `PremiumTypeTreeCoverageAnalyzer` في `Source/AssetRipper.Premium/PremiumTypeTreeCoverageAnalyzer.cs`. يعمل المحلل على `SerializedAssetCollection` التي حمّلها البرنامج، ويقرأ فقط بيانات metadata المتوفرة بالفعل: عدد الأصول، وجود TypeTree مضمّن، عدد الأنواع المسلسلة، الأنواع المجردة، والأنواع المرجعية.

| حالة التقرير | شرط التصنيف | الدلالة |
| --- | --- | --- |
| `Embedded` | يوجد TypeTree مضمّن ولا توجد أنواع مسجلة كمجردة | أعلى مستوى من أدلة المخطط المتوفر داخل الملف. |
| `Partial` | يوجد TypeTree مضمّن مع نوع واحد أو أكثر مجرد | المخطط متوفر جزئيًا؛ يعرض التقرير النقص بوضوح. |
| `KnownEngineSchema` | لا يوجد TypeTree مضمّن، لكن المجموعة تحتوي أصولًا | يستخدم المستورد مخطط المحرك المتاح؛ لا يدعي التقرير وجود TypeTree مضمّن. |
| `Unavailable` | لا توجد أدلة TypeTree ولا أصول في المجموعة | لا توجد تغطية يمكن إثباتها من metadata المتوفرة. |

تم تسجيل هذه metadata في `SerializedAssetCollection` أثناء الاستيراد، ثم تُجمع في `PremiumImportDiagnosticReport.TypeTreeCoverage`. يبقى الهدف تشخيص الثقة والتغطية فقط؛ لا توجد محاولة لتخمين أسماء حقول أو أطوال بيانات مفقودة.

## 2. شبكة مراجع PPtr والمكونات الدورية

تم تحديث `Source/AssetRipper.Premium/PremiumReferenceGraph.cs` ليبني رسمًا بيانيًا محدود الحجم من المراجع التي تم الوصول إليها في الأصول المحملة. يتضمن التقرير عدد العقد، الحواف، المراجع المحلولة، المراجع الفارغة، المجموعات المفقودة، الأصول المفقودة، وحالة القطع عند الوصول إلى حد الحماية.

بدل الاكتفاء بعدّ حافة راجعة واحدة، يستخدم الإصدار الأخير مرورين غير تكراريين لبناء **المكونات المترابطة بقوة**. يعطي ذلك عدادين أكثر فائدة:

| الحقل | المعنى |
| --- | --- |
| `CycleComponentCount` | عدد المجموعات الدورية المستقلة داخل الرسم البياني. |
| `CyclicNodeCount` | عدد الأصول الواقعة ضمن دورات، بما يشمل Self-reference إن وجدت. |

هذا التحليل لا يغير PPtr أو يستبدل أهدافها. إذا كانت مادة أو Mesh أو Component تشير إلى أصل مفقود، يسجل التقرير الحالة لتوجيه اختبار العينة أو قرار التصدير اللاحق.

## 3. سجل Material وTexture

تمت إضافة `PremiumMaterialBindingAnalyzer` في `Source/AssetRipper.Premium/PremiumMaterialBindingAnalyzer.cs`. يستعمل واجهة `IMaterial.GetTextureProperties()` الموجودة أصلًا، ثم يحفظ لكل رابط ما يلي:

| الحقل | المصدر | الاستخدام |
| --- | --- | --- |
| `PropertyName` | اسم خاصية Texture المسلسلة، مثل `_MainTex` | يحدد قناة المادة التي تحتاج Texture. |
| `TexturePathID` و`TextureName` | هدف PPtr إذا كان متاحًا | يربط المادة بالأصل المقروء القابل للتصدير. |
| `ScaleX`, `ScaleY`, `OffsetX`, `OffsetY` | `UnityTexEnv` المسلسل | يحافظ على تحويل Texture المستخدم في المادة. |
| `Status` | نتيجة حل المرجع | `Resolved` لــ `Texture2D` متاح، و`Unresolved` لهدف غير Texture2D، و`Null` لمرجع فارغ أو غير قابل للحل. |

يضاف إجمالي المواد وروابط Texture وحالاتها إلى `PremiumImportDiagnosticReport.MaterialBindings`. السجل لا يقرأ Shader bytecode، ولا يحاول تحويل شيدر خاص إلى كود مصدر. إنه يعرض فقط metadata والروابط التي كانت متاحة للمستورد والتصدير المعتاد.

## 4. التكامل مع تقرير Premium

امتد `PremiumImportDiagnostics` ليجمع أربعة تقارير مترابطة بعد التحميل: `ReferenceGraph` و`TypeTreeCoverage` و`MaterialBindings` و`VertexStreams`. يعرض تقرير القنوات عدد Mesh التي تحققت مواقعها أو Normals أو Tangents، وملخصًا مجمعًا لكل سبب رفض. لذلك يتيح المسار `/Assets/PremiumDiagnostics` رؤية متجانسة لحالة الاعتمادات والمخططات والمواد وقنوات Mesh قبل اتخاذ قرار بالتصدير. إذا لم تُحمّل أصول بعد، يعيد المسار حالة انتظار بدلاً من نتيجة مصطنعة.

## 5. تطبيق Phase 2 على مسارات التصدير

تعمل هذه المرحلة على الأصول التي استوردها البرنامج بوصفها نصًا صريحًا قابلًا للقراءة فقط. لا تفك تشفير الحاويات، ولا تستنتج byte stride مفقودًا، ولا تحول معلومات غائبة إلى مخرجات ظاهرها صحيح.

| المسار | التنفيذ | سلوك البيانات غير الموثقة |
| --- | --- | --- |
| Mesh Vertex Streams | `PremiumVertexStreamProcessor` يعالج `ReadOnlySpan<byte>` لقنوات Position وNormal وTangent ذات layouts الموثقة فقط. يستخدم `HalfToFloat` وSNORM 8/16-bit من وحدات التحويل المختبرة. | يرفض format أو stride أو range غير معروف ويضيف كود تشخيص مجمعًا إلى `VertexStreams` بدلاً من التخمين. |
| Animation Curves | `PremiumAnimationStreamProcessor` يبني مفاتيح translation وrotation المعروفة، ويفك `SmallestThreeQuaternion`، ويقدم sampler محدودًا يحافظ على المفاتيح أو يعيد أخذ العينات بخطوة framerate صريحة. | يرفض مفاتيح ناقصة أو غير مرتبة أو أجزاء quaternion مخالفة للمدى الموثق. |
| GLB Materials | `GlbLevelBuilder` يربط `_MainTex` و`_BaseMap` إلى BaseColor، و`_BumpMap` و`_NormalMap` إلى Normal، وخرائط Metallic إلى MetallicRoughness. كما يطبق offset وscale ودوران texture وWrapU/WrapV المقروءين. | ينشئ fallback أبيض أو normal محايد 1×1 فقط للرابط `Null` أو `Unresolved`، وليس لملف Texture موجود لكنه فشل في القراءة. |

يبقى تصدير GLB الحالي هو المستهلك الفعلي لمسارات المواد والـAnimation المعتادة. أما `PremiumVertexStreamProcessor` فيبقى طبقة تحقق عالية الدقة وتشخيصية للمخططات الصريحة، تمنع المسار Premium من تمرير layout مريب باعتباره هندسة مؤكدة.

## 6. الاختبارات والتحقق

تمت إضافة اختبارات إلى `Source/AssetRipper.Premium.Tests/PremiumInputPolicyTests.cs` لتغطية حالات الحدود التالية:

| الاختبار | ما تم التحقق منه |
| --- | --- |
| Half / SNORM / Quaternion | تحويلات عددية حتمية تشمل الصفر وsubnormal وInfinity وNaN والإشارات الموجبة والسالبة وهوية Quaternion. |
| شبكة المراجع | دورة من أصلين، مرجع أصل مفقود، ومرجع فارغ، مع تحقق من أعداد SCC. |
| TypeTree | الحالات Embedded وPartial وKnownEngineSchema وUnavailable دون تخمين المخطط. |
| Material bindings | عدادات الروابط المحلولة وغير المحلولة والفارغة من عينات اختبار صغيرة، مع دمج Texture transforms وneutral fallbacks في GLB. |
| Vertex streams | فك SNORM 8-bit، قبول layout موثق، ورفض stride أو format غير موثق مع أكواد تشخيص. |
| Animation streams | فك Smallest-Three، ترتيب المفاتيح، وعينة Slerp ضمن المدة الموثقة. |
| سياسة Premium وملف الاسترداد | التصريح الصريح، رفض المدخلات خارج السياسة، وخيارات التصدير الآمنة. |

تم تشغيل **9 مشاريع اختبار**؛ النتيجة النهائية **527 اختبارًا ناجحًا و0 فشل**. يتضمن ذلك **17 اختبارًا** في مشروع Premium. كما نجح بناء `AssetRipper.GUI.Premium` و`AssetRipper.Export.Modules.Models` دون أخطاء أو تحذيرات.

## 7. حزمة Windows التجريبية

تم بناء حزمة Windows x64 ذاتية الاحتواء واختبار سلامة الأرشيف محليًا.

| العنصر | القيمة |
| --- | --- |
| اسم الحزمة | حزمة v15 قيد البناء والتحقق محليًا؛ الصف التالي هو baseline v14 فقط. |
| حزمة baseline السابقة | `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.14-preview-Windows-x64.zip` |
| الحجم | 91,622,524 بايت |
| SHA-256 | `c6e108c4d91ee61b8cdda10cea15c73600720b90580e2c18d896faa8216c412f` |
| فحص الأرشيف | نجح `unzip -t` دون أخطاء |
| ملفات مرفقة داخل الحزمة | ملف تشغيل Premium، تعليمات Windows، ووثيقة معمارية الاسترداد المصرح به |

تشغل الحزمة بالتعليمة التالية، بشرط أن يكون المستخدم مخولًا لمعالجة الإدخال غير المشفر:

```text
AssetRipper.GUI.Premium.exe --premium-authorized
```

## 8. الحدود الحالية والخطوة العملية التالية

التحسينات الحالية هي طبقة **قياس وتشخيص** فوق مسار الاستيراد والتصدير القائم. لا تعني أن كل Mesh أو Animation أو Shader سيكون صالحًا في كل لعبة؛ بل تجعل سبب عدم اكتمال الأصل قابلاً للقياس: نقص TypeTree، مرجع مفقود، دورة مراجع، أو Texture غير محلول.

الخطوة التالية ذات الأولوية هي اختبار هذه الحقول على عينات Unity مصرح بها من إصدارات مختلفة، تشمل شخصية SkinnedMeshRenderer، مواد متعددة وTexture transforms، AnimationClip بعظام، وPrefab موزع على أكثر من AssetBundle. عند توفير هذه العينات، يمكن مقارنة تقرير Premium بما يظهر في Unity/Blender وتوسيع إصلاحات التصدير بناءً على أدلة حقيقية.
