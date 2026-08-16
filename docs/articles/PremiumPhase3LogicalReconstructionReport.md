# تقرير Phase 3: إعادة البناء المنطقي وتنسيق التصدير

**المنتج:** AssetRipper DzGreen Premium  
**المالك:** dzgreeno  
**حالة النشر:** محلي فقط؛ لا يوجد رفع إلى GitHub أو إصدار عام ضمن هذه المرحلة.

## الملخص

تضيف Phase 3 طبقة منطقية حتمية فوق بيانات Unity التي حملها المستورد العادي بالفعل. لا تتعامل هذه الطبقة مع التشفير أو مفاتيح التشغيل أو الذاكرة أو الحاويات الخاصة. عندما يكون schema أو stream غير متاح، تسجل نتيجة غير متاحة أو غير مدعومة بدلاً من ملء بيانات تخمينية.

| المجال | التنفيذ | المخرج الحتمي |
| --- | --- | --- |
| Transform / RectTransform | `PremiumHierarchyReconstructor` | رسم parent/child، تحقق اتفاق الرابطين، كشف cycles، ومصفوفات TRS world فقط عندما لا تمر السلسلة بدورة أو parent مفقود. |
| Prefab | `PremiumPrefabOverrideResolver` | تصنيف تعريف/mثيل، جرد dependency fields المسماة Modification، وresolver لا يغير base definition ولا يضيف property أو script غير موجود. |
| Mecanim | `PremiumMecanimStateMachineAnalyzer` | جرد Controller وstate machine وstate وtransition وconditions وblend-tree states، مع ترتيب canonical ومعلمات غير محلولة ظاهرة في التشخيص. |
| BlendTree | `PremiumBlendTreeEvaluator` | حساب 1D من thresholds معلنة، وحساب 2D inverse-distance من مواقع معلنة؛ يرفض القيم غير المنتهية أو الأنماط غير المحددة. |
| Audio / Video | `PremiumAudioMediaProcessor` | جرد streams المقروءة فقط، وإخراج audio عبر المفكك القياسي القائم أو video بحاويته الأصلية بعد integrity check. |
| CLI | `--export-verified-only` و`--fallback-textures` و`--export-diagnostics` | فلترة Partial وUnavailable، فهرس صور fallback للمستخدم، وتقرير JSON أو HTML؛ الفهرس يوثق الاختيارات ولا يبدل موادًا بلا مسار تصدير صريح. |

## معمارية التنفيذ

ينشأ `PremiumImportDiagnostics` التقارير بترتيب ثابت بعد الاستيراد. يضم التقرير الآن `Hierarchy` و`PrefabOverrides` و`Mecanim` و`Media` إلى جانب TypeTree وPPtr وMaterial وVertex streams. لا تعدل هذه التقارير أي asset أو PPtr أو prefab base.

> لا تحسب world matrix لعقدة تقع داخل cycle أو تتبع ancestor دوريًا. كما لا يطبق resolver الخاص بالـPrefab override قيمة إلا على property معلن صراحةً ضمن النسخة التي استدعاها؛ وتبقى properties غير المعروفة في قائمة `UnresolvedOverrides`.

تستعمل CLI خدمة Premium نفسها لتكوين `PremiumVerifiedOnlyPlan`. تقبل الخطة أصول `Embedded` أو `KnownEngineSchema` فقط وتستبعد `Partial` و`Unavailable`، مع سبب لكل قرار. `--fallback-textures` يقبل ملفات الصور ذات الامتدادات المعروفة في المجلد المعطى ويرتبها حتميًا؛ لا يُعامل ملف موجود على القرص على أنه Texture Unity صالح إلى أن يمرر عبر مسار exporter قادر على تطبيقه.

## الاستخدام

```text
AssetRipper.CLI --input game_Data --output export --batch --raw \
  --export-verified-only --export-diagnostics html

AssetRipper.CLI --input game_Data --output export --batch --fbx \
  --fallback-textures replacement_textures --export-diagnostics json
```

يؤدي الخيار `--export-verified-only` إلى batch mode حتى إن لم يمرر المستخدم `--batch` صراحة. يسجل manifest عدد الأصول أو roots المتجاوزة وخريطة fallback textures. يبقى الاستبدال الفعلي للصور محدودًا بمصدّر يعلن دعمًا صريحًا له؛ لا تعيد هذه المرحلة كتابة ملفات Unity أو تصنع Texture مفقودًا.

## التحقق

| التحقق | النتيجة |
| --- | --- |
| اختبارات Premium | 21 ناجحة، 0 فشل. تغطي stream math وhierarchy cycles وPrefab non-invention وverified-only وBlendTree. |
| الانحدار الكامل | 9 مشاريع اختبار، **531 اختبارًا ناجحًا، 0 فشل**. |
| واجهة Premium | تم البناء بنجاح دون أخطاء أو تحذيرات خلال بناء التحقق. |
| CLI | تم البناء بنجاح، وتحققت المساعدة وخيار JSON diagnostics وHTML diagnostics وخيار verified-only وفهرس fallback textures على مجلد اختبار غير متلف. |

## حدود صريحة

لا تتضمن هذه المرحلة تحليلًا لبايتات controller غير المكشوفة في schema، أو توليد transition/condition ناقص، أو قراءة scripts مفقودة. لم تكن العينة المحلية المتاحة في هذه الجلسة تحتوي AudioClip أو VideoClip صالحين لاختبار التحويل النهائي؛ لذلك يغطي الاختبار البناء، التقرير الفارغ، ومسار CLI، بينما يتطلب تحقق WAV/OGG أو video container النهائي عينة Unity مصرح بها تحتوي تلك الأصول.

كذلك لا تعمل هذه المرحلة على الملفات المشفرة أو المحمية أو runtime memory dumps أو custom virtual file systems. هذه الحالات تبقى خارج نطاق المنتج وتظهر كمدخلات مرفوضة أو بيانات غير متاحة.
