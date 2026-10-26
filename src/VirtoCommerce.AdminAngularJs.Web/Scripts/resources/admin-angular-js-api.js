angular.module('VirtoCommerce.AdminAngularJs')
    .factory('VirtoCommerce.AdminAngularJs.webApi', ['$resource', function ($resource) {
        return $resource('api/admin-angular-js');
    }]);
