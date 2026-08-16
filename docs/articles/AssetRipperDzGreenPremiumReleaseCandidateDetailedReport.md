# التقرير التفصيلي لتسليم Release Candidate

**المنتج:** AssetRipper DzGreen Premium  
**المالك والمؤسس:** dzgreeno  
**الفرع المحلي:** `dzgreen-vnext-hardening`  
**الحالة:** حزمة Release Candidate محلية متحققة. لم يُنفذ أي push إلى GitHub ولم يُنشأ أي GitHub Release ضمن هذا التسليم.

## 1. نطاق التقرير ومنهجيته

يوثق هذا التقرير ما هو موجود ومتحقق منه فعليًا في شجرة المصدر المحلية وفي سجلات البناء والاختبار وحزم التسليم. بنيت النسخة فوق الحل الموجود `AssetRipper.slnx` وفوق تعديلات AssetRipper DzGreen السابقة، وليس فوق مشروع بديل أو هيكل مصطنع. كل ميزة موصوفة هنا مرتبطة بملف مصدر محدد أو نتيجة اختبار أو ناتج نشر محفوظ محليًا.

> تعمل Premium فقط مع بيانات Unity النصية غير المشفرة التي يملك المستخدم حق معالجتها. لا تتضمن الحزمة فك تشفير، استخراج مفاتيح runtime، التعامل مع memory dumps، تجاوز DRM، أو محاولة تجاوز أي حماية تقنية.

| فئة الدليل | المصدر المحلي | الغرض |
| --- | --- | --- |
| الحل والمصدر | `AssetRipper.slnx` و`Source/` | المراجعة والبناء وإعادة الإنتاج. |
| سجل البناء | `/tmp/build-release-gui-cli-roslyn-compat.log` | إثبات بناء GUI وCLI في وضع Release. |
| سجل الاختبارات | `/tmp/test-release-candidate-no-build.log` | إثبات تنفيذ المشاريع التسعة ونتائجها. |
| سجل النشر | `/tmp/publish-release-candidate-win-x64.log` | إثبات نشر Windows x64 self-contained. |
| وثائق المراحل | `docs/articles/PremiumPhase3LogicalReconstructionReport.md` و`PremiumPhase4ReleaseCandidateReport.md` | حدود وتنفيذ Phase 3 وPhase 4. |
| دليل البناء | `docs/articles/PremiumReleaseCandidateBuildEvidence.md` | الأوامر والنتائج وقائمة الأدلة الخام. |

## 2. خط الإيداعات المحلي

تظهر الإيداعات التالية تاريخ التنفيذ المحلي لتراكم Phase 2 إلى Release Candidate. جميع الإيداعات موقعة محليًا باسم `dzgreeno <dzgreeno@users.noreply.github.com>`.

| الإيداع | الوصف |
| --- | --- |
| `40484fc8691001715a2cd7fb046cc770a7242e5a` | تطبيق معالجات Phase 2 لمسارات Vertex وAnimation وMaterial/GLB. |
| `15e77e4d3f9bfa5219ac7a81ab57fb44d4dec591` | تسجيل تحقق حزمة Phase 2 محليًا. |
| `dee5a9ba0d99d1d22c2d2689973c5f2a301d1c70` | إضافة إعادة البناء المنطقي لـPhase 3: hierarchy وPrefab وMecanim ووسائط قابلة للقراءة وتنسيق CLI. |
| `ff434377d2a58ebb9dda183171e3f38e54feb7ac` | توضيح أن فهرس fallback texture ليس استبدالًا مطبقًا تلقائيًا. |
| `72d8bd60cb284b45bf21701959096c2b8b12131e` | إضافة تشخيصات texture وshader وخطوط CLI ولوحة diagnostics في Phase 4. |
| `4642275e7e236a42425f8f9e91fdb222fc7fb2d2` | إضافة دليل بناء Release Candidate المحلي. |
| `7beddb4cddd382bd5e7e754adf9afb58a2de35c3` | تسجيل تحقق الحزم والبصمات في TODO. |

## 3. بنية المصدر التي تم تسليمها

