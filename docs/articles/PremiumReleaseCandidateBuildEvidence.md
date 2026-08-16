# أدلة بناء Release Candidate المحلي

**المنتج:** AssetRipper DzGreen Premium  
**المالك:** dzgreeno  
**الفرع:** `dzgreen-vnext-hardening`  
**النشر الخارجي:** لم يتم تنفيذ أي push أو GitHub Release أو رفع ZIP.

## أساس المصدر

تم بناء Release Candidate فوق شجرة `AssetRipper-DzGreen-vnext` وملف الحل `AssetRipper.slnx` الموجود، لا فوق مشروع بديل. تضم مكتبة Premium الملفات المنفذة من Phase 0 إلى Phase 4، ومنها analyzers وstream processors وhierarchy/prefab/Mecanim diagnostics وTextureTranscoder وshader assignment plan.

| المكون | ملف نقطة الدخول |
| --- | --- |
| واجهة Premium | `Source/AssetRipper.GUI.Premium/Program.cs` |
| CLI | `Source/AssetRipper.Tools.CLI/Program.cs` |
| مكتبة Premium | `Source/AssetRipper.Premium/` |
| اختبارات Premium | `Source/AssetRipper.Premium.Tests/PremiumInputPolicyTests.cs` |
| الحل | `AssetRipper.slnx` |

## أوامر التحقق المنفذة

```text
dotnet restore AssetRipper.slnx --nologo -v:minimal
dotnet build Source/AssetRipper.GUI.Premium/AssetRipper.GUI.Premium.csproj --no-restore --nologo -c Release -v:minimal /m:1
dotnet build Source/AssetRipper.Tools.CLI/AssetRipper.Tools.CLI.csproj --no-restore --nologo -c Release -v:minimal /m:1
for project in Source/*/*.Tests.csproj; do dotnet test "$project" --no-build --no-restore --nologo -v:minimal /m:1; done
dotnet publish Source/AssetRipper.GUI.Premium/AssetRipper.GUI.Premium.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false -p:PublishTrimmed=false -p:PublishSingleFile=false
dotnet publish Source/AssetRipper.Tools.CLI/AssetRipper.Tools.CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false -p:PublishTrimmed=false -p:PublishSingleFile=false
```

تحتاج بيئة البناء الحالية توافقًا مؤقتًا للمولدات فقط: خُفض مرجع `Microsoft.CodeAnalysis.CSharp` من 5.6.0 إلى 5.0.0 في المولدات الأربعة أثناء `restore/build/publish`، ثم أعيد إلى 5.6.0 فورًا. لا يدخل هذا التعديل المؤقت في المصدر المسلّم.

## نتائج متحققة

| الفحص | النتيجة |
| --- | --- |
| استعادة NuGet | نجحت لحل `AssetRipper.slnx` ولمشروعي GUI وCLI في النشر runtime-specific. |
| بناء Release GUI | نجح، 0 تحذيرات، 0 أخطاء. |
| بناء Release CLI | نجح، 0 تحذيرات، 0 أخطاء. |
| مشاريع الاختبار | 9 مشاريع، 532 اختبارًا ناجحًا، 0 فشل. |
| اختبارات Premium | 22 اختبارًا ناجحًا، 0 فشل. |
| CLI runtime | عرضت `--export-verified-only` و`--fallback-textures` و`--textures` و`--texture-format`. |
| GUI runtime | طبع رسالة قبول البيانات النصية المصرح بها وتحذيرًا صريحًا من التشفير وDRM وmemory dumps. |
| Windows publish | تم إنشاء `gui/AssetRipper.GUI.Premium.exe` و`cli/AssetRipper.CLI.exe` في staging المحلي. |

## حدود معروفة في Release Candidate

هذا الدليل لا يدعي دعم بيانات Unity غير المقروءة أو المشفرة. توجد بنود عمل متبقية عمداً: حفظ/تصدير mip chains فقط بعد إثبات source layout، وربط catalog الصور المقدمة من المستخدم بمصدّر GLB بحيث يطبق فقط على material binding المعلن `Unresolved`. لا تولد النسخة mipmaps أو textures أو bindings مفقودة تخمينًا.

## الأدلة الخام

توجد سجلات التشغيل المحلية التي أنتجت هذه النتائج في:

```text
/tmp/restore-release-candidate.log
/tmp/build-release-gui-cli-roslyn-compat.log
/tmp/test-release-candidate-no-build.log
/tmp/publish-release-candidate-win-x64.log
```

تدرج الحزمة الموزعة نسخة من هذا السجل، وREADME، وترخيص GPL-3.0، وتقارير Phase 3 وPhase 4، وmanifest يحتوي البصمة النهائية.
