angular.module('platformWebApp').controller('platformWebApp.oAuthAppsController', ['$scope', '$q', '$translate', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.oauthapps', 'platformWebApp.validators', 'platformWebApp.clipboardService', function ($scope, $q, $translate, bladeNavigationService, dialogService, oauthapps, validators, clipboardService) {
    var blade = $scope.blade;
    blade.updatePermission = 'platform:security:update';

    var resourcePrefix = 'rsrc:';
    var pkce = 'ft:pkce';
    var clientCredentials = 'gt:client_credentials';
    var authorizationCode = 'gt:authorization_code';
    // set by the platform from the grant types when the application is saved
    var derivedPermissions = ['ept:authorization', 'ept:end_session', 'ept:token', 'rst:code'];

    $scope.grantTypes = [authorizationCode, 'gt:refresh_token', clientCredentials, 'gt:password'];
    $scope.scopes = ['scp:email', 'scp:profile', 'scp:roles'];
    $scope.clientTypes = ['public', 'confidential'];
    $scope.applicationTypes = ['web', 'native'];

    blade.resources = [];

    blade.refresh = function () {
        blade.isLoading = true;
        blade.error = undefined;

        // the resource list is optional: an older platform has none
        var resources = oauthapps.resources().$promise.catch(function () { return []; });
        var app = blade.isNew
            ? oauthapps.new().$promise
            // the stored state; the row of the list on a platform without GET by client id
            : oauthapps.get({ clientId: blade.data.clientId }).$promise.catch(function () { return angular.copy(blade.data); });

        $q.all([resources, app]).then(function (results) {
            blade.resources = results[0];

            var data = toPlain(results[1]);
            if (blade.isNew) {
                // kept for a switch back from a public to a confidential client
                blade.newSecret = data.clientSecret;
            }
            initialize(data);
            blade.isLoading = false;
        }, function (error) {
            blade.isLoading = false;
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    };

    function toPlain(app) {
        var result = angular.fromJson(angular.toJson(app));
        result.permissions = result.permissions || [];
        result.requirements = result.requirements || [];
        result.redirectUris = result.redirectUris || [];
        result.postLogoutRedirectUris = result.postLogoutRedirectUris || [];
        return result;
    }

    function initialize(data) {
        blade.origEntity = data;
        blade.currentEntity = angular.copy(data);
        setConsentTypes();
    }

    // a consent type the screen doesn't offer (external) stays selectable on the client that has it
    function setConsentTypes() {
        var values = ['explicit', 'implicit', 'systematic'];
        var current = blade.currentEntity && blade.currentEntity.consentType;
        blade.consentTypes = current && !_.contains(values, current) ? values.concat([current]) : values;
    }

    // permissions and requirements are sets: their order doesn't make the application modified
    function comparable(app) {
        var result = angular.copy(app);
        if (result) {
            result.permissions = _.sortBy(result.permissions);
            result.requirements = _.sortBy(result.requirements);
        }
        return result;
    }

    function isDirty() {
        return !angular.equals(comparable(blade.currentEntity), comparable(blade.origEntity)) && blade.hasUpdatePermission();
    }

    // permissions
    $scope.hasPermission = function (permission) {
        return blade.currentEntity && _.contains(blade.currentEntity.permissions, permission);
    };

    $scope.togglePermission = function (permission) {
        setPermission(permission, !$scope.hasPermission(permission));
    };

    function setPermission(permission, on) {
        var rest = _.without(blade.currentEntity.permissions, permission);
        blade.currentEntity.permissions = on ? rest.concat([permission]) : rest;
    }

    $scope.hasGrantType = function () {
        return _.any(blade.currentEntity.permissions, function (x) { return x.indexOf('gt:') === 0; });
    };

    $scope.otherPermissions = function () {
        return _.filter(blade.currentEntity.permissions, function (x) {
            return !_.contains($scope.grantTypes, x) && !_.contains($scope.scopes, x) && !_.contains(derivedPermissions, x) && x.indexOf(resourcePrefix) !== 0;
        });
    };

    $scope.requiresPkce = function () {
        return blade.currentEntity && _.contains(blade.currentEntity.requirements, pkce);
    };

    $scope.togglePkce = function () {
        setPkce(!$scope.requiresPkce());
    };

    function setPkce(on) {
        var rest = _.without(blade.currentEntity.requirements, pkce);
        blade.currentEntity.requirements = on ? rest.concat([pkce]) : rest;
    }

    $scope.setClientType = function (clientType) {
        blade.currentEntity.clientType = clientType;
        if (clientType === 'public') {
            // a public client has no secret, proves the sign-in with PKCE and can't use the client credentials grant
            blade.currentEntity.clientSecret = '';
            setPkce(true);
            setPermission(clientCredentials, false);
        } else {
            blade.currentEntity.clientSecret = blade.newSecret;
        }
    };


    // resources: those the platform knows (MCP endpoints, Authorization:Resources) are checkboxes, the others a list
    $scope.selectedResources = function () {
        return _.map(_.filter(blade.currentEntity.permissions, function (x) { return x.indexOf(resourcePrefix) === 0; }), function (x) {
            return x.substring(resourcePrefix.length);
        });
    };

    function isKnownResource(uri) {
        return _.any(blade.resources, function (x) { return x.uri === uri; });
    }

    $scope.otherResources = function () {
        return _.reject($scope.selectedResources(), isKnownResource);
    };

    $scope.unregisteredResources = function () {
        // with no resource list (an older platform), nothing is known to be missing
        if (!blade.resources.length) {
            return [];
        }
        return _.filter($scope.selectedResources(), function (uri) {
            return !_.any(blade.resources, function (x) { return x.uri === uri && x.isRegistered; });
        });
    };

    $scope.resourceTitle = function (resource) {
        if (!resource.title) {
            return resource.uri;
        }
        return resource.kind === 'mcp'
            ? $translate.instant('platform.blades.oauthapps-detail.labels.mcp-endpoint', { title: resource.title })
            : resource.title;
    };

    function setResources(uris) {
        var others = _.filter(blade.currentEntity.permissions, function (x) { return x.indexOf(resourcePrefix) !== 0; });
        blade.currentEntity.permissions = others.concat(_.map(_.uniq(uris), function (x) { return resourcePrefix + x; }));
    }

    $scope.toggleResource = function (uri) {
        var selected = $scope.selectedResources();
        setResources(_.contains(selected, uri) ? _.without(selected, uri) : selected.concat([uri]));
    };

    // redirect URIs: a URI the client already had is not checked again (the platform keeps it)
    $scope.uriErrors = function (uris, storedUris) {
        return _.filter(uris, function (uri) {
            var problem = validators.redirectUriProblem(uri);
            return problem && !(problem === 'http' && _.contains(storedUris, uri));
        }).map(function (uri) {
            return { uri: uri, problem: validators.redirectUriProblem(uri) };
        });
    };

    $scope.legacyHttpUris = function () {
        return _.filter((blade.currentEntity.redirectUris || []).concat(blade.currentEntity.postLogoutRedirectUris || []), function (uri) {
            return validators.redirectUriProblem(uri) === 'http';
        });
    };

    $scope.canSave = function () {
        var app = blade.currentEntity;
        return app && blade.hasUpdatePermission() && (blade.isNew || isDirty()) &&
            app.displayName && app.clientId && app.clientId !== blade.clientIdTaken &&
            $scope.hasGrantType() &&
            !$scope.uriErrors(app.redirectUris, blade.origEntity.redirectUris).length &&
            !$scope.uriErrors(app.postLogoutRedirectUris, blade.origEntity.postLogoutRedirectUris).length;
    };

    blade.onClose = function (closeCallback) {
        bladeNavigationService.showConfirmationIfNeeded(isDirty(), !!$scope.canSave(), blade, $scope.saveChanges, closeCallback, "platform.dialogs.oauthapps-save.title", "platform.dialogs.oauthapps-save.message");
    };

    $scope.cancelChanges = function () {
        $scope.bladeClose();
    };

    $scope.saveChanges = function () {
        blade.isLoading = true;
        blade.error = undefined;

        // POST oauthapps updates the application with the client id: a new one must not overwrite another
        var exists = blade.isNew
            ? oauthapps.get({ clientId: blade.currentEntity.clientId }).$promise.then(function () { return true; }, function () { return false; })
            : $q.resolve(false);

        exists.then(function (taken) {
            if (taken) {
                blade.isLoading = false;
                blade.clientIdTaken = blade.currentEntity.clientId;
                return;
            }

            oauthapps.save({}, blade.currentEntity, function () {
                blade.isLoading = false;
                blade.origEntity = angular.copy(blade.currentEntity);
                blade.parentBlade.refresh();
                $scope.bladeClose();
            }, function (error) {
                blade.isLoading = false;
                var errors = error.data && error.data.errors;
                bladeNavigationService.setError(errors && errors.length ? errors.join(' ') : 'Error ' + error.status, blade);
            });
        });
    };

    function deleteEntry() {
        var dialog = {
            id: "confirmDelete",
            title: "platform.dialogs.oauthapps-delete.title",
            message: "platform.dialogs.oauthapps-delete.message",
            messageValues: { name: blade.currentEntity.displayName || blade.currentEntity.clientId },
            callback: function (remove) {
                if (remove) {
                    blade.isLoading = true;
                    var clientIds = [blade.currentEntity.clientId];
                    oauthapps.delete({ clientIds }, result => {
                        blade.isLoading = false;
                        blade.parentBlade.refresh();
                        $scope.bladeClose();
                    });
                }
            }
        }
        dialogService.showDeleteConfirmationDialog(dialog);
    }

    blade.headIcon = 'fas fa-key';

    blade.toolbarCommands = [
        {
            name: "platform.commands.refresh", icon: 'fa fa-refresh',
            executeMethod: blade.refresh,
            canExecuteMethod: function () {
                return !blade.isNew;
            }
        },
        {
            name: "platform.commands.reset",
            icon: 'fa fa-undo',
            executeMethod: function () {
                blade.currentEntity = angular.copy(blade.origEntity);
                setConsentTypes();
            },
            canExecuteMethod: isDirty,
            permission: blade.updatePermission
        },
        {
            name: "platform.commands.delete", icon: 'fas fa-trash-alt',
            executeMethod: deleteEntry,
            canExecuteMethod: function () {
                return !blade.isNew;
            },
            permission: blade.updatePermission
        }
    ];

    $scope.copyToClipboard = function (elementId) {
        var element = document.getElementById(elementId);
        clipboardService.copyText(element.value || element.innerText);
    };

    // the secret can't be shown again after the application is created: keep it in a file too
    $scope.downloadCredentials = function () {
        var app = blade.currentEntity;
        var text = 'client_id=' + app.clientId + '\r\nclient_secret=' + app.clientSecret + '\r\n';
        var a = document.createElement('a');
        a.href = URL.createObjectURL(new Blob([text], { type: 'text/plain' }));
        a.download = 'oauth-client-' + app.clientId + '.txt';
        a.click();
        URL.revokeObjectURL(a.href);
    };

    function editList(id, values, validator, onChange) {
        var newBlade = {
            id: id,
            updatePermission: blade.updatePermission,
            data: values,
            validator: validator,
            headIcon: 'far fa-plus-square',
            title: 'platform.blades.oauthapps-detail.blades.' + id + '.title',
            subtitle: 'platform.blades.oauthapps-detail.blades.' + id + '.subtitle',
            controller: 'platformWebApp.editArrayController',
            template: '$(Platform)/Scripts/common/blades/edit-array.tpl.html',
            onChangesConfirmedFn: function (items) {
                onChange(angular.copy(items));
            }
        };

        bladeNavigationService.showBlade(newBlade, blade);
    }

    $scope.editRedirectUris = function () {
        editList('edit-redirectUris', blade.currentEntity.redirectUris, validators.redirectUri, function (values) {
            blade.currentEntity.redirectUris = values;
        });
    };

    $scope.editPostLogoutRedirectUris = function () {
        editList('edit-postLogoutRedirectUris', blade.currentEntity.postLogoutRedirectUris, validators.redirectUri, function (values) {
            blade.currentEntity.postLogoutRedirectUris = values;
        });
    };

    $scope.editOtherResources = function () {
        editList('edit-resources', $scope.otherResources(), validators.absoluteUri, function (values) {
            setResources(_.filter($scope.selectedResources(), isKnownResource).concat(values));
        });
    };

    $scope.editOtherPermissions = function () {
        editList('edit-permissions', $scope.otherPermissions(), validators.none, function (values) {
            var others = $scope.otherPermissions();
            blade.currentEntity.permissions = _.reject(blade.currentEntity.permissions, function (x) { return _.contains(others, x); }).concat(values);
        });
    };

    blade.refresh();
}]);
