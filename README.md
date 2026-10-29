# Admin (AngularJS)

## Overview

The AngularJS 1.8 back office of Virto Commerce (Commerce Manager), which used to be part of `VirtoCommerce.Platform.Web`.
The platform is API only from 3.1093.0; install this module to get the AngularJS admin.

- Without an admin UI module the platform runs as before, minus the UI: REST API, authorization endpoints, Swagger
  (`/docs`), health checks, SignalR and `<apps>` such as System Operations all work. `/` shows a short notice.
- It works alone or next to [Admin Shell](https://github.com/VirtoCommerce/vc-module-admin-shell), the Vue admin:
  `VirtoCommerce:PlatformUI:RootApp` decides which one owns `/`. The AngularJS admin is also always reachable at
  `/apps/platform/`, where Admin Shell embeds its screens when the shell owns the root.
- Modules' AngularJS bundles (`dist/app.js`, `dist/style.css`) load into it as before, from `GET /api/apps/platform/manifest`.
- No dependencies on other modules. Requires platform 3.1093.0 or later.

## Site root

| Installed | `VirtoCommerce:PlatformUI:RootApp` | `/` | `/apps/platform/` |
|---|---|---|---|
| This module | empty | AngularJS admin | AngularJS admin |
| This module + Admin Shell | empty | AngularJS admin (priority 100 beats the shell's 50) | AngularJS admin; Admin Shell at `/apps/admin-shell/` embeds `/` |
| This module + Admin Shell | `admin-shell` | Admin Shell | AngularJS admin, embedded by the shell |
| Any | `none` | The platform's notice | AngularJS admin |

See the platform's [Admin UI](https://github.com/VirtoCommerce/vc-platform/blob/dev/docs/developer-guide/admin-ui.md) guide.

## How it works

`module.manifest` declares the app the AngularJS admin always had:

```xml
<app id="platform">
  <title>Commerce Manager</title>
  <contentPath>Content/wwwroot</contentPath>
  <adminUI priority="100" hosting="Endpoint" />
</app>
```

| Extension point | What it does |
|---|---|
| `<adminUI hosting="Endpoint">` | The module maps the page itself; the platform also mounts it at `/apps/platform/` as a path base, so the page and every URL relative to it (API calls, bundles) work there too. |
| MVC controller at `/` (`AdminAngularJsController`, `[AdminUIPage("platform")]`) | Renders the page with the values of the platform's `IAdminUIContextProvider` (version, license, environment banner, ...). `<base href="~/">` follows the path base. |
| `IHasHostEnvironment` | Adds `Content/wwwroot` to the platform web root, so the SPA keeps its URLs: `/dist/app.js`, `/images/...`, `/css/...`, `/scripts/platformWebApp.pluginLoader.js`. Files of the platform's own `wwwroot` win on the same path. |
| `IModule.PostInitialize` | Serves the SPA sources at `/$(Platform)/Scripts`, where module and platform templates are loaded from in development builds. |
| `Localizations` | The `platform.*` UI strings, merged by the platform translation service. |

The bundles are built into `Content/wwwroot/dist`, not into `dist`: the platform treats every module's `dist/app.js`
as an AngularJS plugin, and the admin itself must not be loaded twice.

## Layout

```
src/VirtoCommerce.AdminAngularJs.Web/
├── Content/wwwroot/          ← served at the site root (and at /apps/platform/)
│   ├── js/                   ← SPA sources (app, common, i18n); also served at /$(Platform)/Scripts
│   ├── css/                  ← SASS theme (entry css/themes/main/sass/main.sass), embedded-mode.css
│   ├── images/
│   ├── scripts/              ← platformWebApp.pluginLoader.js (outside js/ on purpose: not bundled)
│   ├── src/js/vendor.js      ← vendor bundle entry
│   ├── vendor/               ← vendored libraries
│   └── dist/                 ← webpack output (git-ignored)
├── Controllers/AdminAngularJsController.cs
├── Views/AdminAngularJs/     ← Index, _Layout, _AppDependencies
├── Localizations/
├── Module.cs
├── package.json, npm-shrinkwrap.json, webpack.config.js
```

## Development

```bash
cd src/VirtoCommerce.AdminAngularJs.Web
npm ci
npm run webpack:build    # or webpack:watch / webpack:dev
```

Client tests for the SPA scripts run with Node:

```bash
node --test tests/VirtoCommerce.AdminAngularJs.Tests/Client/oauth-login.test.cjs
```

The history of the files before the move is in [vc-platform](https://github.com/VirtoCommerce/vc-platform)
(`src/VirtoCommerce.Platform.Web/wwwroot`).

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

<https://virtocommerce.com/open-source-license>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
