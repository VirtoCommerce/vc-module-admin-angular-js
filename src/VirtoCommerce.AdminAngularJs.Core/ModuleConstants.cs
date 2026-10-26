using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.AdminAngularJs.Core;

public static class ModuleConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Access = "admin-angular-js:access";
            public const string Create = "admin-angular-js:create";
            public const string Read = "admin-angular-js:read";
            public const string Update = "admin-angular-js:update";
            public const string Delete = "admin-angular-js:delete";

            public static string[] AllPermissions { get; } =
            [
                Access,
                Create,
                Read,
                Update,
                Delete,
            ];
        }
    }

    public static class Settings
    {
        public static class General
        {
            public static SettingDescriptor AdminAngularJsEnabled { get; } = new()
            {
                Name = "AdminAngularJs.Enabled",
                GroupName = "Admin Angular JS|General",
                ValueType = SettingValueType.Boolean,
                DefaultValue = false,
            };

            public static IEnumerable<SettingDescriptor> AllGeneralSettings
            {
                get
                {
                    yield return AdminAngularJsEnabled;
                }
            }
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return General.AllGeneralSettings;
            }
        }
    }
}
