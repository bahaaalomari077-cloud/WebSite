# CreditPlusAngular

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 18.2.21.

## API connection

All frontend API calls go through `src/app/services/api.service.ts`.

The published backend URL is configured in `public/app-config.js`:

```js
window.CREDIT_PLUS_CONFIG = {
  apiBaseUrl: ""
};
```

Use `apiBaseUrl: ""` when the frontend and backend are published on the same domain, for example `https://credit-plus.me/api/news`.

Use the backend URL when the backend is hosted separately:

```js
window.CREDIT_PLUS_CONFIG = {
  apiBaseUrl: "https://api.credit-plus.me"
};
```

Do not use `proxy.conf.json` for production. It is only for local `ng serve`.

## Local run

Run the backend:

```powershell
npm.cmd run backend
```

Run the frontend:

```powershell
npm.cmd start
```

Local frontend requests go to `/api/...`, and Angular proxies them to the backend from `proxy.conf.json`.

## Publishing frontend and backend separately

1. Publish the ASP.NET backend first.
2. Set backend environment variables:

```text
DB_HOST=...
DB_PORT=5432
DB_NAME=...
DB_USER=...
DB_PASSWORD=...
CORS_ALLOWED_ORIGINS=https://your-frontend-domain.com
CROSS_SITE_COOKIES=true
```

3. Put the backend URL in `public/app-config.js`.
4. Build and publish the Angular site.

For Netlify, the site publishes from `dist/credit-plus-angular/browser`. API calls are controlled by `public/app-config.js`; Netlify Functions are not used for this ASP.NET backend.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
