﻿angular.module('virtoCommerce.packaging.blades.modulesList', [
    'virtoCommerce.packaging.blades.moduleDetail',
    'virtoCommerce.packaging.wizards.newModule.installWizard',
    'virtoCommerce.packaging.resources.modules'
])
.controller('modulesListController', ['$rootScope', '$scope', 'bladeNavigationService', 'dialogService', 'modules', function ($rootScope, $scope, bladeNavigationService, dialogService, modules) {
    $scope.selectedEntityId = null;

    $scope.blade.refresh = function () {
        $scope.blade.isLoading = true;

        modules.getModules({}, function (results) {
            $scope.blade.isLoading = false;
            $scope.blade.currentEntities = results;
        });
    };

    $scope.selectItem = function (listItem) {
        $scope.selectedEntityId = listItem.id;

        var newBlade = {
            id: 'moduleDetails',
            title: 'Module information',
            currentEntity: listItem,
            controller: 'moduleDetailController',
            template: 'Modules/Packaging/VirtoCommerce.PackagingModule.Web/Scripts/blades/module-detail.tpl.html'
        };

        bladeNavigationService.showBlade(newBlade, $scope.blade);
    }

    $scope.blade.onClose = function (closeCallback) {
        closeChildrenBlades();
        closeCallback();
    };

    function closeChildrenBlades() {
        angular.forEach($scope.blade.childrenBlades.slice(), function (child) {
            bladeNavigationService.closeBlade(child);
        });
    }

    $scope.bladeToolbarCommands = [
        {
            name: "Refresh", icon: 'fa fa-refresh',
            executeMethod: function () {
                $scope.blade.refresh();
            },
            canExecuteMethod: function () {
                return true;
            }
        },
        {
            name: "Install", icon: 'fa fa-plus',
            executeMethod: function () {
                openAddEntityBlade();
            },
            canExecuteMethod: function () {
                return true;
            }
        }
    ];

    function openAddEntityBlade() {
        closeChildrenBlades();

        var newBlade = {
            id: "moduleWizard",
            title: "Module install",
            // subtitle: '',
            mode: 'install',
            controller: 'installWizardController',
            template: 'Modules/Packaging/VirtoCommerce.PackagingModule.Web/Scripts/blades/module-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, $scope.blade);
    }

    $scope.blade.refresh();
}]);
