﻿angular.module('virtoCommerce.packaging.resources.modules', [])
.factory('modules', ['$resource', function ($resource) {

    return $resource('api/modules/:id', { id: '@id' }, {
        getModules: { url: 'api/modules', isArray: true },
        // get: { url: 'api/modules/:id', isArray: false },
        install: { url: 'api/modules/install' },
        update: { url: 'api/modules/:id/update' },
        uninstall: { url: 'api/modules/:id/uninstall' },
        getInstallationStatus: { url: 'api/modules/jobs/:id' }
    });
}]);
