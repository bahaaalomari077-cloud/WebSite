# دليل نشر موقع Credit Plus على IIS

## هيكل النشر

```
الفرونت اند: c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\frontend
الباك اند:  c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\backend
```

---

## الخيار 1: موقع واحد مع الباك اند كتطبيق فرعي (الأفضل)

### الخطوة 1: إنشاء موقع رئيسي للفرونت اند

1. افتح **IIS Manager**
2. انقر بزر الماوس الأيمن على **Sites** → **Add Website**
3. قم بالإعدادات التالية:
   - **اسم الموقع:** `Credit-Plus-Website`
   - **المسار الفيزيائي:** `c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\frontend`
   - **اسم المضيف:** `yourdomain.com` (أو `localhost` للاختبار المحلي)
   - **المنفذ:** `80` (HTTP) أو `443` (HTTPS)
   - **البروتوكول:** `http` أو `https`
4. انقر **OK**

### الخطوة 2: إنشاء Application Pool للباك اند

1. انقر بزر الماوس الأيمن على **Application Pools** → **Add Application Pool**
2. قم بالإعدادات التالية:
   - **الاسم:** `Credit-Plus-Backend`
   - **.NET CLR version:** `.NET CLR Version v4.0.30319` (أو No Managed Code)
   - **Managed pipeline mode:** `Integrated`
3. انقر **OK**

### الخطوة 3: إنشاء تطبيق فرعي للـ API

1. في IIS Manager، اختر الموقع: `Credit-Plus-Website`
2. انقر بزر الماوس الأيمن → **Add Application**
3. قم بالإعدادات التالية:
   - **الاسم المستعار:** `api`
   - **Application pool:** `Credit-Plus-Backend`
   - **المسار الفيزيائي:** `c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\backend`
4. انقر **OK**

### الخطوة 4: إعدادات URL Rewrite لـ SPA Routing

1. اختر الموقع الرئيسي `Credit-Plus-Website`
2. انقر نقراً مزدوجاً على **URL Rewrite**
3. انقر **Add Rule(s)** → **Blank rule**
4. قم بالإعدادات التالية:
   - **الاسم:** `SPA Routing`
   - **النمط:** `.*`
   - **الشروط:** أضف شرط: `{REQUEST_FILENAME}` ليس ملف AND `{REQUEST_FILENAME}` ليس مجلد
   - **الإجراء:** إعادة كتابة إلى `index.html`
5. انقر **Apply**

### الخطوة 5: إعدادات تطبيق الباك اند

1. اختر التطبيق الفرعي `/api`
2. انقر نقراً مزدوجاً على **Configuration Editor** (أو عدّل `web.config`)
3. تأكد من أن `web.config` يحتوي على:

```xml
<configuration>
  <system.webServer>
    <staticContent>
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
    </staticContent>
    <httpProtocol>
      <customHeaders>
        <add name="Access-Control-Allow-Origin" value="*" />
        <add name="Access-Control-Allow-Methods" value="GET, POST, PUT, DELETE, OPTIONS" />
        <add name="Access-Control-Allow-Headers" value="Content-Type" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

### الخطوة 6: تعيين الصلاحيات

1. انقر بزر الماوس الأيمن على مجلد التطبيق `/api` → **Properties** → **Security**
2. امنح حساب **IIS AppPool\Credit-Plus-Backend** الصلاحيات التالية:
   - Read
   - Read & Execute
   - List Folder Contents

---

## الخيار 2: مواقع منفصلة

### موقع الفرونت اند

1. إنشاء موقع يشير إلى `publish\iis\frontend`
2. المنفذ: `80` أو `443`
3. المضيف: `yourdomain.com`

### موقع الباك اند

1. إنشاء موقع يشير إلى `publish\iis\backend`
2. المنفذ: `5001` (أو أي منفذ متاح)
3. أضف binding لـ localhost أو مجالك

### تحديث إعدادات الفرونت اند

حدّث إعدادات الـ proxy أو API base URL في Angular `environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'http://yourdomain.com:5001/api'  // عنوان الباك اند على منفذ منفصل
};
```

أعد البناء: `npm run build`

---

## إعدادات قاعدة البيانات

### Connection String في `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=credit_plus;Username=postgres;Password=123456"
  }
}
```

**مهم:** حدّث هذه القيم لموقع قاعدة البيانات PostgreSQL الخاص بك.

### متطلبات PostgreSQL

- يجب أن تكون PostgreSQL قيد التشغيل وقابلة للوصول
- اسم قاعدة البيانات: `credit_plus`
- سيتم إنشاء الجداول تلقائياً عند التشغيل الأول (إذا تم تكوين EnsureDatabase)

---

## متغيرات البيئة

عيّن هذه المتغيرات كمتغيرات بيئة Application Pool في IIS:

1. في IIS Manager، اختر Application Pool
2. انقر بزر الماوس الأيمن → **Set Application Pool Defaults** → **Recycling**
3. أو أنشئ ملف `.env` في مجلد الباك اند:

```
PORT=5001
ASPNETCORE_ENVIRONMENT=Production
ConnectionString__DefaultConnection=Host=localhost;Port=5433;Database=credit_plus;Username=postgres;Password=123456
```

---

## إعدادات API الفرونت اند

تم إعداد الفرونت اند للتواصل مع الباك اند على: **`http://localhost:5001`**

