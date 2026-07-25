# IIS Deployment Guide - Credit Plus Website

## Deployment Structure

```
Frontend: c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\frontend
Backend:  c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\backend
```

---

## Deployment Option 1: Single Site with Backend as Sub-Application (Recommended)

### Step 1: Create Main Website for Frontend

1. Open **IIS Manager**
2. Right-click **Sites** → **Add Website**
3. Configure:
   - **Site name:** `Credit-Plus-Website`
   - **Physical path:** `c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\frontend`
   - **Host name:** `yourdomain.com` (or `localhost` for local testing)
   - **Port:** `80` (HTTP) or `443` (HTTPS)
   - **Protocol:** `http` or `https`
4. Click **OK**

### Step 2: Create Application Pool for Backend

1. Right-click **Application Pools** → **Add Application Pool**
2. Configure:
   - **Name:** `Credit-Plus-Backend`
   - **.NET CLR version:** `.NET CLR Version v4.0.30319` (or No Managed Code if using standalone)
   - **Managed pipeline mode:** `Integrated`
3. Click **OK**

### Step 3: Create Sub-Application for Backend API

1. In IIS Manager, select your website: `Credit-Plus-Website`
2. Right-click → **Add Application**
3. Configure:
   - **Alias:** `api`
   - **Application pool:** `Credit-Plus-Backend`
   - **Physical path:** `c:\Users\MSI1\Downloads\Credit Plus Website\Credit Plus Website\credit-plus-angular\publish\iis\backend`
4. Click **OK**

### Step 4: Configure URL Rewrite for Frontend SPA Routing

1. Select the main website `Credit-Plus-Website`
2. Double-click **URL Rewrite**
3. Click **Add Rule(s)** → **Blank rule**
4. Configure:
   - **Name:** `SPA Routing`
   - **Pattern:** `.*`
   - **Conditions:** Add condition: `{REQUEST_FILENAME}` is NOT a file AND `{REQUEST_FILENAME}` is NOT a directory
   - **Action:** Rewrite to `index.html`
5. Click **Apply**

### Step 5: Configure Backend Application Settings

1. Select the `/api` sub-application
2. Double-click **Configuration Editor** (or edit `web.config`)
3. Ensure `web.config` contains:

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

### Step 6: Set Permissions

1. Right-click the `/api` application folder → **Properties** → **Security**
2. Grant **IIS AppPool\Credit-Plus-Backend** account:
   - Read
   - Read & Execute
   - List Folder Contents

---

## Deployment Option 2: Separate Sites

### Frontend Site

1. Create website pointing to `publish\iis\frontend`
2. Port: `80` or `443`
3. Host: `yourdomain.com`

### Backend Site

1. Create website pointing to `publish\iis\backend`
2. Port: `5001` (or any available port)
3. Add binding for localhost or your domain

### Update Frontend Configuration

Update the proxy configuration or API base URL in Angular `environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'http://yourdomain.com:5001/api'  // Backend URL on separate port
};
```

Rebuild: `npm run build`

---

## Database Configuration

### Connection String in `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=credit_plus;Username=postgres;Password=123456"
  }
}
```

**Important:** Update these values for your production PostgreSQL database location.

### PostgreSQL Requirements

- PostgreSQL must be running and accessible
- Database: `credit_plus`
- Tables will be auto-created on first run (if EnsureDatabase is configured)

---

## Environment Variables

Set these as IIS Application Pool environment variables:

1. In IIS Manager, select the application pool
2. Right-click → **Set Application Pool Defaults** → **Recycling**
3. Or create a `.env` file in the backend folder with:

```
PORT=5001
ASPNETCORE_ENVIRONMENT=Production
ConnectionString__DefaultConnection=Host=localhost;Port=5433;Database=credit_plus;Username=postgres;Password=123456
```

---

## Frontend API Configuration

The frontend has been configured to communicate with the backend at: **`http://localhost:5001`**

This is set in `proxy.conf.json`:

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

**For production deployment**, update this to your production backend URL.

---

## CORS Configuration (if needed)

If frontend and backend are on different domains, enable CORS in `Program.cs`:

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

## Testing

### Test Backend

```bash
curl http://localhost:5001/api
```

Expected response: `{"message":"Credit Plus ASP.NET backend is running"}`

### Test Frontend

Navigate to: `http://yourdomain.com/` (or `http://localhost/` if using localhost)

---

## Troubleshooting

### 502 Bad Gateway
- Backend service not running
- Check Application Pool is started
- Verify backend port is accessible

### 404 Not Found
- URL Rewrite rule not configured properly
- web.config is missing or invalid

### Frontend API Calls Failing
- Check proxy configuration
- Verify backend is running on port 5001
- Check browser console for CORS errors
- Verify Application Pool permissions

### Database Connection Error
- PostgreSQL is not running
- Connection string is incorrect
- Database doesn't exist

---

## Production Checklist

- [ ] PostgreSQL is running and accessible
- [ ] Backend application pool is configured with proper permissions
- [ ] Frontend SPA routing (URL Rewrite) is enabled
- [ ] CORS is configured if needed
- [ ] SSL/HTTPS bindings are configured
- [ ] Backend service is set to auto-start
- [ ] Backup strategy is in place
- [ ] Monitoring and logging are configured

---

## File Locations

| Component | Location |
|-----------|----------|
| Frontend Build | `dist/credit-plus-angular/browser/` |
| Frontend Deploy | `publish/iis/frontend/` |
| Backend Build | `publish/iis/backend/` |
| Web Config (Frontend) | `publish/iis/frontend/web.config` |
| Web Config (Backend) | `publish/iis/backend/web.config` |
| App Settings | `publish/iis/backend/appsettings.json` |

