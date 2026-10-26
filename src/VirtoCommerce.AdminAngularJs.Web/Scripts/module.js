// Call this to register your module to main application
var moduleName = 'VirtoCommerce.AdminAngularJs';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider',
        function ($stateProvider) {
            $stateProvider
                .state('workspace.AdminAngularJsState', {
                    url: '/admin-angular-js',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        'platformWebApp.bladeNavigationService',
                        function (bladeNavigationService) {
                            var newBlade = {
                                id: 'blade1',
                                controller: 'VirtoCommerce.AdminAngularJs.helloWorldController',
                                template: 'Modules/$(VirtoCommerce.AdminAngularJs)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state',
        function (mainMenuService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/admin-angular-js',
                icon: 'fa fa-cube',
                title: 'Admin Angular JS',
                priority: 100,
                action: function () { $state.go('workspace.AdminAngularJsState'); },
                permission: 'admin-angular-js:access',
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