هذا معرّف في `proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true
  },
  "/uploads": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true
  }
}
```

**للنشر الإنتاجي**، حدّث هذا لعنوان الباك اند الإنتاجي.

---

## إعدادات CORS (إذا لزم الحال)

إذا كان الفرونت اند والباك اند على نطاقات مختلفة، فعّل CORS في `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

---

## الاختبار

### اختبار الباك اند

```bash
curl http://localhost:5001/api
```

الاستجابة المتوقعة: `{"message":"Credit Plus ASP.NET backend is running"}`

### اختبار الفرونت اند

انتقل إلى: `http://yourdomain.com/` (أو `http://localhost/` إذا كنت تستخدم localhost)

---

## استكشاف الأخطاء والتصحيح

### 502 Bad Gateway
- خدمة الباك اند غير مشغّلة
- تحقق من أن Application Pool قد بدأ
- تحقق من أن منفذ الباك اند متاح للوصول إليه

### 404 Not Found
- قد لا تكون قاعدة URL Rewrite معدّلة بشكل صحيح
- ملف web.config مفقود أو غير صحيح

### فشل استدعاءات API للفرونت اند
- تحقق من إعدادات proxy
- تحقق من تشغيل الباك اند على المنفذ 5001
- تحقق من وحدة التحكم في المتصفح لأخطاء CORS
- تحقق من صلاحيات Application Pool

### خطأ اتصال قاعدة البيانات
- PostgreSQL غير مشغّل
- Connection string غير صحيح
- قاعدة البيانات غير موجودة

---

## قائمة فحص الإنتاج

- [ ] PostgreSQL قيد التشغيل وقابل للوصول إليه
- [ ] تم عمل نسخة احتياطية من قاعدة البيانات الحالية
- [ ] IIS مثبت وقيد التشغيل
- [ ] تم إنشاء Application Pool جديد للباك اند
- [ ] تم نسخ الفرونت اند إلى مجلد IIS
- [ ] تم نسخ الباك اند إلى مجلد IIS
- [ ] تم تكوين web.config للاثنين
- [ ] تم تعيين صلاحيات الملفات
- [ ] اختبار محلي: `http://localhost/`
- [ ] اختبار نقطة نهاية API: `http://localhost/api`
- [ ] تم تكوين شهادة SSL (للإنتاج)
- [ ] تم إعداد استراتيجية النسخ الاحتياطي التلقائي
- [ ] تم تكوين المراقبة والتسجيل

---

## مواقع الملفات

| المكون | الموقع |
|-------|--------|
| بناء الفرونت اند | `dist/credit-plus-angular/browser/` |
| نشر الفرونت اند | `publish/iis/frontend/` |
| نشر الباك اند | `publish/iis/backend/` |
| Web Config (الفرونت اند) | `publish/iis/frontend/web.config` |
| Web Config (الباك اند) | `publish/iis/backend/web.config` |
| إعدادات التطبيق | `publish/iis/backend/appsettings.json` |

---

## نقاط النهاية (API Endpoints)

سيتواصل الفرونت اند مع هذه النقاط:

```
POST   /api/auth/login
GET    /api/news
GET    /api/articles
GET    /api/admin
POST   /api/contact
```

جميعها موجهة عبر: `http://yourdomain.com/api/` (موقع واحد) أو عنوان الباك اند المعيّن (مواقع منفصلة)

---

## الملخص السريع

### للموقع الواحد (الأفضل):
```
الفرونت اند: http://yourdomain.com/
الـ API: http://yourdomain.com/api/
```

### لمواقع منفصلة:
```
الفرونت اند: http://yourdomain.com/
الـ API: http://yourdomain.com:5001/
```

**الخطوات التالية:**
1. ✅ تم بناء الفرونت اند
2. ✅ تم تحضير ملفات الباك اند
3. 📝 الآن: تطبيق هذا الدليل على IIS
4. 🧪 الاختبار والتحقق من الاتصال