يحتوي أرشيف المصدر على 1,744 مدخلًا، منها الحل ومشاريع C# الفعلية والاختبارات والوثائق. الملفات المركزية أدناه موجودة في أرشيف المصدر وفي شجرة المشروع الأصلية.

| المجال | ملفات رئيسية | الوظيفة العملية |
| --- | --- | --- |
| سياسة الإدخال والتشخيص | `PremiumInputPolicy.cs`، `PremiumImportDiagnostics.cs`، `PremiumRecoveryProfile.cs` | رفض سياقات الإدخال غير المصرح بها وتجميع تقرير الحزم والمخططات والمراجع. |
| المخططات والمراجع | `PremiumTypeTreeCoverageAnalyzer.cs`، `PremiumReferenceGraph.cs` | تصنيف التغطية إلى Embedded/KnownEngineSchema/Partial/Unavailable وكشف cycles في PPtr graph. |
| Mesh وAnimation | `PremiumGeometryUnpackers.cs`، `PremiumVertexStreamProcessor.cs`، `PremiumAnimationStreamProcessor.cs` | Half وSNORM وSmallest-Three quaternion وقبول layouts المعلنة فقط. |
| Material وGLB planning | `PremiumMaterialBindingAnalyzer.cs`، `PremiumShaderPropertyInjector.cs`، `PremiumExportOrchestrator.cs` | تصنيف bindings وحساب خطة URP/HDRP reviewable وسياسة verified-only. |
| Hierarchy وPrefab | `PremiumHierarchyReconstructor.cs` | بناء parent/child graph وTRS world matrices وكشف cycle دون تعديل asset. |
| Mecanim | `PremiumMecanimStateMachineAnalyzer.cs`، `PremiumBlendTreeEvaluator.cs` | جرد states/transitions والشروط والحساب المحدود لـ1D و2D blend trees المعلنة. |
| Texture وMedia | `PremiumTextureTranscoder.cs`، `PremiumAudioMediaProcessor.cs` | تصدير textures التي يقبلها المفكك القائم وجرد مسارات Audio/Video القابلة للقراءة فقط. |
| Premium GUI | `Source/AssetRipper.GUI.Premium/Program.cs`، `Source/AssetRipper.GUI.Web/Pages/PremiumDiagnosticsPage.cs` | نقطة تشغيل Premium ولوحة `/PremiumDiagnostics` للقراءة فقط. |
| CLI | `Source/AssetRipper.Tools.CLI/Program.cs`، `Source/AssetRipper.Tools.Common/AssetRipperToolService.cs` | إدخال batch وverified-only وdiagnostics وfallback catalog وtexture export. |
| الاختبارات | `Source/AssetRipper.Premium.Tests/PremiumInputPolicyTests.cs` | اختبارات سياسة الإدخال والتحويلات والهندسة والحركة والمواد وhierarchy وMecanim وTexture/shader plan. |

## 4. خصائص Premium المتحققة

### 4.1 قبول الإدخال والتشخيص الأساسي

تتطلب نسخة GUI Premium الوسيط `--premium-authorized`. تضبط نقطة التشغيل edition Premium وتطبع رسالة صريحة بأن البيانات المطلوبة يجب أن تكون Unity plaintext مصرحًا بها. لا يعد الوسيط وسيلة تحايل؛ بل هو attestation تشغيلي يذكر المستخدم بمسؤولية التفويض. تظل سياسة المكتبة مسؤولة عن رفض أنواع الإدخال التي تقع خارج النطاق المعلن.

يجمع `PremiumImportDiagnostics` تقريرًا حتميًا يحتوي على TypeTree coverage وPPtr graph وMaterial bindings وVertex stream diagnostics وHierarchy وPrefab override/Mecanim وMedia وTexture transcoding وخطة standard shader. لا يتسبب إنشاء التقرير في تعديل أصل Unity أو PPtr أو property.

### 4.2 Vertex وMesh

