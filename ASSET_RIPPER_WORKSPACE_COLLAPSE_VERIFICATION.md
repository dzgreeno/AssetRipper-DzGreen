# AssetRipper Workspace Collapse — Verification

هذه النسخة مبنية فوق مشروع AssetRipper المعدّل الحالي، وتضيف تحكمًا قابلًا للطي والإظهار داخل Asset Workspace دون فصل المعاينة عن قائمة الأصول.

| Feature | Result |
|---|---|
| Hide/Show asset list | زر `Hide asset list` و`Show asset list` في رأس قائمة الملفات |
| Hierarchy panel | زر `Hierarchy` لإخفاء/إظهار الشجرة اليسرى |
| Asset actions panel | زر `Asset actions` لإخفاء/إظهار اللوحة اليمنى |
| Focus preview | زر يوسّع المعاينة المركزية ويخفي اللوحتين الجانبيتين مؤقتًا |
| State persistence | الحالات محفوظة في localStorage مع بقاء selected asset والفلاتر |
| JavaScript | `node --check` نجح |
| GUI.Web build | 0 warnings, 0 errors |
| GUI.Free build | 0 warnings, 0 errors |
| Windows publish | نجح، self-contained win-x64 |

اختبار واجهة التفاعل النهائي يجب رؤيته على Windows في نسخة النشر الجديدة لأن البيئة الحالية Linux لا تشغّل ملف PE نفسه. لا تتطلب الميزة إعادة معالجة ملفات Unity؛ يكفي تشغيل النسخة الجديدة وإعادة تحميل الصفحة.
