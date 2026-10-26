angular.module('VirtoCommerce.AdminAngularJs')
    .controller('VirtoCommerce.AdminAngularJs.helloWorldController', ['$scope', 'VirtoCommerce.AdminAngularJs.webApi', function ($scope, api) {
        var blade = $scope.blade;
        blade.title = 'Admin Angular JS';

        blade.refresh = function () {
            api.get(function (data) {
                blade.title = 'admin-angular-js.blades.hello-world.title';
                blade.data = data.result;
                blade.isLoading = false;
            });
        };

        blade.refresh();
    }]);