يستخدم `PremiumVertexStreamProcessor` وحدات التحويل المختبرة فقط. يقبل layout موثقًا صراحة ويقرأ البيانات بـ`ReadOnlySpan<byte>`؛ أما stride أو channel أو format غير المعلن فيسجل سبب الرفض في diagnostics. لا توجد محاولة لاستنتاج stride من حجم buffer أو اسم asset.

| تحويل مدعوم | الاستخدام |
| --- | --- |
| Half إلى Float | قنوات position/normal/tangent التي يثبت layout أنها Float16. |
| SNORM 8/16 | تحويل normal/tangent المعلن في schema. |
| SNORM 10-bit | مفكك رياضي مختبر ضمن `PremiumGeometryUnpackers`. |
| Float | قراءة مباشرة من layout المعلن. |

### 4.3 Animation وMecanim

يفك `PremiumAnimationStreamProcessor` quaternion بصيغة Smallest-Three عندما تكون stream descriptor والتوقيت متاحين. يشمل المسار sampler مبنيًا على Slerp للمفاتيح المقروءة. لا يولد keyframes من motion غير متاح ولا يستنتج tangents مفقودة.

يفهرس `PremiumMecanimStateMachineAnalyzer` بيانات AnimatorController المكشوفة في schema: state machines وstates وtransitions وconditions وblend tree states. تسجل parameter bindings غير المحلولة بدل تحويلها إلى شروط افتراضية. يحسب `PremiumBlendTreeEvaluator` وزن 1D من thresholds المعلنة و2D inverse-distance من مواقع معلنة، ويرفض NaN وInfinity والأنماط غير المحددة.

### 4.4 Transform وPrefab

يعيد `PremiumHierarchyReconstructor` بناء الرسم parent/child من Transform وGameObject references المتاحة. يتحقق من اتفاق الرابطين، ويمنع حساب world matrix خلال cycle أو ancestor دوري أو parent غير متاح. تحسب مصفوفة العالم فقط من TRS التي توفرها البيانات المقروءة.

Prefab resolver غير متلف: يجرد تعريفات Prefab ومثيلاتها ومعلومات modification المكشوفة، ويضع unknown property أو script غير قابل للحل ضمن قائمة unresolved. لا يصنع MonoBehaviour أو script مفقودًا، ولا يغير base prefab.

### 4.5 Materials وTextures

يفهرس `PremiumMaterialBindingAnalyzer` properties النصية القابلة للقراءة، ويصنف texture PPtr إلى `Resolved` أو`Unresolved` أو`Null` مع scale وoffset. تحول طبقة GLB الحالية bindings القياسية وtexture transforms وwrap modes الموثقة، وتستخدم 1×1 neutral texture فقط عندما تكون القيمة `Null` أو `Unresolved` ضمن سياق fallback الآمن.

ينتج `PremiumShaderPropertyInjector` خطة قابلة للمراجعة للـURP Lit أوHDRP Lit. يربط أسماء source القياسية فقط:

| Unity property مقروء | Standard-Lit target | حالة غير قياسية |
| --- | --- | --- |
| `_MainTex` أو`_BaseMap` | `_BaseMap` | غير المعروفة تسجل `NotMapped`. |
| `_BumpMap` أو`_NormalMap` | `_NormalMap` | لا تغير normal convention بلا metadata. |
| `_MetallicGlossMap` | `_MetallicGlossMap` | لا يحلل shader خاصًا. |
| `_OcclusionMap` | `_OcclusionMap` | لا ينشئ texture مفقودًا. |
| `_EmissionMap` | `_EmissionMap` | لا يستنتج emission color. |

`PremiumTextureTranscoder` يعتمد على `TextureConverter` القائم في AssetRipper. يكتب PNG أوTGA أوEXR فقط بعد `CheckAssetIntegrity()` ونجاح `TryConvertToBitmap`. تمثل حالة `Unsupported` رفضًا واضحًا للمفكك، وليست صورة صامتة أو بديلًا تخمينيًا.

### 4.6 CLI وواجهة التشخيص

