angular.module('platformWebApp')
    .controller('platformWebApp.accountApiController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.accounts', 'platformWebApp.clipboardService', function ($scope, bladeNavigationService, dialogService, accounts, clipboardService) {
        var blade = $scope.blade;
        blade.updatePermission = 'platform:security:update';

        // The platform generates keys and returns a key's value once, in the response that created it. The blade
        // shows it from there; afterwards only the hint the platform sends in place of the key is shown.
        blade.newApiKey = null;

        function refresh() {
            blade.isLoading = true;
            accounts.getUserApiKeys({ id: blade.user.id }, function (data) {
                initializeBlade(data.length > 0 ? data[0] : { userId: blade.user.id, userName: blade.user.userName, isActive: true });
            },
                function (error) {
                    bladeNavigationService.setError(error, blade);
                });
        }

        function initializeBlade(data) {
            blade.isNew = !data.id;
            blade.currentEntity = angular.copy(data);
            blade.origEntity = data;
            blade.isLoading = false;
        }

        function showNewApiKey(apiKey) {
            blade.newApiKey = apiKey.apiKey;
            initializeBlade(angular.extend({}, apiKey, { apiKey: apiKey.apiKeyHint }));
        }

        $scope.setForm = function (form) {
            $scope.formScope = form;
        }

        function isDirty() {
            return !angular.equals(blade.currentEntity, blade.origEntity) && blade.hasUpdatePermission();
        }

        $scope.cancelChanges = function () {
            $scope.bladeClose();
        }

        $scope.saveChanges = function () {
            var creating = blade.isNew;
            blade.isLoading = true;
            // An existing key is sent with its hint, which keeps the stored key; a new one without a value, so the
            // platform generates it.
            var entity = angular.copy(blade.currentEntity);
            if (creating) {
                delete entity.apiKey;
            }

            accounts.saveUserApiKey(entity, function (data) {
                if (creating && data && data.length) {
                    // Stay open: this is the only time the key can be copied.
                    showNewApiKey(data[0]);
                } else {
                    $scope.bladeClose();
                }
            }, function (error) {
                bladeNavigationService.setError(error, blade);
            });
        };

        function regenerate() {
            dialogService.showConfirmationDialog({
                id: "confirmRegenerateApiKey",
                title: "platform.dialogs.api-key-regenerate.title",
                message: "platform.dialogs.api-key-regenerate.message",
                callback: function (confirmed) {
                    if (confirmed) {
                        blade.isLoading = true;
                        accounts.regenerateUserApiKey({ keyId: blade.currentEntity.id }, null, function (data) {
                            showNewApiKey(data);
                        }, function (error) {
                            bladeNavigationService.setError(error, blade);
                        });
                    }
                }
            });
        }

        function deleteEntry() {
            var dialog = {
                id: "confirmDelete",
                title: "platform.dialogs.api-key-delete.title",
                message: "platform.dialogs.api-key-delete.message",
                messageValues: { name: blade.currentEntity.apiKeyHint || blade.currentEntity.id },
                callback: function (remove) {
                    if (remove) {
                        accounts.deleteUserApiKey({ ids: blade.currentEntity.id });
                        $scope.bladeClose();
                    }
                }
            }
            dialogService.showDeleteConfirmationDialog(dialog);
        }

        $scope.copyNewApiKey = function () {
            clipboardService.copyText(blade.newApiKey);
        };

        blade.headIcon = 'fas fa-key';

        blade.toolbarCommands = [
            {
                name: "platform.commands.generate", icon: 'fa fa-refresh',
                executeMethod: regenerate,
                canExecuteMethod: function () {
                    return !blade.isNew;
                },
                permission: blade.updatePermission
            },
            {
                name: "platform.commands.reset",
                icon: 'fa fa-undo',
                executeMethod: function () {
                    angular.copy(blade.origEntity, blade.currentEntity);
                },
                canExecuteMethod: isDirty,
            },
            {
                name: "platform.commands.delete", icon: 'fas fa-trash-alt',
                executeMethod: deleteEntry,
                canExecuteMethod: function () {
                    return !blade.isNew;
                },
                permission: 'platform:security:delete'
            }
        ];

        // on load:
        refresh();
    }]);
