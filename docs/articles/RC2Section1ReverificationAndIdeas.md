# RC2 — القسم 1: إعادة تحقق RC1 وتدقيق الوحدات وترتيب التحسينات

**الفرع:** `dzgreen-vnext-hardening`  
**النطاق:** بيانات Unity النصية المصرح بها فقط.  
**النشر الخارجي:** لم يُنفذ push أو GitHub Release.

## A. مطابقة أرشيفات RC1

| الأرشيف | SHA-256 المتوقع | SHA-256 المحسوب | المطابقة |
| --- | --- | --- | --- |
| `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.16-rc1-Windows-x64.zip` | `ff6fc576a01d7105e434a8e44f0dea7052a3390fff22d6c9fa7924858139edd8` | `ff6fc576a01d7105e434a8e44f0dea7052a3390fff22d6c9fa7924858139edd8` | نجحت |
| `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.16-rc1-Source.zip` | `9abb47fa08211f9ea0d8a04bb1c1787a5ff1231d27c89af2bd02abd64f681d45` | `9abb47fa08211f9ea0d8a04bb1c1787a5ff1231d27c89af2bd02abd64f681d45` | نجحت |

استخدم التحقق ملفي `.sha256` الموزعين مع الأرشيفين عبر `sha256sum -c`. يثبت ذلك سلامة الأرشيفين اللذين تم اختبارهما، لا صلاحية أي ملف مصدر خارجي غير موجود في الحزمة.

## B. إعادة بناء RC1 من أرشيف المصدر

استُخرج أرشيف RC1 في مسار تحقق مؤقت منفصل، ثم شُغلت الأوامر التالية على المصدر المستخرج:

```text
dotnet restore AssetRipper.slnx --nologo -v:minimal /m:1
dotnet restore Source/AssetRipper.Tools.CLI/AssetRipper.Tools.CLI.csproj --nologo -v:minimal /m:1
dotnet build Source/AssetRipper.GUI.Premium/AssetRipper.GUI.Premium.csproj --no-restore --nologo -c Release -v:minimal /m:1
dotnet build Source/AssetRipper.Tools.CLI/AssetRipper.Tools.CLI.csproj --no-restore --nologo -c Release -v:minimal /m:1
```

| بناء | التحذيرات | الأخطاء | النتيجة |
| --- | ---: | ---: | --- |
| GUI.Premium Release | 0 | 0 | نجح |
| Tools.CLI Release | 0 | 0 | نجح |

تتطلب بيئة الاختبار الحالية توافق Roslyn مؤقتًا للمولدات الأربعة فقط. خُفض `Microsoft.CodeAnalysis.CSharp` مؤقتًا إلى 5.0.0 في نسخة المصدر المستخرجة أثناء restore/build، ثم أعيد إلى 5.6.0 قبل اكتمال التحقق. بقيت شجرة RC2 الفعلية على 5.6.0.

## C. إعادة تشغيل المشاريع التسعة

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

## D. تدقيق الوحدات الست

فُحصت الكلمات `TODO` و`NotImplementedException` و`NotSupportedException` والنتائج null/default في الوحدات المطلوبة. ظهرت أربعة `return null` في `PremiumVertexStreamProcessor` فقط؛ وهي نتائج رفض موثقة في مسارات channel/layout/format غير المدعومة، ويضاف معها `PremiumVertexStreamIssue`. لا تشكل stub ولا تسمح بتخمين layout. لم يظهر TODO أو `NotImplementedException` أو `NotSupportedException` أو placeholder قابل للتنفيذ في الوحدات الست.

| الوحدة | أسطر تقريبة | نتيجة التدقيق | الإجراء |
| --- | ---: | --- | --- |
| `PremiumTypeTreeCoverageAnalyzer.cs` | 94 | لا stub marker | لا تغيير مطلوب |
| `PremiumReferenceGraph.cs` | 239 | لا stub marker | لا تغيير مطلوب |
| `PremiumVertexStreamProcessor.cs` | 287 | nulls مقصودة لمسار رفض تشخيصي | يؤكدها اختبار vertex layout rejection القائم |
| `PremiumHierarchyReconstructor.cs` | 267 | لا stub marker | لا تغيير مطلوب |
| `PremiumTextureTranscoder.cs` | 117 | لا stub marker | لا تغيير مطلوب |
| `PremiumShaderPropertyInjector.cs` | 63 | لا stub marker | لا تغيير مطلوب |

## E. ترتيب تحسينات RC2 المقترحة

| الترتيب | التحسين | الأولوية | سبب الاختيار | حالة التنفيذ |
| ---: | --- | --- | --- | --- |
| 1 | GLB fallback injection المقيد لـUnresolved فقط | عالية | يغلق بند RC1 مع الحفاظ على Null neutral fallback وعدم استبدال binding صالح. | مقرر ضمن RC2 |
| 2 | تشخيصات وأدلة mip/color-space غير التخمنية | عالية | تمنع ادعاءات fidelity غير مثبتة وتغلق مسارات التقرير المفتوحة. | مقرر ضمن RC2 |
| 3 | وضع `--ci` مع exit codes وone-line JSON summary | عالية | يجعل التشغيل الآلي قابلًا للتدقيق ويسهل إعادة التجارب. | مقرر ضمن RC2 |
| 4 | run-to-run diagnostic and manifest diff | عالية | يثبت الحتمية ويكشف تغيرات export planning بلا مقارنة يدوية. | مقرر ضمن RC2 |
| 5 | asset-level provenance records في manifests | عالية | يربط قرار التصدير بالأصل وcoverage وسبب الإقصاء. | مقرر ضمن RC2 |
| 6 | incremental export resume checkpoints | متوسطة | يقلل تكرار التصدير بعد توقف موثق دون تغيير محتوى الأصول. | مؤجل بعد top 5 |
| 7 | deterministic bounded parallel pipeline | متوسطة | يحسن الأداء دون المساس بترتيب diagnostics أو manifests. | مؤجل بعد top 5 |
| 8 | compatibility matrix auto-report | متوسطة | يوضح Unity schema/TypeTree coverage لكل تشغيل. | مؤجل بعد top 5 |
| 9 | preview صفحة export-plan داخل diagnostics dashboard | متوسطة | يعرض قرارات verified-only وfallback قبل التصدير. | مؤجل بعد top 5 |
| 10 | localization-ready diagnostic message catalog | متوسطة | يفصل codes الحتمية عن النصوص المترجمة. | مؤجل بعد top 5 |
| 11 | round-trip verifier orchestration | متوسطة | يؤتمت مقارنة counts عند وجود importer مرخص في البيئة. | مؤجل لحين fixtures حقيقية |
| 12 | watch-mode للحاويات الجديدة | منخفضة | مفيد للتدفقات المحلية لكنه يحتاج سياسات ثبات وأمان إضافية. | مؤجل |

## F. الأدلة المرفقة

| الدليل | المسار المحلي | المحتوى |
| --- | --- | --- |
| مقارنة البصمات | `/tmp/rc2-rc1-checksum-comparison.txt` | SHA-256 المحسوب ونتيجة `sha256sum -c`. |
| سجل البناء النظيف | `/tmp/rc2-rc1-rebuild-clean.log` | restore وبناء GUI/CLI الناجحان من أرشيف RC1. |
| سجل الاختبارات | `/tmp/rc2-rc1-tests.log` | مخرجات المشاريع التسعة، 532 ناجحًا و0 فشل. |
| تدقيق الوحدات | `/tmp/rc2-core-module-audit.txt` | نتائج البحث في الوحدات الست وتفسير nulls. |
