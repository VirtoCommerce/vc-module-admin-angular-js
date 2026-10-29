# Admin (AngularJS) Module

[![CI status](https://github.com/VirtoCommerce/vc-module-admin-angular-js/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-admin-angular-js/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-admin-angular-js&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-admin-angular-js)

## Overview

The AngularJS back office of Virto Commerce — Commerce Manager. It used to be part of the platform; from platform 3.1093.0 the platform is API only and this module brings the AngularJS admin back. Install it to keep the admin you have today; every module's AngularJS screens load into it unchanged.

It works alone, or next to [Admin Shell](https://github.com/VirtoCommerce/vc-module-admin-shell), the Vue admin: either one can own the site root, and Admin Shell embeds the AngularJS screens that have no Vue version yet.

## Key Features

* **The AngularJS admin as it was:** the same page, scripts, styles, images, templates and translations, at the same URLs (`/dist/app.js`, `/images/...`, `/$(Platform)/Scripts/...`).
* **Module screens unchanged:** each module's AngularJS bundle (`dist/app.js`, `dist/style.css`) is listed by `GET /api/apps/platform/manifest` and loaded by the admin, as before.
* **Site root or app path:** served at `/` when it owns the site root, and always at `/apps/platform/`, where Admin Shell embeds it when the shell owns the root.
* **Turn off without uninstalling:** `VirtoCommerce:PlatformUI:Enable = false` keeps the page closed; the API is not affected.
* **No database, no settings, no permissions** of its own. Who may sign in is the platform's admin UI access policy (`VirtoCommerce:PlatformUI:Access`).

## Configuration

### Site root

The module declares the app id the AngularJS admin always had, `platform`, as an admin UI with priority 100. The platform's `VirtoCommerce:PlatformUI:RootApp` decides which admin UI answers `/`:

| Installed | `RootApp` | `/` | `/apps/platform/` |
|---|---|---|---|
| This module | empty | AngularJS admin | AngularJS admin |
| This module + Admin Shell | empty | AngularJS admin (priority 100 beats the shell's 50) | AngularJS admin; Admin Shell at `/apps/admin-shell/` embeds `/` |
| This module + Admin Shell | `admin-shell` | Admin Shell | AngularJS admin, embedded by the shell |
| Any | `none` | The platform's notice (API-only nodes) | AngularJS admin |

```json
{
  "VirtoCommerce": {
    "PlatformUI": {
      "RootApp": "admin-shell"
    }
  }
}
```

or the environment variable `VirtoCommerce__PlatformUI__RootApp=admin-shell`. See the platform's [Admin UI](https://github.com/VirtoCommerce/vc-platform/blob/dev/docs/developer-guide/admin-ui.md) guide.

### Platform options the page uses

| Option | Effect |
|---|---|
| `VirtoCommerce:PlatformUI:Enable` | `false` turns the AngularJS admin off (the page is refused, also at `/apps/platform/`) |
| `VirtoCommerce:PlatformUI:EnvironmentBanner` | The environment banner at the top of the page |
| `VirtoCommerce:PlatformUI:Access` | Who may enter the admin after signing in |
| `VirtoCommerce:DemoCredentials`, `VirtoCommerce:DemoResetTime` | Demo sign-in hint and reset countdown |
| `VirtoCommerce:RefreshProbingFolderOnStart` | Whether modules can be installed from the Modules screen |
| `PushNotifications:ForceWebSockets` | SignalR transport for push notifications |

The values reach the page through the platform's `IAdminUIContextProvider`.

### Reverse proxies

Under `/apps/platform/` the platform answers like at the site root (`/apps/platform/api/...` is `/api/...`). Rules that allow or block paths by prefix need to cover that form too.

## Architecture

### Request flow

1. The platform's admin UI routing decides who answers the request. For `/` it is this module when the `platform` app owns the site root. A request under `/apps/platform/` reaches the platform as if it came to the site root, carrying `AdminUIRequestFeature.MountPath = "/apps/platform"`.
2. `AdminAngularJsController` (GET and HEAD at `/`, marked `[AdminUIPage("platform")]`) reads the platform's `IAdminUIContextProvider` and renders `Views/AdminAngularJs/Index.cshtml`. Its `<base href>` is the site path base plus the mount path, so the page's relative URLs (`api/...`, `dist/...`) stay where the page is.
3. The page loads `/dist/vendor.js`, `/dist/app.js` and `/dist/style.css` from the module's `Content/wwwroot`, which `Module.Initialize` added to the platform's web root.
4. `platformWebApp.pluginLoader.js` reads `GET /api/apps/platform/manifest` and loads every module's AngularJS bundle in dependency order, then starts AngularJS.
5. Non-bundled templates (development builds) load from `/$(Platform)/Scripts/...`, served by `Module.PostInitialize` from `Content/wwwroot/js`.

## Components

### Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| VirtoCommerce.AdminAngularJs.Web | Web | The AngularJS admin (`Content/wwwroot`), its page (controller and Razor views), translations and module initialization |
| VirtoCommerce.AdminAngularJs.Tests | Tests | Page rendering, module wiring, controller values; `Client/` holds Node tests of the SPA scripts |

### Key Classes

| Class | Responsibility |
|---------|----------------|
| `Module` | Adds `Content/wwwroot` to the platform web root (platform files win on the same path); serves `/$(Platform)/Scripts` (no-cache in Development) |
| `AdminAngularJsController` | Renders the admin page at `/` from the platform's UI context: version, license, demo values, environment banner, base href |

### Layout

```
src/VirtoCommerce.AdminAngularJs.Web/
├── Content/wwwroot/          ← joins the platform web root
│   ├── js/                   ← SPA sources (app, common, i18n); also served at /$(Platform)/Scripts
│   ├── css/                  ← SASS theme (entry css/themes/main/sass/main.sass), embedded-mode.css
│   ├── images/
│   ├── scripts/              ← platformWebApp.pluginLoader.js (outside js/ on purpose: not bundled)
│   ├── src/js/vendor.js      ← vendor bundle entry
│   ├── vendor/               ← vendored libraries
│   └── dist/                 ← webpack output (git-ignored)
├── Controllers/AdminAngularJsController.cs
├── Views/AdminAngularJs/     ← Index, _Layout, _AppDependencies
├── Localizations/            ← platform.* UI strings
├── Module.cs
└── package.json, npm-shrinkwrap.json, webpack.config.js
```

The bundles are built into `Content/wwwroot/dist`, not `dist`: the platform loads every module's `dist/app.js` as an AngularJS plugin, and the admin itself must not load twice.

### REST API

None. The module serves the page at `/` and static files; the controller reads the values from the platform's `IAdminUIContextProvider` in process (the same values other admin UIs get from `GET /api/platform/common/ui/context`).

## Build

```bash
cd src/VirtoCommerce.AdminAngularJs.Web
npm ci
npm run webpack:build    # or webpack:watch / webpack:dev
cd ../..
dotnet build
```

vc-build's `Compress` runs `npm ci` and `webpack:build` and packs `Content/` with the bundles.

## Testing

```bash
dotnet test
node --test tests/VirtoCommerce.AdminAngularJs.Tests/Client/oauth-login.test.cjs
```

## Documentation

* [Admin UI in the platform](https://github.com/VirtoCommerce/vc-platform/blob/dev/docs/developer-guide/admin-ui.md) — scenarios, `RootApp`, declaring an admin UI
* [Backoffice modularity](https://github.com/VirtoCommerce/vc-platform/blob/dev/docs/developer-guide/backoffice-modularity.md) — the plugin manifest the admin loads modules from
* [Admin Shell](https://github.com/VirtoCommerce/vc-module-admin-shell) — the Vue admin

The history of the files before the move is in [vc-platform](https://github.com/VirtoCommerce/vc-platform) (`src/VirtoCommerce.Platform.Web/wwwroot`).

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-admin-angular-js/releases/latest)

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