| خيار CLI | السلوك المتحقق |
| --- | --- |
| `--export-verified-only` | يستبعد collections المصنفة Partial أوUnavailable ويحتفظ بـEmbedded وKnownEngineSchema فقط. |
| `--export-diagnostics json|html` | يكتب تقرير Premium بالصيغة المختارة. |
| `--fallback-textures <dir>` | يفهرس صور fallback المقدمة من المستخدم بترتيب حتمي ويضعها في التشخيص/manifest. |
| `--textures` | يصدّر فقط IImageTexture الذي اجتاز المفكك القائم. |
| `--texture-format png|tga|exr` | يحدد صيغة ناتج `--textures`. |

تتوفر في GUI صفحة `/PremiumDiagnostics`. هي صفحة قراءة فقط وتعرض ملخص TypeTree partial/unavailable وreference cycles وmaterial bindings غير المحلولة/null وخطة verified-only، وتوفر حقل بحث محلي لا يرسل أو يغير البيانات.

## 5. البناء والاختبارات

تم تشغيل `dotnet restore AssetRipper.slnx --nologo -v:minimal`. ولأن compiler المتاح في بيئة البناء يتطلب analyzer assemblies متوافقة، خُفض مرجع `Microsoft.CodeAnalysis.CSharp` في مولدات المصدر الأربعة مؤقتًا من 5.6.0 إلى 5.0.0 أثناء restore/build/publish، ثم أعيد إلى 5.6.0 فورًا. هذه ليست تعديلات ضمن الإيداع أو أرشيف المصدر النهائي.

| عملية | الأمر الأساسي | النتيجة |
| --- | --- | --- |
| Restore | `dotnet restore AssetRipper.slnx --nologo -v:minimal` | نجح. |
| Release GUI build | `dotnet build ...AssetRipper.GUI.Premium.csproj -c Release --no-restore` | نجح، 0 warnings، 0 errors. |
| Release CLI build | `dotnet build ...AssetRipper.Tools.CLI.csproj -c Release --no-restore` | نجح، 0 warnings، 0 errors. |
| Regression | `dotnet test Source/*/*.Tests.csproj --no-build --no-restore` بالتسلسل | 532 ناجحًا، 0 فشل. |
| CLI smoke check | `AssetRipper.CLI.dll --help` | ظهرت خيارات verified-only وfallback-textures وtextures وtexture-format. |
| GUI smoke check | `AssetRipper.GUI.Premium.dll --premium-authorized --headless` | ظهرت سياسة plaintext المصرح بها والتحذير من encryption/DRM/memory dumps. |

### تفاصيل المشاريع التسعة

| مشروع الاختبار | ناجح | فاشل |
| --- | ---: | ---: |
| AssetRipper.AssemblyDumper.Tests | 9 | 0 |
| AssetRipper.Assets.Tests | 57 | 0 |
| AssetRipper.GUI.Web.Tests | 6 | 0 |
| AssetRipper.IO.Files.Tests | 141 | 0 |
| AssetRipper.Numerics.Tests | 65 | 0 |
| AssetRipper.Premium.Tests | 22 | 0 |
| AssetRipper.SerializationLogic.Tests | 48 | 0 |
| AssetRipper.Tests | 173 | 0 |
| AssetRipper.Yaml.Tests | 11 | 0 |
| **الإجمالي** | **532** | **0** |

## 6. النشر والحزم

نُشرت واجهة GUI وCLI محليًا على `win-x64` بوضع self-contained، مع `PublishAot=false` و`PublishTrimmed=false` و`PublishSingleFile=false`. اختير هذا الوضع كي تبقى ملفات التشخيص والمكتبات والتبعيات واضحة وقابلة للفحص في Release Candidate.

### 6.1 حزمة Windows

| الحقل | القيمة |
| --- | --- |
| اسم الأرشيف | `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.16-rc1-Windows-x64.zip` |
| الحجم | 178,431,596 بايت |
| عدد المداخل | 923 |
| فحص ZIP | `unzip -t` نجح دون أخطاء. |
| SHA-256 | `ff6fc576a01d7105e434a8e44f0dea7052a3390fff22d6c9fa7924858139edd8` |
| GUI | `GUI/AssetRipper.GUI.Premium.exe` موجود. |
| CLI | `CLI/AssetRipper.CLI.exe` موجود. |
| الوثائق | README وGPL-3.0 وتقارير Phase 3 وPhase 4 ودليل البناء موجودة. |

### 6.2 حزمة المصدر

| الحقل | القيمة |
| --- | --- |
| اسم الأرشيف | `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.16-rc1-Source.zip` |
| الحجم | 41,060,449 بايت |
| عدد المداخل | 1,744 |
| فحص ZIP | `unzip -t` نجح دون أخطاء. |
| SHA-256 | `9abb47fa08211f9ea0d8a04bb1c1787a5ff1231d27c89af2bd02abd64f681d45` |
| ملفات تحقق | الحل، `PremiumTextureTranscoder.cs`، `PremiumShaderPropertyInjector.cs`، `PremiumHierarchyReconstructor.cs`، `PremiumImportDiagnostics.cs`، GUI Program، CLI Program، ودليل البناء موجودة. |

## 7. القيود المفتوحة وخطة التحقق التالية

هذه القيود ليست أخطاء مخفية؛ هي حدود مقصودة كي لا يتسبب المنتج في استنتاج أو استبدال محتوى غير مثبت:

| البند | الوضع الحالي | شرط الإغلاق |
| --- | --- | --- |
| Mip chains | لا يصدر transcoder أو يولد mip chain مستقلًا؛ يسجل `NotExposed`. | عينة مسموح بها مع layout mip قابل للقراءة لكل texture type، ثم اختبار يحفظ أو يرفض المips. |
| GLB user fallback | catalog موجود في CLI والخطة، لكنه لا يطبق replacement تلقائيًا داخل GLB. | ربط export target واضح يطبق فقط على `Unresolved` binding مع اختبار material/texture فعلي. |
| Color space | يبقى `Unknown` عند غياب metadata صريح. | قراءة sRGB/linear metadata من schema متاح مع اختبار تحويل غير تخميني. |
| Audio/Video final conversion | جرد ومسارات آمنة موجودة؛ لا توجد عينة محلية مصرح بها ذات AudioClip وVideoClip صالحين للاختبار النهائي. | عينة Unity مصرح بها تحتوي audio/video غير محميين. |
| صيغ ضغط texture الفعلية | المفكك القائم هو المسار، لكن لم تتوافر عينة قبول محلية لكل ASTC/ETC2/PVRTC/Crunch. | عينة مرخصة لكل صيغة، ومقارنة decoded output مع خصائص المصدر. |

## 8. إرشادات الاستلام والتشغيل

بعد تنزيل وفك حزمة Windows، تحقق من البصمة عبر PowerShell:

```powershell
Get-FileHash .\AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.16-rc1-Windows-x64.zip -Algorithm SHA256
```

شغّل واجهة Premium فقط عندما تملك تفويضًا لمعالجة المدخل النصي:

```text
GUI\AssetRipper.GUI.Premium.exe --premium-authorized
```

لفحص أوامر CLI أو استخدام التصدير الموثق:

```text
CLI\AssetRipper.CLI.exe --help
CLI\AssetRipper.CLI.exe --input game_Data --output export --batch --export-verified-only --export-diagnostics html
CLI\AssetRipper.CLI.exe --input game_Data --output export --textures --texture-format png
```

استخدم حزمة المصدر عند الحاجة إلى مراجعة الكود أو إعادة البناء. يحافظ المشروع على ترخيص GPL-3.0 وملفات النسبة الخاصة بـAssetRipper الأصلي داخل المصدر المرفق.

## 9. الخلاصة القابلة للتدقيق

الحزمة ليست وصفًا نظريًا: توجد GUI وCLI منشورتان محليًا، وأرشيف مصدر، وبصمات SHA-256، وسجلات build/test، ونتيجة اختبار من 532 ناجحًا من أصل 532. لم تدع الحزمة دعمًا لما لم يختبر: المips وGLB fallback replacement وبعض عينات الضغط والوسائط تظل بنود تحقق لاحقة. لم يُرفع أي من هذه التغييرات أو الحزم إلى GitHub في هذا التسليم.
